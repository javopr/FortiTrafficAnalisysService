using Azure;
using Azure.AI.OpenAI;
using FortiTrafficAnalysis.Data;
using FortiTrafficAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace FortiTrafficAnalysis.Services.AI
{
    /// <summary>
    /// Azure OpenAI service implementation for traffic analysis recommendations
    /// Uses GPT-4.1 for intelligent FortiGate policy suggestions
    /// Author: javier.morales@intwo.cloud
    /// </summary>
    public class AzureOpenAIService : IAIRecommendationService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly string _apiVersion;

        public AzureOpenAIService(
            IConfiguration configuration,
            ILogger<AzureOpenAIService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

            // Read configuration
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            _deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4.1";
            _apiVersion = configuration["AzureOpenAI:ApiVersion"] ?? "2024-02-01";
            _maxTokens = int.Parse(configuration["AzureOpenAI:MaxTokens"] ?? "2000");
            _temperature = float.Parse(configuration["AzureOpenAI:Temperature"] ?? "0.3");
            var apiKey = configuration["AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI configuration is missing. Check appsettings.json and appsettings.Development.json");
            }

            // Create OpenAI client
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

            _logger.LogInformation("Azure OpenAI Service initialized: Endpoint={Endpoint}, Deployment={Deployment}",
                endpoint, _deploymentName);
        }

        public async Task<string> AskQuestionAsync(
            Guid trafficAnalysisId,
            string userQuestion,
            List<TrafficLog> selectedLogs,
            string userName)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "AI Question from {User} on ticket {TicketId}: {Question}",
                    userName, trafficAnalysisId, userQuestion);

                // Get conversation history for this ticket
                var conversationHistory = await GetConversationHistoryAsync(trafficAnalysisId);
                var isFirstQuestion = !conversationHistory.Any();

                // Get FortiGate config only if first question (to include in context)
                string? configFile = null;
                if (isFirstQuestion)
                {
                    var ticket = await _context.TrafficAnalyses
                        .Include(t => t.FortiGate)
                        .FirstOrDefaultAsync(t => t.TrafficAnalysisID == trafficAnalysisId);

                    configFile = ticket?.FortiGate?.ConfigFile;

                    _logger.LogInformation(
                        "First question - including config. HasConfig={HasConfig}, ConfigLength={Length}",
                        configFile != null,
                        configFile?.Length ?? 0);
                }
                else
                {
                    _logger.LogInformation(
                        "Subsequent question - using conversation history (config already sent)");
                }

                // Build context from selected logs and config (config only on first question)
                var context = BuildLogContext(selectedLogs, configFile);

                // Build system prompt
                var systemPrompt = @"You are a senior FortiGate firewall expert with 10+ years of experience in network security.

Your expertise includes:
- FortiOS 7.x CLI syntax and configuration
- Network security best practices
- FortiGate policy optimization and troubleshooting
- Traffic analysis and pattern recognition
- Security risk assessment

CRITICAL INSTRUCTIONS:

1. **ANSWER STYLE - BE DIRECT AND CONCISE**
   - Answer the EXACT question asked - nothing more, nothing less
   - Start with the DIRECT ANSWER in the first sentence
   - Then provide brief explanation ONLY if needed
   - Don't repeat the question back to the user
   - Don't explain basic concepts unless asked
   - Don't add unnecessary context or disclaimers

2. **READ THE CONFIGURATION FIRST**
   - Before answering, review the FortiGate configuration provided
   - NEVER create objects that already exist in the config
   - ALWAYS reference existing objects by name
   - If the answer requires new objects, create them with unique names

3. **RESPONSE FORMAT**
   - For factual questions (""which zone?"", ""what interface?""): Give ONE LINE answer
   - For CLI commands: Provide ONLY the command, no explanation unless asked
   - For troubleshooting: State the problem, then the fix
   - Use code blocks ONLY for CLI commands
   - Use bullet points ONLY when listing multiple items

