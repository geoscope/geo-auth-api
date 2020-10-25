using System;
using System.Text;
using geo_auth_api.Interfaces;
using geo_auth_api.Middleware;
using geo_auth_api.Services;
using geo_auth_data.Hydrators;
using geo_auth_data.Interfaces;
using geo_auth_data.Repositories;
using geo_auth_shared.Helpers;
using geo_auth_shared.Interfaces;
using geo_auth_shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace geo_auth_api
{
    public class Startup
    {
        private bool isDevelopmentEnvironment = true;
        public IConfiguration configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            // custom jwt auth middleware
            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/api/health");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Retrieve App Settings:
            var appSettingsSection = configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            // Configure JWT:
            var key = Encoding.ASCII.GetBytes(appSettings.JwtSharedSecret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = !isDevelopmentEnvironment;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = appSettings.JwtValidateIssuer,
                    ValidateAudience = appSettings.JwtValidateAudience,
                    ValidateLifetime = appSettings.JwtValidateLifetime,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddControllers();
            services.AddHealthChecks();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = appSettings.CacheHost;
                options.InstanceName = appSettings.CacheInstanceName;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<ISecurityHelper, SecurityHelper>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IHydrator<geo_auth_data.models.domain.User, geo_auth_data.models.dto.User>, UserToUserDtoHydrator>();
        }
    }
}
