using System;
using System.Linq;
using EMSfIIoT.Services;
using System.Reflection;
using EMSfIIoT.Resources;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using EMSfIIoT.RouteModelConventions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;

using SharedAPI;
using EMSfIIoT_API.Models;
using Microsoft.IdentityModel.Logging;
using System.Text.Json.Serialization;

namespace EMSfIIoT
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
            new EMSfIIoTApiConnector(Configuration.GetSection("AzureAdB2C"));
            new BoschIoTSuiteApiConnector(Configuration.GetSection("BoschIoTSuite"));
            new GraphApiConnector(Configuration.GetSection("AzureAdB2C"));

            IdentityModelEventSource.ShowPII = true;

            services.TryAddSingleton<CommonLocalizationService>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
            .AddAzureADB2C(options =>
            {
                options.Instance = Configuration["AzureAdB2C:Instance"];
                options.ClientId = Configuration["AzureAdB2C:ClientId"];
                options.CallbackPath = Configuration["AzureAdB2C:CallbackPath"];
                options.Domain = Configuration["AzureAdB2C:Domain"];
                options.SignUpSignInPolicyId = Configuration["AzureAdB2C:SignUpSignInPolicyId"];
                options.ResetPasswordPolicyId = Configuration["AzureAdB2C:ResetPasswordPolicyId"];
                options.EditProfilePolicyId = Configuration["AzureAdB2C:EditProfilePolicyId"];
            })
            .AddCookie();

            services.Configure<OpenIdConnectOptions>(AzureADB2CDefaults.OpenIdScheme, options =>
            {
                string authority = Configuration["AzureAdB2C:Instance"] + "tfp/" + Configuration["AzureAdB2C:Domain"] + "/" + Configuration["AzureAdB2C:SignUpSignInPolicyId"] + "/v2.0";

                options.ClientId = Configuration["AzureAdB2C:ClientId"];
                options.Authority = authority;
                options.UseTokenLifetime = true;
                options.SaveTokens = true;
                options.CallbackPath = new PathString("/auth");
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ResponseMode = OpenIdConnectResponseMode.FormPost;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    NameClaimType = "name",
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async ctx =>
                    {
                        // Use MSAL to swap the code for an access token
                        // Extract the code from the response notification
                        var code = ctx.ProtocolMessage.Code;

                        await GraphApiConnector.UpdateAdministrators();

                        string signedInUserID = ctx.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;

                        IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder.Create(Configuration["AzureAdB2C:ClientId"])
                            .WithB2CAuthority(authority)
                            .WithClientSecret(Configuration["AzureAdB2C:ClientSecret"])
                            .Build();

                        var cache = new MSALStaticCache(signedInUserID, ctx.HttpContext).EnablePersistence(cca.UserTokenCache);

                        try
                        {
                            AuthenticationResult result = await cca.AcquireTokenByAuthorizationCode(options.Scope, code).ExecuteAsync();

                            if (result.AccessToken != null)
                                ctx.HttpContext.Response.Cookies.Append("ADB2CToken",
                                    result.AccessToken,
                                    new CookieOptions
                                    {
                                        Expires = result.ExpiresOn
                                    });

                            ctx.HandleCodeRedemption(result.AccessToken, result.IdToken);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    },

                    OnRedirectToIdentityProvider = ctx =>
                    {
                        var defaultPolicy = Configuration["AzureAdB2C:SignUpSignInPolicyId"];
                        
                        if (ctx.Properties.Items.TryGetValue("Policy", out var policy) && !policy.Equals(defaultPolicy))
                        {
                            ctx.ProtocolMessage.Scope = OpenIdConnectScope.OpenIdProfile;
                            ctx.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdToken;
                            ctx.ProtocolMessage.IssuerAddress = ctx.ProtocolMessage.IssuerAddress.ToLower().Replace(defaultPolicy.ToLower(), policy.ToLower());
                            ctx.Properties.Items.Remove(Configuration["AzureAdB2C:SignUpSignInPolicyId"]);
                        }
                        else if (!string.IsNullOrEmpty(Configuration["AzureAdB2C:Url"]))
                        {
                            ctx.ProtocolMessage.Scope += $" offline_access {Configuration["AzureAdB2C:Scopes"]}";
                            ctx.ProtocolMessage.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                        }

                        ctx.ProtocolMessage.UiLocales = ctx.HttpContext.Request.Query["culture"].ToString();

                        return Task.FromResult(0);
                    },

                    OnRemoteFailure = ctx =>
                    {
                        ctx.HandleResponse();

                        // Handle the error code that Azure AD B2C throws when trying to reset a password from the login page 
                        // because password reset is not supported by a "sign-up or sign-in policy"
                        if (ctx.Failure is OpenIdConnectProtocolException && ctx.Failure.Message.Contains("AADB2C90118"))
                        {
                            // If the user clicked the reset password link, redirect to the reset password route
                            ctx.Response.Redirect("/Account/ResetPassword");
                        }
                        else if (ctx.Failure is OpenIdConnectProtocolException && ctx.Failure.Message.Contains("access_denied"))
                        {
                            ctx.Response.Redirect("/");
                        }                       
                        else
                        {
                            ctx.Response.Redirect("/Error?message=" + Uri.EscapeDataString(ctx.Failure.Message));
                        }
                        return Task.FromResult(0);
                    },

                    OnRemoteSignOut = ctx =>
                    {
                        ctx.HttpContext.Response.Cookies.Delete("ADB2CToken");

                        return Task.FromResult(0);
                    }
                };
            });

            services
                .AddRazorPages(options =>
                {
                    options.Conventions.AuthorizeFolder("/bXi0nRT90v34snk");
                    options.Conventions.AuthorizeFolder("/Gateways");
                    options.Conventions.AuthorizeFolder("/Devices");
                    options.Conventions.AuthorizeFolder("/Events");
                    options.Conventions.AuthorizePage("/User");
                    options.Conventions.Add(new CultureTemplatePageRouteModelConvention());
                })
                .RemoveADB2CAccountController();


            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("pt-PT")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                
                options.RequestCultureProviders.Insert(0, new RouteDataRequestCultureProvider { Options = options });
            });

            services.AddMvc()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                {
                    var assemblyName = new AssemblyName(typeof(CommonResources).GetTypeInfo().Assembly.FullName);
                    return factory.Create(nameof(CommonResources), assemblyName.Name);
                };
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())); ;

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
            });

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            GraphApiConnector.UpdateAdministrators();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseSession();

            app.UseRouting();

            app.UseRequestLocalization(app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value);

            app.UseAuthentication();
            app.UseAuthorization();

            //app.UseRewriter(new RewriteOptions().AddRedirect("^$", "/en-US/Index"));


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }

    public static class B2CExtensions
    {
        public static IMvcBuilder RemoveADB2CAccountController(this IMvcBuilder builder)
        {
            var ADB2CAccountController = builder.PartManager.FeatureProviders.FirstOrDefault(fp => fp.ToString()
                .Equals("Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2CAccountControllerFeatureProvider"));
            if (ADB2CAccountController != null)
            {
                builder.PartManager.FeatureProviders.Remove(ADB2CAccountController);
            }
            return builder;
        }
    }
}