4. **EXAMPLES OF CORRECT RESPONSES:**

User: ""The interface Claro-3536 is member of which SD-WAN zone?""
? BAD: ""Based on your FortiGate configuration, I can see the SD-WAN configuration includes... [200 words]""
? GOOD: ""The interface 'Claro-3536' is not found in the SD-WAN members. The configured SD-WAN interfaces are: Claro_139-1250 (zone: Internet), LibertyA9200050 (zone: Internet).""

User: ""Create a policy to allow HTTP traffic from LAN to DMZ""
? BAD: ""First, let me explain firewall policies... Based on your configuration... [300 words]""
? GOOD: 
```
config firewall policy
    edit 0
    set name ""Allow_LAN_to_DMZ_HTTP""
    set srcintf ""port1""
    set dstintf ""port3""
    set srcaddr ""LAN_Subnet""
    set dstaddr ""DMZ_Network""
    set service ""HTTP""
    set action accept
end
```
(Using existing objects from your config: LAN_Subnet, DMZ_Network)

User: ""What's the IP of interface port2?""
? BAD: ""Looking at your configuration, I can see that interface port2 is configured with... [100 words]""
? GOOD: ""port2: 10.0.0.1/24""

5. **WHEN TO BE VERBOSE**
   - ONLY when explicitly asked: ""explain"", ""why"", ""how does"", ""what's the difference""
   - For security warnings (but keep them brief)
   - When listing multiple items (use bullets)

6. **WHAT TO AVOID**
   - Don't start with ""Based on your configuration...""
   - Don't say ""I can see in the config...""
   - Don't explain what you're doing - just do it
   - Don't add warnings unless there's a real security risk
   - Don't suggest testing in lab unless explicitly risky

Remember: The user is an expert. They want ANSWERS, not explanations.";

                // Build user prompt with context
                var userPrompt = $@"Traffic Analysis Context:
{context}

User Question: {userQuestion}

