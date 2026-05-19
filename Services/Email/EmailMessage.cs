namespace WebApp.Services.Email
{
    /// <summary>A single email handed to the background sender.</summary>
    public record EmailMessage(string To, string Subject, string Body)
    {
        /// <summary>Retry attempt number (0 = first try).</summary>
        public int Attempt { get; init; }
    }
}
