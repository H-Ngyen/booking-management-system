using API.Authorization;
using API.Interfaces;
using API.Interfaces.Authorization;
using API.Interfaces.Repositories;
using API.Interfaces.Services;
using API.Middlewares;
using API.Repositories;
using API.Services;

namespace API.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
    {
        // Middleware
        services.AddScoped<ErrorHandlingMiddleware>();

        //Services
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IServicesService, ServicesService>();
        services.AddScoped<IStaffsService, StaffsService>();
        services.AddScoped<IBookingsService, BookingsService>();


        //Repository
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IServicesRepository, ServicesRepository>();
        services.AddScoped<IStaffsRepository, StaffsRepository>();
        services.AddScoped<IWorkSchedulesRepository, WorkSchedulesRepository>();
        services.AddScoped<IBookingsRepository, BookingsRepository>();

        // Authorization
        services.AddScoped<IServicesAuthorization, ServicesAuthorization>();
        services.AddScoped<IStaffsAuthorization, StaffsAuthorization>();
        services.AddScoped<IBookingsAuthorization, BookingsAuthorization>();

        // SignalR
        services.AddSingleton<IBookingNotifier, BookingNotifier>();

        return services;
    }
}