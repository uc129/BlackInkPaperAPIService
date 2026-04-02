namespace Common
{
    namespace YourProject.Models
    {
        public class ServiceResponse<T>
        {
            public T? Data { get; set; }

            // Success/Failure indicator
            public bool Success { get; set; } = true;

            // Recommended HTTP status code for controller translation
            public int StatusCode { get; set; } = 200;

            // Stable machine-readable error code
            public string? ErrorCode { get; set; }

            // User-friendly message for the UI
            public string Message { get; set; } = string.Empty;

            // Internal technical details or stack traces (optional)
            public string? TechnicalDetails { get; set; }

            // Metadata for logging or temp tracking
            public Dictionary<string, object> Metadata { get; set; } = new();

            // Helper methods for quick instantiation
            public static ServiceResponse<T> Ok(T data, string message = "Success", int statusCode = 200)
                => new() { Data = data, Success = true, Message = message, StatusCode = statusCode };

            public static ServiceResponse<T> Fail(string message, string? techDetails = null, int statusCode = 400, string? errorCode = null)
                => new() { Success = false, Message = message, TechnicalDetails = techDetails, StatusCode = statusCode, ErrorCode = errorCode };
        }
    }
}
