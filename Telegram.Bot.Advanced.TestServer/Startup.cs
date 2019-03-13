using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using TestServer;

namespace Telegram.Bot.Advanced.TestServer {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        // TODO Insert bot key
        public void ConfigureServices(IServiceCollection services) {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddTelegramHolder( new TelegramBotData[] {
                new TelegramBotData(new TelegramBotClient("XXXXXXXXXXXXXXXXXXXXXXXXXXX"),
                    new DispatcherBuilder<TestTelegramContext, TelegramTestController>(),
                    "test",
                    "/proxy/telegram")
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseTelegramRouting();
            //app.Run(async (context) => { await context.Response.WriteAsync("Hello World!"); });
            app.UseMvc();
        }
    }
}