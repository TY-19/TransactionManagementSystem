using MediatR;

namespace TMS.Application.Commands.Client.AddUpdateClient;

public class AddUpdateClientCommand : IRequest
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
