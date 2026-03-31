namespace Common
{
    namespace YourProject.Models
    {
        public class ServiceResponse<T>
        {
            public T? Data { get; set; }

            // Success/Failure indicator
            public bool Success { get; set; } = true;

            // User-friendly message for the UI
            public string Message { get; set; } = string.Empty;

            // Internal technical details or stack traces (optional)
            public string? TechnicalDetails { get; set; }

            // Metadata for logging or temp tracking
            public Dictionary<string, object> Metadata { get; set; } = new();

            // Helper methods for quick instantiation
            public static ServiceResponse<T> Ok(T data, string message = "Success")
                => new() { Data = data, Success = true, Message = message };

            public static ServiceResponse<T> Fail(string message, string? techDetails = null)
                => new() { Success = false, Message = message, TechnicalDetails = techDetails };
        }
    }
}
