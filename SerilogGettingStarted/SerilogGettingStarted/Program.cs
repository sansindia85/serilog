using System.Timers;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

//Log.Logger = new LoggerConfiguration()
//    .WriteTo.Console(new JsonFormatter())
//    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
//    .CreateLogger();

//To address this, Serilog supports two-stage initialization. An initial "bootstrap" logger is
//configured immediately when the program starts, and this is replaced by the fully-configured
//logger once the host has loaded.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

//Log.Logger = new LoggerConfiguration()
//    .WriteTo.Debug()
//    .CreateLogger();

try
{
    Log.Information("Starting web application.");
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()); //Redirect all log events through Serilog pipeline.

    var app = builder.Build();


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging(options =>
    {
        // Customize the message template
        options.MessageTemplate = "Handled {RequestPath}";

        // Emit debug-level events instead of the defaults
        options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;

        // Attach additional properties to the request completion event
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });



    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

