using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.TestServer.SeedData {
    public static class SeedDataExtensions {
        public static IApplicationBuilder SeedData(this IApplicationBuilder app) {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetService<TestTelegramContext>();
 
                new DataSeeder(context).SeedData();
            }

            return app;
        }
    }
}