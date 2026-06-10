using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Auth;
using TaskManager.Application.Tasks;

namespace TaskManager.Application;

public static class DependencyInjection
{
    /// <summary>Registers business-logic services and their validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAuthService, AuthService>();

        // Register all FluentValidation validators in this assembly.
        services.AddValidatorsFromAssemblyContaining<TaskService>();

        return services;
    }
}
