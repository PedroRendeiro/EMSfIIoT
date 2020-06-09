using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

using EMSfIIoT_API.DbContexts;
using EMSfIIoT_API.Services;
using EMSfIIoT_API.Helpers;
using EMSfIIoT_API.Filters;
using EMSfIIoT_API.Models;
using EMSfIIoT_API.Tasks;
using EMSfIIoT_API.Hubs;
using SharedAPI;
using EMSfIIoT_API.SharedAPI;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Linq;

namespace EMSfIIoT_API
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
            new GraphApiConnector(Configuration.GetSection("AzureAdB2C"));
            new BoschIoTSuiteApiConnector(Configuration.GetSection("BoschIoTSuite"));
            new SendGridAPIConnector(Configuration.GetSection("SendGrid"));
            new TelegramAPIConnector(Configuration.GetSection("Telegram"));

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer("bearerAuth", jwtOptions =>
            {
                jwtOptions.Authority = Configuration["AzureAdB2C:Authority"];
                jwtOptions.Audience = Configuration["AzureAdB2C:ClientId"];
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name"
                };
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        await GraphApiConnector.UpdateUsers();
                    }
                };
            })
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("basicAuth", null);

            services.AddAuthorization(c =>
            {
                c.AddPolicy("read", p => p.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", new List<string>
                {
                    "read", "read write", "write read"
                }));
                c.AddPolicy("write", p => p.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", new List<string>
                {
                    "write", "read write", "write read"
                }));
            });

            services.AddScoped<IUserService, UserService>();

            services.AddDbContext<ApiDbContext>(options =>
            {
                options.UseSqlServer(
                    Configuration.GetConnectionString("database"),
                    providerOptions => providerOptions.EnableRetryOnFailure()                    
                );
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "EMSfIIoT API",
                    Version = "v1.0.0",
                    Description = "Energy Management Solution for Industrial IoT"
                });
                c.EnableAnnotations();
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "EMSfIIoT_API.xml"));

                c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(Configuration["AzureAdB2C:AuthorizationUrl"], UriKind.Absolute),
                            Scopes = new Dictionary<string, string>
                            {
                                {"https://emsfiiot.onmicrosoft.com/EMSfIIoT_API/read","Read permissions"},
                                {"https://emsfiiot.onmicrosoft.com/EMSfIIoT_API/write","Write permissions"}
                            }
                        }
                    }
                });
                c.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Description = "Please enter your username and password",
                    Scheme = "basic",
                    In = ParameterLocation.Header
                });

                c.OperationFilter<AuthenticationRequirementOperationsFilter>();
                c.OperationFilter<DefaultResponseOperationsFilter>();
            });

            services.AddControllers(options =>
            {
                options.RespectBrowserAcceptHeader = true;
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddHostedService<NotificationTask>();
            services.AddHostedService<TelegramTask>();

            services.AddSignalR();

            services.AddCors(options =>
            {
                options.AddPolicy(name: "EMSfIIoT",
                    builder =>
                    {
                        builder.WithOrigins(
                            "https://localhost:5001",
                            "https://emsfiiot.azurewebsites.net"
                            );
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.AllowCredentials();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApiDbContext db)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            } else
            {
                app.UseHttpsRedirection();
            }

            app.UseStatusCodePages(async context =>
            {

                Response response = new Response
                {
                    StatusCode = context.HttpContext.Response.StatusCode
                };

                switch (response.StatusCode)
                {
                    case 400:
                        response.Error = "request.error";
                        response.Message = "There was an error with your request!";
                        response.Description = "Please check your request.";
                        break;
                    case 401:
                        response.Error = "authentication.failed";
                        response.Message = "The credentials were missing or the provided credentials could not be authenticated!";
                        response.Description = "Please provide valid credentials.";
                        break;
                    case 403:
                        response.Error = "authentication.failed";
                        response.Message = "The credentials provided don't allow acess to this resource!";
                        response.Description = "Please provide valid credentials or check the authentication scopes.";
                        break;
                    case 404:
                        response.Error = "not.found";
                        response.Message = "The endpoint requested doesn't exist!";
                        response.Description = "Please check the URI.";
                        break;
                    case 405:
                        response.Error = "method.not.allowed";
                        response.Message = "The endpoint requested doesn't accept the requested method!";
                        response.Description = context.HttpContext.Request.Method + " not allowed for this endpoint. Please check the HTTP Method.";
                        break;
                    case 406:
                        response.Error = "not.acceptable";
                        response.Message = "The endpoint requested doesn't produce the requested media type!";
                        response.Description = "Please check the HTTP Accept Header.";
                        break;
                    case 415:
                        response.Error = "unsupported.media.type";
                        response.Message = "The endpoint requested doesn't accept the requested media type!";
                        response.Description = "Please check the HTTP Content-Type Header.";
                        break;
                    default:
                        response.Error = "internal.server.error";
                        response.Message = "An Internal Server Error occurred!";
                        response.Description = "Please check your request.";
                        break;
                }

                string s = JsonSerializer.Serialize(response);

                context.HttpContext.Response.ContentLength = s.Length;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(s), 0, s.Length);
            });

            db.Database.Migrate();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer
                        {
                            Url = $"{httpReq.Scheme}://{httpReq.Host.Value}",
                            //Description = "Azure",
                        }
                    };
                });

                c.RouteTemplate = "{documentName}/openApi.json";
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "";
                c.SwaggerEndpoint("/v1/openApi.json", "EMSfIIoT v1");
                c.SwaggerEndpoint("https://measures-api.azurewebsites.net/v1/openApi.json", "Measures v1");
                c.DocumentTitle = "API Documentation";
                c.EnableFilter();

                c.InjectJavascript("/custom.js", "text/javascript");

                c.OAuthClientId(Configuration["AzureAdB2C:ClientId"]);
                c.OAuthClientSecret(Configuration["AzureAdB2C:ClientSecret"]);
                c.OAuthAppName(Configuration["AzureAdB2C:AppName"]);
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
                {
                    {"prompt", "login"},
                    {"nonce", "defaultNonce"}
                });
                c.OAuthScopeSeparator(" ");
                c.OAuthUsePkce();

                c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
                c.DefaultModelExpandDepth(1);
                c.DefaultModelsExpandDepth(-1);

                c.DisplayRequestDuration();
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.EnableDeepLinking();

                c.EnableValidator();

                c.InjectStylesheet("/static/css/swaggerUI.css");
            });

            app.UseRouting();

            app.UseCors("EMSfIIoT");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationsHub>("/NotificationsHub");
            });
        }
    }
}