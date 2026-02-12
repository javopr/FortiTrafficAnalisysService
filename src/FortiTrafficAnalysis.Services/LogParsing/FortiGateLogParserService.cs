using FortiTrafficAnalysis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FortiTrafficAnalysis.Services.LogParsing
{
    /// <summary>
    /// Service for parsing FortiGate traffic log files
    /// </summary>
    public interface IFortiGateLogParserService
    {
        Task<List<TrafficLog>> ParseLogFileAsync(Stream fileStream, Guid trafficAnalysisId, Guid? fortiGateId = null);
    }

    /// <summary>
    /// Implementation of FortiGate log parser service
    /// Parses key=value format logs from FortiGate
    /// </summary>
    public class FortiGateLogParserService : IFortiGateLogParserService
    {
        public async Task<List<TrafficLog>> ParseLogFileAsync(Stream fileStream, Guid trafficAnalysisId, Guid? fortiGateId = null)
        {
            var logs = new List<TrafficLog>();

            using (var reader = new StreamReader(fileStream))
            {
                string line;
                int lineNumber = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var logEntry = ParseLogLine(line, trafficAnalysisId, fortiGateId);
                        if (logEntry != null)
                        {
                            logs.Add(logEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log parsing error but continue with other lines
                        Console.WriteLine($"Error parsing line {lineNumber}: {ex.Message}");
                    }
                }
            }

            return logs;
        }

        private TrafficLog? ParseLogLine(string line, Guid trafficAnalysisId, Guid? fortiGateId)
        {
            // Parse key=value pairs
            var fields = ParseKeyValuePairs(line);

            // Only process traffic logs
            if (!fields.ContainsKey("type") || fields["type"] != "traffic")
                return null;

            // Create TrafficLog entity
            var log = new TrafficLog
            {
                TrafficLogID = Guid.NewGuid(),
                TrafficAnalysisID = trafficAnalysisId,
                FGID = fortiGateId,
                RawLogLine = line,
                ImportedDate = DateTime.UtcNow
            };

            // Parse date and time
            if (fields.ContainsKey("date") && fields.ContainsKey("time"))
            {
                if (DateTime.TryParse($"{fields["date"]} {fields["time"]}", out DateTime logDateTime))
                {
                    log.LogDate = logDateTime.Date;
                    log.LogTime = fields["time"];
                }
            }

            // Parse required fields
            log.LogId = GetFieldValue(fields, "logid");
            log.SrcIP = GetFieldValue(fields, "srcip");
            log.SrcInt = GetFieldValue(fields, "srcintf");
            log.SrcPort = GetFieldValue(fields, "srcport");
            log.DstIP = GetFieldValue(fields, "dstip");
            log.DstInt = GetFieldValue(fields, "dstintf");
            log.DstPort = GetFieldValue(fields, "dstport");
            log.Proto = GetFieldValue(fields, "proto");
            log.PolicyId = GetFieldValue(fields, "policyid");
            log.Action = GetFieldValue(fields, "action");

            // Parse optional fields
            log.Service = GetFieldValue(fields, "service");
            log.SessionId = GetFieldValue(fields, "sessionid");
            log.PolicyName = GetFieldValue(fields, "policyname");

            // Parse numeric fields
            if (fields.ContainsKey("sentbyte") && long.TryParse(fields["sentbyte"], out long sentByte))
                log.SentByte = sentByte;

            if (fields.ContainsKey("rcvdbyte") && long.TryParse(fields["rcvdbyte"], out long rcvdByte))
                log.RcvdByte = rcvdByte;

            if (fields.ContainsKey("duration") && int.TryParse(fields["duration"], out int duration))
                log.Duration = duration;

            return log;
        }

        private Dictionary<string, string> ParseKeyValuePairs(string line)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Regex to match key=value or key="value with spaces"
            var regex = new Regex(@"(\w+)=(""[^""]*""|[^\s]+)");
            var matches = regex.Matches(line);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;

                    // Remove quotes if present
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    fields[key] = value;
                }
            }

            return fields;
        }

        private string? GetFieldValue(Dictionary<string, string> fields, string key)
        {
            return fields.ContainsKey(key) ? fields[key] : null;
        }
    }
}
