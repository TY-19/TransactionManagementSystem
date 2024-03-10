using Microsoft.Extensions.Configuration;
using TMS.Application.Interfaces;

namespace TMS.Infrastructure.Data;

public class DbConnectionOptions(IConfiguration configuration) : IDbConnectionOptions
{
    public const string DefaultConnectionStringName = "Default";
    public string ConnectionString => configuration.GetConnectionString(DefaultConnectionStringName)
        ?? throw new ArgumentException("Database connection string is invalid");
}
