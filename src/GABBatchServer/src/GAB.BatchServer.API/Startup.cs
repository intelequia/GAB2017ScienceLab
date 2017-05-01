using System;
using System.IO;
using System.Reflection;
using GAB.BatchServer.API.Common;
using GAB.BatchServer.API.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;

namespace GAB.BatchServer.API
{
    /// <summary>
    /// Startup class
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Starttup constructor
        /// </summary>
        /// <param name="env"></param>
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            if (env.IsDevelopment())
            {
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
            Configuration = builder.Build();
        }

        /// <summary>
        /// Configuration 
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {

            // Register the IConfiguration instance which BatchServerOptions binds against.
            services.AddSingleton<IConfiguration>(Configuration);

            // Setup Application Insights
            services.AddApplicationInsightsTelemetry(Configuration);

            // Add framework services.
            services.AddMvc();

            if (Configuration.GetValue<bool>("BatchServer:RedirectToHttps"))
            {
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                });
            }

            // Configure Entity Framework
            services.AddDbContext<BatchServerContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("BatchServer"),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null);
                }));

            // Register the Swagger generator, defining one or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                var runtimeVersion = typeof(Startup)
                            .GetTypeInfo()
                            .Assembly
                            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                            .InformationalVersion;
                c.SwaggerDoc("v1", new Info
                {
                    Title = "GAB Science Lab Batch Server",
                    Version = runtimeVersion,
                    Description = "API to pull task batches and push results to the GAB Science Lab",
                    TermsOfService = "None",
                    Contact = new Contact
                    {
                        Name = "David Rodriguez (@davidjrh), Adonai Suárez (@adonaisg) and Martin Abbott (@martinabbott), for the Global Azure Bootcamp 2017 Science Lab running Sebastian Hidalgo's Star Formation History SELIGA algorithm at the Instituo de Astrofisica de Canarias",
                        Email = "",
                        Url = "http://global.azurebootcamp.net"
                    },
                    License = new License
                    {
                        Name = "Under MIT license",
                        Url = "https://en.wikipedia.org/wiki/MIT_License"
                    }
                });

                //Set the comments path for the swagger json and ui.
                var xmlfilePath = GetXmlCommentsPath();
                if (File.Exists(xmlfilePath))
                {
                    c.IncludeXmlComments(xmlfilePath);
                }

            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="context"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, BatchServerContext context)
        {
            // Setup logging
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            var logger = loggerFactory.CreateLogger("GAB.BatchServer.API");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();

            if (Configuration.GetValue<bool>("BatchServer:RedirectToHttps"))
            {
                // Setup rewriting
                var options = new RewriteOptions()
                            .AddRedirectToHttps();
                app.UseRewriter(options);
            }

            // Initialize database
            DbInitializer.Initialize(context, logger, env.IsDevelopment());

            // Initialize storage
            Storage.Initialize(Configuration, logger, env.IsDevelopment());

            // Initialize event hub pool
            EventHubs.Initialize(Configuration, logger);
                        
            // Setup swagger
            if (Configuration.GetValue<bool>("BatchServer:SwaggerEnabled"))
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GAB Science Lab Batch Server v1.0");
                });
            }
        }

        private static string GetXmlCommentsPath()
        {
            var assembly = typeof(Startup).GetTypeInfo().Assembly;
            return assembly.Location.Substring(0, assembly.Location.Length - 3) + "xml";
        }
    }
}
