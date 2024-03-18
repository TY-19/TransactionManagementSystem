namespace TMS.Application.Models;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets a message describing the result of the operation.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// Gets the list of errors that occurred during the operation.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    public OperationResult()
    {
    }
    public OperationResult(bool succeeded)
    {
        Succeeded = succeeded;
    }
    public OperationResult(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }
}

/// <summary>
/// Represents the result of an operation with an optional payload.
/// </summary>
/// <typeparam name="T">The type of payload included in the operation result.</typeparam>
public class OperationResult<T> : OperationResult
{
    /// <summary>
    /// Gets or sets the payload of the operation result.
    /// </summary>
    public T? Payload { get; set; }

    public OperationResult()
    {
    }
    public OperationResult(bool succeeded) : base(succeeded)
    {
    }
    public OperationResult(bool succeeded, string message) : base(succeeded, message)
    {
    }
    public OperationResult(bool succeeded, T? payload) : this(succeeded)
    {
        Payload = payload;
    }
}
