using FortiTrafficAnalysis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FortiTrafficAnalysis.Services.Recommendations
{
    public interface IPolicyRecommendationService
    {
        List<TrafficAnalysisRecommendation> AnalyzeLogs(
            List<TrafficLog> logs, 
            Guid trafficAnalysisId, 
            string createdByUPN);
    }

    public class PolicyRecommendationService : IPolicyRecommendationService
    {
        public List<TrafficAnalysisRecommendation> AnalyzeLogs(
            List<TrafficLog> logs, 
            Guid trafficAnalysisId, 
            string createdByUPN)
        {
            var recommendations = new List<TrafficAnalysisRecommendation>();

            if (logs == null || !logs.Any())
                return recommendations;

            // Group logs by action
            var deniedLogs = logs.Where(l => l.Action != null && 
                (l.Action.Equals("deny", StringComparison.OrdinalIgnoreCase) || 
                 l.Action.Contains("rst", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var acceptedLogs = logs.Where(l => l.Action != null && 
                l.Action.Equals("accept", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Analyze denied traffic patterns
            if (deniedLogs.Any())
            {
                var deniedPatterns = AnalyzeDeniedTraffic(deniedLogs);
                recommendations.AddRange(CreateRecommendations(
                    deniedPatterns, 
                    trafficAnalysisId, 
                    createdByUPN, 
                    "Denied Traffic Analysis"));
            }

            // Analyze accepted traffic for optimization
            if (acceptedLogs.Any())
            {
                var acceptedPatterns = AnalyzeAcceptedTraffic(acceptedLogs);
                recommendations.AddRange(CreateRecommendations(
                    acceptedPatterns, 
                    trafficAnalysisId, 
                    createdByUPN, 
                    "Accepted Traffic Optimization"));
            }

            // Summary recommendation
            if (recommendations.Any())
            {
                recommendations.Add(CreateSummaryRecommendation(
                    logs, 
                    deniedLogs, 
                    acceptedLogs, 
                    trafficAnalysisId, 
                    createdByUPN));
            }

            return recommendations;
        }

        private List<TrafficPattern> AnalyzeDeniedTraffic(List<TrafficLog> deniedLogs)
        {
            var patterns = new List<TrafficPattern>();

            // Group by source IP, destination IP, destination port, and protocol
            var grouped = deniedLogs
                .GroupBy(l => new
                {
                    SrcIP = l.SrcIP ?? "Unknown",
                    DstIP = l.DstIP ?? "Unknown",
                    DstPort = l.DstPort ?? "Any",
                    Proto = l.Proto ?? "Any",
                    Service = l.Service ?? "Unknown"
                })
                .OrderByDescending(g => g.Count())
                .Take(10); // Top 10 patterns

            foreach (var group in grouped)
            {
                var pattern = new TrafficPattern
                {
                    SourceIP = group.Key.SrcIP,
                    DestinationIP = group.Key.DstIP,
                    DestinationPort = group.Key.DstPort,
                    Protocol = group.Key.Proto,
                    Service = group.Key.Service,
                    Count = group.Count(),
                    Action = "deny",
                    SourceInterfaces = group.Select(l => l.SrcInt).Distinct().Where(i => i != null).ToList(),
                    DestinationInterfaces = group.Select(l => l.DstInt).Distinct().Where(i => i != null).ToList()
                };

                patterns.Add(pattern);
            }

            return patterns;
        }

        private List<TrafficPattern> AnalyzeAcceptedTraffic(List<TrafficLog> acceptedLogs)
        {
            var patterns = new List<TrafficPattern>();

            // Look for duplicate or overlapping policies
            var policyGroups = acceptedLogs
                .Where(l => !string.IsNullOrEmpty(l.PolicyId))
                .GroupBy(l => new
                {
                    PolicyId = l.PolicyId,
                    PolicyName = l.PolicyName ?? "Unnamed"
                })
                .Where(g => g.Count() > 100) // High volume policies
                .OrderByDescending(g => g.Count())
                .Take(5);

            foreach (var group in policyGroups)
            {
                var pattern = new TrafficPattern
                {
                    PolicyId = group.Key.PolicyId,
                    PolicyName = group.Key.PolicyName,
                    Count = group.Count(),
                    Action = "accept",
                    SourceIP = string.Join(", ", group.Select(l => l.SrcIP).Distinct().Take(3)),
                    DestinationIP = string.Join(", ", group.Select(l => l.DstIP).Distinct().Take(3))
                };

                patterns.Add(pattern);
            }

            return patterns;
        }

        private List<TrafficAnalysisRecommendation> CreateRecommendations(
            List<TrafficPattern> patterns,
            Guid trafficAnalysisId,
            string createdByUPN,
            string category)
        {
            var recommendations = new List<TrafficAnalysisRecommendation>();

            foreach (var pattern in patterns)
            {
                if (pattern.Action == "deny")
                {
                    recommendations.Add(CreateDeniedTrafficRecommendation(
                        pattern, 
                        trafficAnalysisId, 
                        createdByUPN));
                }
                else if (pattern.Action == "accept")
                {
                    recommendations.Add(CreateAcceptedTrafficRecommendation(
                        pattern, 
                        trafficAnalysisId, 
                        createdByUPN));
                }
            }

            return recommendations;
        }

        private TrafficAnalysisRecommendation CreateDeniedTrafficRecommendation(
            TrafficPattern pattern,
            Guid trafficAnalysisId,
            string createdByUPN)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"**Policy Recommendation: Allow Traffic**");
            sb.AppendLine();
            sb.AppendLine($"**Pattern Detected:** {pattern.Count} denied connection(s)");
            sb.AppendLine($"- Source IP: `{pattern.SourceIP}`");
            sb.AppendLine($"- Destination IP: `{pattern.DestinationIP}`");
            sb.AppendLine($"- Destination Port: `{pattern.DestinationPort}`");
            sb.AppendLine($"- Protocol: `{GetProtocolName(pattern.Protocol)}`");
            sb.AppendLine($"- Service: `{pattern.Service}`");
            sb.AppendLine();
            sb.AppendLine("**Recommended Action:**");
            sb.AppendLine("Create a firewall policy to allow this traffic if it's legitimate business traffic.");
            sb.AppendLine();
            sb.AppendLine("**Suggested Policy Configuration:**");
            sb.AppendLine("```");
            sb.AppendLine($"config firewall policy");
            sb.AppendLine($"    edit 0");
            sb.AppendLine($"    set name \"Allow_{pattern.Service}_{pattern.DestinationPort}\"");
            
            if (pattern.SourceInterfaces.Any())
                sb.AppendLine($"    set srcintf \"{pattern.SourceInterfaces.First()}\"");
            if (pattern.DestinationInterfaces.Any())
                sb.AppendLine($"    set dstintf \"{pattern.DestinationInterfaces.First()}\"");
            
            sb.AppendLine($"    set srcaddr \"all\"");
            sb.AppendLine($"    set dstaddr \"all\"");
            sb.AppendLine($"    set action accept");
            sb.AppendLine($"    set schedule \"always\"");
            sb.AppendLine($"    set service \"{pattern.Service}\"");
            sb.AppendLine($"    set logtraffic all");
            sb.AppendLine($"    next");
            sb.AppendLine($"end");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**Important:** Review and adjust source/destination addresses based on your security requirements.");

            return new TrafficAnalysisRecommendation
            {
                TrafficAnalysisRecommendationID = Guid.NewGuid(),
                TrafficAnalysisID = trafficAnalysisId,
                Title = $"Allow {pattern.Service} Traffic ({pattern.DestinationPort})",
                RecommendationText = sb.ToString(),
                AnalysisDetails = $"Analyzed {pattern.Count} denied connections. Pattern: {pattern.SourceIP} ? {pattern.DestinationIP}:{pattern.DestinationPort}",
                CreatedByUPN = createdByUPN,
                CreatedDate = DateTime.UtcNow
            };
        }

        private TrafficAnalysisRecommendation CreateAcceptedTrafficRecommendation(
            TrafficPattern pattern,
            Guid trafficAnalysisId,
            string createdByUPN)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"**Policy Optimization: High-Volume Policy**");
            sb.AppendLine();
            sb.AppendLine($"**Pattern Detected:** Policy '{pattern.PolicyName}' (ID: {pattern.PolicyId}) has high traffic volume");
            sb.AppendLine($"- Number of connections: {pattern.Count:N0}");
            sb.AppendLine($"- Source IPs: {pattern.SourceIP}");
            sb.AppendLine($"- Destination IPs: {pattern.DestinationIP}");
            sb.AppendLine();
            sb.AppendLine("**Recommended Actions:**");
            sb.AppendLine("1. Review this policy to ensure it's still necessary");
            sb.AppendLine("2. Consider splitting if it covers too many services");
            sb.AppendLine("3. Verify logging settings for performance impact");
            sb.AppendLine("4. Check if addresses can be consolidated into address groups");

            return new TrafficAnalysisRecommendation
            {
                TrafficAnalysisRecommendationID = Guid.NewGuid(),
                TrafficAnalysisID = trafficAnalysisId,
                Title = $"Optimize Policy: {pattern.PolicyName}",
                RecommendationText = sb.ToString(),
                AnalysisDetails = $"Policy {pattern.PolicyId} processed {pattern.Count:N0} connections",
                CreatedByUPN = createdByUPN,
                CreatedDate = DateTime.UtcNow
            };
        }

        private TrafficAnalysisRecommendation CreateSummaryRecommendation(
            List<TrafficLog> allLogs,
            List<TrafficLog> deniedLogs,
            List<TrafficLog> acceptedLogs,
            Guid trafficAnalysisId,
            string createdByUPN)
        {
            var sb = new StringBuilder();

            sb.AppendLine("## Traffic Analysis Summary");
            sb.AppendLine();
            sb.AppendLine($"**Total Logs Analyzed:** {allLogs.Count:N0}");
            sb.AppendLine($"- Accepted: {acceptedLogs.Count:N0} ({GetPercentage(acceptedLogs.Count, allLogs.Count)}%)");
            sb.AppendLine($"- Denied/Blocked: {deniedLogs.Count:N0} ({GetPercentage(deniedLogs.Count, allLogs.Count)}%)");
            sb.AppendLine();

            var topSources = allLogs
                .GroupBy(l => l.SrcIP)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            sb.AppendLine("**Top 5 Source IPs:**");
            foreach (var src in topSources)
            {
                sb.AppendLine($"- {src.Key}: {src.Count():N0} connections");
            }
            sb.AppendLine();

            var topDestinations = allLogs
                .GroupBy(l => l.DstIP)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            sb.AppendLine("**Top 5 Destination IPs:**");
            foreach (var dst in topDestinations)
            {
                sb.AppendLine($"- {dst.Key}: {dst.Count():N0} connections");
            }
            sb.AppendLine();

            var topPorts = allLogs
                .Where(l => !string.IsNullOrEmpty(l.DstPort))
                .GroupBy(l => l.DstPort)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            sb.AppendLine("**Top 5 Destination Ports:**");
            foreach (var port in topPorts)
            {
                sb.AppendLine($"- Port {port.Key}: {port.Count():N0} connections");
            }

            return new TrafficAnalysisRecommendation
            {
                TrafficAnalysisRecommendationID = Guid.NewGuid(),
                TrafficAnalysisID = trafficAnalysisId,
                Title = "Traffic Analysis Summary",
                RecommendationText = sb.ToString(),
                AnalysisDetails = $"Summary of {allLogs.Count:N0} traffic logs",
                CreatedByUPN = createdByUPN,
                CreatedDate = DateTime.UtcNow
            };
        }

        private string GetProtocolName(string proto)
        {
            return proto switch
            {
                "6" => "TCP (6)",
                "17" => "UDP (17)",
                "1" => "ICMP (1)",
                _ => proto
            };
        }

        private double GetPercentage(int count, int total)
        {
            if (total == 0) return 0;
            return Math.Round((double)count / total * 100, 1);
        }

        private class TrafficPattern
        {
            public string SourceIP { get; set; }
            public string DestinationIP { get; set; }
            public string DestinationPort { get; set; }
            public string Protocol { get; set; }
            public string Service { get; set; }
            public string PolicyId { get; set; }
            public string PolicyName { get; set; }
            public int Count { get; set; }
            public string Action { get; set; }
            public List<string> SourceInterfaces { get; set; } = new List<string>();
            public List<string> DestinationInterfaces { get; set; } = new List<string>();
        }
    }
}
