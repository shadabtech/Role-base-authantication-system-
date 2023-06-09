using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Graph;
using Azure.Identity;
using System;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace B2CAuthZ.Admin.WebApiHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("ApiAuthorization"));

            services.AddSingleton<GraphServiceClient>(sc =>
            {
                return new GraphServiceClient(new Azure.Identity.ClientSecretCredential(
                    Configuration.GetValue<string>("B2CGraphPrivilegedCredentials:TenantId"),
                    Configuration.GetValue<string>("B2CGraphPrivilegedCredentials:ClientId"),
                    Configuration.GetValue<string>("B2CGraphPrivilegedCredentials:ClientSecret")));
            });

            services.AddOptions<SwaggerUIClientAuthOptions>().Configure<IConfiguration>((opt, config) =>
            {
                config.GetSection("SwaggerUIClientAuthentication").Bind(opt);
            });

            services.AddOptions<OrganizationOptions>().Configure<IConfiguration>((opt, config) =>
            {
                config.GetSection("OrganizationOptions").Bind(opt);
            });

            // these are very important to stay scoped - as they are configured _per request_ with the orgId
            services.AddScoped<IUserRepository, OrganizationFilteredUserRepository>();
            services.AddScoped<IApplicationRepository, OrganizationFilteredApplicationRepository>();

            services.AddControllers().AddJsonOptions(x =>
            {
                // used to keep graph objects small
                x.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                x.JsonSerializerOptions.MaxDepth = 5;
            });

            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.ReportApiVersions = true;
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new QueryStringApiVersionReader(),
                    new HeaderApiVersionReader("api-version"));
            });
            // Add ApiExplorer to discover versions
            services.AddVersionedApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });
            services.AddEndpointsApiExplorer();

            // see 
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>();
            services.AddSwaggerGen();

            services.AddCors(options =>
            {
                options.AddPolicy(name: "DevCorsPolicy",
                    builder =>
                    {
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.AllowAnyOrigin();
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("DevCorsPolicy");
            }

            var apiVersionDescriptionProvider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
            app.UseSwagger();            
            app.UseSwaggerUI(c =>
                {
                    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                    {
                        c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                    }
                    c.RoutePrefix = string.Empty;
                    c.DefaultModelExpandDepth(1);
                    c.OAuthClientId(Configuration.GetValue<string>("SwaggerUIClientAuthentication:ClientId"));
                    c.OAuthScopes();
                    c.OAuthUsePkce();

                });
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
