namespace Portfolio.Application.Common;

public enum ResultStatus
{
    Ok,
    NotFound,
    Conflict,

    /// <summary>The request is well-formed but refers to data that does not exist (e.g. an unknown technology id).</summary>
    Invalid,
}

/// <summary>Outcome of a write operation that can fail for expected business reasons.</summary>
public sealed record Result<T>(ResultStatus Status, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new(ResultStatus.Ok, value, null);
    public static Result<T> NotFound() => new(ResultStatus.NotFound, default, null);
    public static Result<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
    public static Result<T> Invalid(string error) => new(ResultStatus.Invalid, default, error);
}

/// <summary>Outcome of a write operation without a payload.</summary>
public sealed record Result(ResultStatus Status, string? Error)
{
    public static readonly Result Success = new(ResultStatus.Ok, null);
    public static readonly Result Missing = new(ResultStatus.NotFound, null);
    public static Result Conflict(string error) => new(ResultStatus.Conflict, error);
}
