using FortiTrafficAnalysis.Domain.Entities;

namespace FortiTrafficAnalysis.Services.AI
{
    /// <summary>
    /// Service interface for AI-powered traffic analysis recommendations
    /// Integrates with Azure OpenAI GPT-4.1 for intelligent policy suggestions
    /// Author: javier.morales@intwo.cloud
    /// </summary>
    public interface IAIRecommendationService
    {
        /// <summary>
        /// Ask a question about selected traffic logs using AI
        /// </summary>
        /// <param name="trafficAnalysisId">The traffic analysis ticket ID</param>
        /// <param name="userQuestion">The question from the user</param>
        /// <param name="selectedLogs">List of selected traffic logs for context</param>
        /// <param name="userName">UPN of the user asking the question</param>
        /// <returns>AI-generated response</returns>
        Task<string> AskQuestionAsync(
            Guid trafficAnalysisId,
            string userQuestion,
            List<TrafficLog> selectedLogs,
            string userName);

        /// <summary>
        /// Get conversation history for a specific traffic analysis ticket
        /// </summary>
        /// <param name="trafficAnalysisId">The traffic analysis ticket ID</param>
        /// <returns>List of previous conversations</returns>
        Task<List<AIConversation>> GetConversationHistoryAsync(Guid trafficAnalysisId);
    }
}
