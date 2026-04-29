namespace BlackInkPaperAdmin;

public sealed record ServiceResponse<T>(bool Success, int StatusCode, string? ErrorCode, string Message, T? Data);
public sealed record ProblemDetailsDto(string? Title, string? Detail, int? Status);
