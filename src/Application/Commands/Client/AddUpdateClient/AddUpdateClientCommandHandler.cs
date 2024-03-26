using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using TMS.Application.Interfaces;

namespace TMS.Application.Commands.Client.AddUpdateClient;

public class AddUpdateClientCommandHandler(
    IDbConnectionOptions connectionOptions
    ) : IRequestHandler<AddUpdateClientCommand>
{
    public async Task Handle(AddUpdateClientCommand command, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            command.Name,
            command.Email,
            command.Latitude,
            command.Longitude
        };

        string sql = @$"
            MERGE INTO Clients
            USING (VALUES (@Name, @Email, @Latitude, @Longitude))
                AS source (Name, Email, Latitude, Longitude)
            ON Clients.Email = source.Email
            WHEN MATCHED THEN
                UPDATE SET Name = source.Name, Latitude = source.Latitude, Longitude = source.Longitude
            WHEN NOT MATCHED THEN
                INSERT (Email, Name, Latitude, Longitude)
                VALUES (source.Email, source.Name, source.Latitude, source.Longitude);
        ";

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        await dbConnection.ExecuteAsync(sql, parameters);
    }
}
