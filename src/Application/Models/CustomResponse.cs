namespace TMS.Application.Models;

public class CustomResponse
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public CustomResponse()
    {
    }
    public CustomResponse(bool succeeded)
    {
        Succeeded = succeeded;
    }
    public CustomResponse(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }
}

public class CustomResponse<T> : CustomResponse
{
    public T? Payload { get; set; }

    public CustomResponse()
    {
    }
    public CustomResponse(bool succeeded) : base(succeeded)
    {
    }

    public CustomResponse(bool succeeded, string message) : base(succeeded, message)
    {
    }

    public CustomResponse(bool succeeded, T? payload) : this(succeeded)
    {
        Payload = payload;
    }
}
