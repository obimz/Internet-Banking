namespace Internet_Banking.DTOs.Trasactions;

public record ServiceResult(
    bool IsSuccess,
    int StatusCode,
    string? ErrorMessage = null,
    object? Data = null);