using Core.Interfaces;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Infrastracture.Loggers;
using Infrastracture.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastracture
{
    public static class DependencyInjectionInfra
    {
        public static void AddInfrastructure(this IServiceCollection services)
        {
            SetConnection();

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase-config.json"),
                ProjectId = "ADD YOUR CREDENTIALS",
            });;

            services.AddHostedService<CommiterHostedService>();
            services.AddSingleton<ILoggerHelper, LoggerHelper>();
        }



        private static void SetConnection()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-config.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
        }
    }


}
