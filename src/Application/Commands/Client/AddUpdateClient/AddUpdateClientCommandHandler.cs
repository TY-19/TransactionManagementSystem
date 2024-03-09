using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TMS.Application.Commands.Client.AddUpdateClient;

public class AddUpdateClientCommandHandler(
    IConfiguration configuration
    ) : IRequestHandler<AddUpdateClientCommand>
{
    public async Task Handle(AddUpdateClientCommand command, CancellationToken cancellationToken)
    {
        var parameters = new { command.Name, command.Email, command.Latitude, command.Longitude };

        var sql = @$"
			IF EXISTS(SELECT Id AS ClientId FROM Clients WHERE Email = @Email)
				BEGIN
					UPDATE Clients
					SET Name = @Name, Latitude = @Latitude, Longitude = @Longitude
					WHERE Email = @Email
				END
			ELSE
				BEGIN
					INSERT INTO Clients(Name, Email, Latitude, Longitude)
					VALUES (@Name, @Email, @Latitude, @Longitude)
				END
		";

        using var dbConnection = new SqlConnection(configuration.GetConnectionString("Default"));
        await dbConnection.ExecuteAsync(sql, parameters);
    }
}
