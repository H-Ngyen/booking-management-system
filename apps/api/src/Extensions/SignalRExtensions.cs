using API.Hubs;

namespace API.Extensions;

public static class SignalRExtensions
{
    public static void AddSignalRInfrastructure(this WebApplicationBuilder builder, string frontendOrigin)
    {
        builder.Services.AddCors(options =>
            options.AddPolicy("web", policy => policy
                .WithOrigins(frontendOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        builder.Services.AddSignalR();
    }

    public static void MapSignalRHubs(this WebApplication app)
    {
        app.MapHub<BookingHub>(BookingHub.Route);
    }
}
