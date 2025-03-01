using Core.Interfaces.UseCases;
using Core.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Core
{
    public static class DependencyInjectionCore
    {
        public static void AddCore(this IServiceCollection services)
        {
            services.AddScoped<ICreateCommitUseCase, CreateCommitUseCase>();
        }
    }
}
