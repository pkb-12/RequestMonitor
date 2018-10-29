using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataStore.Interface;
using DataStore.Implementation;
using RequestMonitor.Filters;

namespace RequestMonitor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Add global exception handler
            services.AddMvc()
                .AddMvcOptions(o => o.Filters.Add(typeof(GlobalExceptionHandler)));

            // Add necessary services for dependency injection
            services.AddSingleton(typeof(IItemStore), typeof(InMemoryStore));
            services.AddSingleton(Configuration);
        }
            
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
        }
    }
}