Please provide a detailed, accurate response. If the question involves creating policies, include complete FortiGate CLI commands.";

                // Create chat completion request with conversation history
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _deploymentName,
                    MaxTokens = _maxTokens,
                    Temperature = _temperature
                };

                // Add system message
                chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(systemPrompt));

                // Add conversation history (limit to last 10 to avoid context window overflow)
                var recentHistory = conversationHistory
                    .OrderBy(c => c.CreatedDate)
                    .TakeLast(10)
                    .ToList();

                foreach (var conv in recentHistory)
                {
                    chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(conv.UserQuestion));
                    chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(conv.AIResponse));
                }

                // Add current question with context
                chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(userPrompt));

                _logger.LogInformation(
                    "Calling Azure OpenAI API with {RecentCount} of {TotalCount} messages in history",
                    recentHistory.Count, conversationHistory.Count);

                // Call Azure OpenAI
                Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);

                stopwatch.Stop();

                var aiResponse = response.Value.Choices[0].Message.Content;
                var tokensUsed = response.Value.Usage.TotalTokens;

                _logger.LogInformation(
                    "AI Response generated successfully. Tokens: {Tokens}, Time: {Ms}ms",
                    tokensUsed, stopwatch.ElapsedMilliseconds);

                // Save conversation to database
                await SaveConversationAsync(
                    trafficAnalysisId,
                    userQuestion,
                    aiResponse,
                    userName,
                    tokensUsed,
                    (int)stopwatch.ElapsedMilliseconds);

                return aiResponse;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex,
                    "Azure OpenAI API request failed. ErrorCode: {ErrorCode}, Status: {Status}",
                    ex.ErrorCode, ex.Status);

                throw new ApplicationException(
                    $"AI service temporarily unavailable: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AI service");
                throw new ApplicationException(
                    "An error occurred while processing your request. Please try again later.", ex);
            }
        }

        public async Task<List<AIConversation>> GetConversationHistoryAsync(Guid trafficAnalysisId)
        {
            try
            {
                return await _context.AIConversations
                    .Where(c => c.TrafficAnalysisID == trafficAnalysisId)
                    .OrderBy(c => c.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation history for ticket {TicketId}", trafficAnalysisId);
                return new List<AIConversation>();
            }
        }

        private string BuildLogContext(List<TrafficLog> logs, string? configFile = null)
        {
            var sb = new StringBuilder();

            // Include FortiGate configuration if available
            if (!string.IsNullOrEmpty(configFile))
            {
                sb.AppendLine("??????????????????????????????????????????????");
                sb.AppendLine("   FORTIGATE CONFIGURATION FILE");
                sb.AppendLine("??????????????????????????????????????????????");
                sb.AppendLine();
                sb.AppendLine("?? CRITICAL: YOU MUST READ AND USE THIS CONFIGURATION");
                sb.AppendLine("?? DO NOT CREATE OBJECTS THAT ALREADY EXIST BELOW");
                sb.AppendLine();
                
                // Parse and extract key sections for the AI to easily reference
                sb.AppendLine("?? KEY OBJECTS TO REFERENCE (parsed from config):");
                sb.AppendLine();
                
                // Extract key object names for quick reference
                ExtractConfigObjectSummary(configFile, sb);
                
                sb.AppendLine("??????????????????????????????????????????????");
                sb.AppendLine("   COMPLETE CONFIGURATION");
                sb.AppendLine("??????????????????????????????????????????????");
                sb.AppendLine();
                
                // Send the ENTIRE config - let the model handle it
                // GPT-4 Turbo supports ~128K tokens (~400K characters)
                // Most FortiGate configs are well within this limit
                sb.AppendLine(configFile);
                
                sb.AppendLine();
                sb.AppendLine("??????????????????????????????????????????????");
                sb.AppendLine($"Configuration size: {configFile.Length:N0} characters, ~{EstimateTokenCount(configFile):N0} tokens");
                sb.AppendLine("??????????????????????????????????????????????");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("?? WARNING: No FortiGate configuration file uploaded.");
                sb.AppendLine("?? Recommendations will be GENERIC and may create duplicate objects.");
                sb.AppendLine("?? Upload the config file for accurate, context-aware suggestions.");
                sb.AppendLine();
            }

            // Traffic logs context
            if (logs == null || !logs.Any())
            {
                sb.AppendLine("No traffic logs selected.");
                return sb.ToString();
            }

            sb.AppendLine("??????????????????????????????????????????????");
            sb.AppendLine("   TRAFFIC LOGS ANALYSIS");
            sb.AppendLine("??????????????????????????????????????????????");
            sb.AppendLine();
            sb.AppendLine($"Total logs selected: {logs.Count}");
            sb.AppendLine();

            // Group by action for summary
            var deniedCount = logs.Count(l => l.Action?.ToLower() == "deny" || l.Action?.Contains("rst") == true);
            var acceptedCount = logs.Count(l => l.Action?.ToLower() == "accept");

            sb.AppendLine($"Traffic Summary:");
            sb.AppendLine($"  • Denied/Blocked: {deniedCount}");
            sb.AppendLine($"  • Accepted: {acceptedCount}");
            sb.AppendLine();

            // Show sample of logs (limit to 50 to avoid token limits)
            var sampleLogs = logs.Take(50).ToList();

            sb.AppendLine("Sample log entries:");
            foreach (var log in sampleLogs)
            {
                sb.AppendLine($"  • Action: {log.Action ?? "N/A"}, " +
                              $"Src: {log.SrcIP}:{log.SrcPort} ({log.SrcInt ?? "N/A"}), " +
                              $"Dst: {log.DstIP}:{log.DstPort} ({log.DstInt ?? "N/A"}), " +
                              $"Proto: {log.Proto ?? "N/A"}, " +
                              $"Service: {log.Service ?? "N/A"}, " +
                              $"Policy: {log.PolicyId ?? "N/A"} ({log.PolicyName ?? "N/A"})");
            }

            if (logs.Count > 50)
            {
                sb.AppendLine($"\n... and {logs.Count - 50} more log entries with similar patterns");
            }
            
            sb.AppendLine();
            sb.AppendLine("??????????????????????????????????????????????");

            return sb.ToString();
        }

        private void ExtractConfigObjectSummary(string configFile, StringBuilder sb)
        {
            try
            {
                // Extract interfaces
                var interfaceMatches = System.Text.RegularExpressions.Regex.Matches(
                    configFile, @"edit ""([^""]+)""\s+set vdom",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (interfaceMatches.Count > 0)
                {
                    sb.AppendLine("?? INTERFACES:");
                    foreach (System.Text.RegularExpressions.Match match in interfaceMatches.Take(20))
                    {
                        sb.AppendLine($"   - {match.Groups[1].Value}");
                    }
                    if (interfaceMatches.Count > 20)
                        sb.AppendLine($"   ... and {interfaceMatches.Count - 20} more");
                    sb.AppendLine();
                }

                // Extract SD-WAN zones and members
                var sdwanZoneMatches = System.Text.RegularExpressions.Regex.Matches(
                    configFile, @"config system sdwan[\s\S]*?config zone[\s\S]*?edit ""([^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (sdwanZoneMatches.Count > 0)
                {
                    sb.AppendLine("?? SD-WAN ZONES:");
                    foreach (System.Text.RegularExpressions.Match match in sdwanZoneMatches)
                    {
                        sb.AppendLine($"   - {match.Groups[1].Value}");
                    }
                    sb.AppendLine();
                }

                // Extract SD-WAN members
                var sdwanMemberMatches = System.Text.RegularExpressions.Regex.Matches(
                    configFile, @"config system sdwan[\s\S]*?set interface ""([^""]+)""[\s\S]*?set zone ""([^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (sdwanMemberMatches.Count > 0)
                {
                    sb.AppendLine("?? SD-WAN MEMBERS:");
                    foreach (System.Text.RegularExpressions.Match match in sdwanMemberMatches)
                    {
                        sb.AppendLine($"   - {match.Groups[1].Value} ? zone '{match.Groups[2].Value}'");
                    }
                    sb.AppendLine();
                }

                // Extract address objects
                var addressMatches = System.Text.RegularExpressions.Regex.Matches(
                    configFile, @"config firewall address[\s\S]*?edit ""([^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (addressMatches.Count > 0)
                {
                    sb.AppendLine("?? ADDRESS OBJECTS:");
                    foreach (System.Text.RegularExpressions.Match match in addressMatches.Take(20))
                    {
                        sb.AppendLine($"   - {match.Groups[1].Value}");
                    }
                    if (addressMatches.Count > 20)
                        sb.AppendLine($"   ... and {addressMatches.Count - 20} more");
                    sb.AppendLine();
                }

                // Extract service objects
                var serviceMatches = System.Text.RegularExpressions.Regex.Matches(
                    configFile, @"config firewall service custom[\s\S]*?edit ""([^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (serviceMatches.Count > 0)
                {
                    sb.AppendLine("?? SERVICE OBJECTS:");
                    foreach (System.Text.RegularExpressions.Match match in serviceMatches.Take(20))
                    {
                        sb.AppendLine($"   - {match.Groups[1].Value}");
                    }
                    if (serviceMatches.Count > 20)
                        sb.AppendLine($"   ... and {serviceMatches.Count - 20} more");
                    sb.AppendLine();
                }

                // Extract policy IDs
                var policyMatches = System.Text.RegularExpressions.Regex.Matches(
                    configFile, @"config firewall policy[\s\S]*?edit (\d+)",
                    System.Text.RegularExpressions.RegexOptions.Multiline);
                if (policyMatches.Count > 0)
                {
                    sb.AppendLine("?? EXISTING POLICY IDs:");
                    sb.Append("   ");
                    sb.AppendLine(string.Join(", ", policyMatches.Cast<System.Text.RegularExpressions.Match>().Take(30).Select(m => m.Groups[1].Value)));
                    if (policyMatches.Count > 30)
                        sb.AppendLine($"   ... and {policyMatches.Count - 30} more");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing config object summary");
            }
        }

        private int EstimateTokenCount(string text)
        {
            // Rough estimate: ~4 characters per token for English text
            // FortiGate configs are more dense, so use ~3.5
            return (int)(text.Length / 3.5);
        }

        private string CompressConfigForAI(string configFile)
        {
            var sb = new StringBuilder();
            
            // Priority sections to keep (everything else gets removed)
            var keepSections = new[]
            {
                "config system global",
                "config system interface",
                "config system sdwan",
                "config system zone",
                "config firewall address",
                "config firewall addrgrp",
                "config firewall service custom",
                "config firewall service group",
                "config firewall policy",
                "config router static",
                "config vpn ipsec phase1-interface",
                "config vpn ipsec phase2-interface",
            };

            try
            {
                var lines = configFile.Split('\n');
                var inKeepSection = false;
                var currentIndent = 0;
                var sectionStartIndent = 0;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    // Check if entering a section we want to keep
                    if (trimmed.StartsWith("config ", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if this is a priority section
                        var isKeepSection = keepSections.Any(s => 
                            trimmed.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                        
                        if (isKeepSection)
                        {
                            inKeepSection = true;
                            sectionStartIndent = currentIndent;
                            currentIndent++;
                            sb.AppendLine(line);
                        }
                        else if (inKeepSection)
                        {
                            // Nested config block inside a keep section
                            currentIndent++;
                            sb.AppendLine(line);
                        }
                    }
                    else if (trimmed == "end")
                    {
                        if (inKeepSection)
                        {
                            sb.AppendLine(line);
                            currentIndent--;
                            
                            // Exit keep section when we return to original indent
                            if (currentIndent <= sectionStartIndent)
                            {
                                inKeepSection = false;
                                sb.AppendLine(); // Add blank line between sections
                            }
                        }
                    }
                    else if (inKeepSection)
                    {
                        // Inside a section we want to keep
                        sb.AppendLine(line);
                    }
                }

                var compressed = sb.ToString();

                // If compression was too aggressive (removed too much), fall back
                if (compressed.Length < configFile.Length * 0.2) // Less than 20% of original
                {
                    _logger.LogWarning(
                        "Compression too aggressive ({Percent:F1}%), using fallback strategy",
                        (compressed.Length * 100.0 / configFile.Length));
                    
                    // Fallback: just truncate to 150KB
                    return configFile.Length > 150000
                        ? configFile.Substring(0, 150000) + "\n... (config truncated at 150KB)"
                        : configFile;
                }

                _logger.LogInformation(
                    "Config compressed: {Original} ? {Compressed} chars ({Percent:F1}% reduction)",
                    configFile.Length, compressed.Length,
                    (100 - (compressed.Length * 100.0 / configFile.Length)));

                return compressed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing config, using original");
                return configFile;
            }
        }

        private async Task SaveConversationAsync(
            Guid trafficAnalysisId,
            string userQuestion,
            string aiResponse,
            string userName,
            int tokensUsed,
            int responseTimeMs)
        {
            try
            {
                var conversation = new AIConversation
                {
                    ConversationID = Guid.NewGuid(),
                    TrafficAnalysisID = trafficAnalysisId,
                    UserQuestion = userQuestion,
                    AIResponse = aiResponse,
                    CreatedByUPN = userName,
                    CreatedDate = DateTime.UtcNow,
                    TokensUsed = tokensUsed,
                    ResponseTimeMs = responseTimeMs
                };

                _context.AIConversations.Add(conversation);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Conversation saved: ID={ConversationId}, Tokens={Tokens}, Time={Ms}ms",
                    conversation.ConversationID, tokensUsed, responseTimeMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save conversation to database");
                // Don't throw - AI response was successful, just logging failed
            }
        }
    }
}
