﻿using Dapper;
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

        using var dbConnection = new SqlConnection(connectionOptions.ConnectionString);
        await dbConnection.ExecuteAsync(sql, parameters);
    }
}
