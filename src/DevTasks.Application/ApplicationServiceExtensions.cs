using Microsoft.Extensions.DependencyInjection;
using DevTasks.Application.Interfaces;
using DevTasks.Application.Services;
using Mapster;
using MapsterMapper;
using MediatR;

namespace DevTasks.Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<ITokenService, TokenService>();

            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(typeof(ApplicationServiceExtensions).Assembly);
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();
            services.AddScoped<IOverdueTaskChecker, OverdueTaskChecker>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly));


            return services;
        }
    }
}