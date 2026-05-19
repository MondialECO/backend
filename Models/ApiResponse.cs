namespace WebApp.Models
{
    /// <summary>
    /// Shared response envelope. Matches the existing per-controller
    /// { success, message, data } shape so the frontend contract is unchanged,
    /// and adds a traceId for correlating client-reported errors with logs.
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        /// <summary>Correlation id of the request. Null on success responses.</summary>
        public string? TraceId { get; set; }

        public static ApiResponse Ok(string message, object? data = null) =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse Error(string message, string? traceId = null, object? data = null) =>
            new() { Success = false, Message = message, Data = data, TraceId = traceId };
    }
}
