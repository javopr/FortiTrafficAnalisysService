using System;

namespace FortiTrafficAnalysis.Domain.Entities
{
    /// <summary>
    /// Represents an AI conversation between user and Azure OpenAI
    /// Stores question/answer pairs for traffic analysis assistance
    /// Author: javier.morales@intwo.cloud
    /// </summary>
    public class AIConversation
    {
        /// <summary>
        /// Unique identifier for this conversation entry
        /// </summary>
        public Guid ConversationID { get; set; }

        /// <summary>
        /// Reference to the Traffic Analysis ticket
        /// </summary>
        public Guid TrafficAnalysisID { get; set; }

        /// <summary>
        /// User's question or prompt
        /// </summary>
        public string UserQuestion { get; set; } = string.Empty;

        /// <summary>
        /// AI-generated response
        /// </summary>
        public string AIResponse { get; set; } = string.Empty;

        /// <summary>
        /// UPN of the user who asked the question
        /// </summary>
        public string CreatedByUPN { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the question was asked
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Number of tokens used for this conversation (prompt + completion)
        /// </summary>
        public int? TokensUsed { get; set; }

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public int? ResponseTimeMs { get; set; }

        // Navigation property
        public TrafficAnalysis? TrafficAnalysis { get; set; }
    }
}
