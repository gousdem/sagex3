using SageX3WebApi.SageX3SoapClient;

namespace SageX3WebApi.Models;

/// <summary>
/// Standard envelope returned by every endpoint.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<SageMessage>? Messages { get; init; }

    public static ApiResponse<T> Ok(T data, IReadOnlyList<SageMessage>? messages = null) =>
        new() { Success = true, Data = data, Messages = messages };

    public static ApiResponse<T> Fail(string error, IReadOnlyList<SageMessage>? messages = null) =>
        new() { Success = false, Error = error, Messages = messages };
}
