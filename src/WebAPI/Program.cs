using Microsoft.EntityFrameworkCore;
using Serilog;
using TMS.Application;
using TMS.Application.Interfaces;
using TMS.Infrastructure.Data;
using TMS.WebAPI.Configuration;
using TMS.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("timezone-aliases.json", optional: true, reloadOnChange: true);

builder.Services.AddSingleton<IDbConnectionOptions, DbConnectionOptions>();
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(DbConnectionOptions.DefaultConnectionStringName)));

builder.Services.RegisterApplicationServices();

builder.Services.AddSerilog(new SerilogConfigureOptions(builder.Configuration).Configure);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<ExceptionHandler>();

builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<SwaggerConfigureOptions>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler(_ => { });

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    if (context.Database.IsRelational() && context.Database.GetPendingMigrations().Any())
    {
        await context.Database.MigrateAsync();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
