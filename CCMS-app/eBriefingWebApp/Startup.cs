using eBriefingWebApp.Interfaces;
using eBriefingWebApp.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Owin;

namespace eBriefingWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IGetGraphUserService, GetGraphUserService>();

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 102400000;
            });

        }
    }
}


