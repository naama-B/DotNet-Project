namespace Gym.Core.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    Conflict,
    Validation,
    Forbidden,
    Unauthorized
}

/// <summary>
/// Outcome of a service operation. The service layer never throws for expected business
/// failures; it returns a <see cref="Result"/> whose <see cref="Status"/> the API layer
/// maps to an HTTP status code.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public ResultStatus Status { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, ResultStatus status, string? error)
    {
        IsSuccess = isSuccess;
        Status = status;
        Error = error;
    }

    public static Result Success() => new(true, ResultStatus.Success, null);
    public static Result NotFound(string error) => new(false, ResultStatus.NotFound, error);
    public static Result Conflict(string error) => new(false, ResultStatus.Conflict, error);
    public static Result Validation(string error) => new(false, ResultStatus.Validation, error);
    public static Result Forbidden(string error) => new(false, ResultStatus.Forbidden, error);
    public static Result Unauthorized(string error) => new(false, ResultStatus.Unauthorized, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, ResultStatus status, string? error, T? value)
        : base(isSuccess, status, error) => Value = value;

    public static Result<T> Success(T value) => new(true, ResultStatus.Success, null, value);

    public static new Result<T> NotFound(string error) => new(false, ResultStatus.NotFound, error, default);
    public static new Result<T> Conflict(string error) => new(false, ResultStatus.Conflict, error, default);
    public static new Result<T> Validation(string error) => new(false, ResultStatus.Validation, error, default);
    public static new Result<T> Forbidden(string error) => new(false, ResultStatus.Forbidden, error, default);
    public static new Result<T> Unauthorized(string error) => new(false, ResultStatus.Unauthorized, error, default);
}
