using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using Newtonsoft.Json.Linq;

namespace DSPlus.LethalQuestsBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var serviceProvier = services.BuildServiceProvider();
            var bot = new LethalQuestsBot(serviceProvier, _configuration);
            services.AddSingleton(bot);

            GlobalConfiguration.LoadConfiguration(_configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }

    public static class GlobalConfiguration
    {
        public static IConfiguration Configuration { get; private set; }

        public static void LoadConfiguration(IConfiguration config)
        {
            Configuration = config;
        }
    }
}