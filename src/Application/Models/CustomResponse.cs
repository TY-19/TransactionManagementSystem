namespace TMS.Application.Models;

public class CustomResponse
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class CustomResponse<T> : CustomResponse
{
    public T? Payload { get; set; }
}
