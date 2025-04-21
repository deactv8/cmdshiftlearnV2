namespace CmdShiftLearn.Api.Models
{
    /// <summary>
    /// Settings for OpenAI API
    /// </summary>
    public class OpenAISettings
    {
        /// <summary>
        /// The API key for OpenAI
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// The model to use for OpenAI API calls (e.g., "gpt-3.5-turbo", "gpt-4")
        /// </summary>
        public string Model { get; set; } = "gpt-3.5-turbo";
        
        /// <summary>
        /// The temperature to use for OpenAI API calls (0.0 to 1.0)
        /// </summary>
        public float Temperature { get; set; } = 0.7f;
        
        /// <summary>
        /// The maximum number of retries for OpenAI API calls
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// The delay in milliseconds between retries for OpenAI API calls
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;
    }
}