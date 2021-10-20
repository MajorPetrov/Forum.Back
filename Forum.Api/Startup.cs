using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;
using Firewall;
using ForumJV.Data;
using ForumJV.Data.Options;
using ForumJV.Data.Services;
using ForumJV.Data.Models;
using ForumJV.Services;
using ForumJV.WebSocket;
using ForumJV.Extensions;

namespace ForumJV
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Cette méthode est appelée par le runtime. Utilisez cette méthode pour ajouter des services au conteneur.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<ApplicationUser>(config =>
            {
                config.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
                config.SignIn.RequireConfirmedEmail = true;
            }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();

            // services.AddAntiforgery(options =>
            // {
            //     options.HeaderName = "X-XSRF-TOKEN"; // Angular's default header name for sending the XSRF token.
            //     options.SuppressXFrameOptionsHeader = true;
            //     options.Cookie.Name = "XSRF";
            //     options.Cookie.HttpOnly = true;
            //     options.Cookie.SameSite = SameSiteMode.Strict;
            // });

            services.Configure<IdentityOptions>(options => options.Password.RequireNonAlphanumeric = false);

            // En production, les fichiers Vue seront chargés à partir de ce répertoire
            services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/dist/spa");

            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "API du forum",
                    Version = "v1",
                    Description = "Documentation officielle de l'API du forum",
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                c.IncludeXmlComments(xmlPath);
            });

            services.AddControllers();
            services.AddOptions();
            services.AddMemoryCache();

            services.AddCors(options =>
            {
                options.AddPolicy(name: "VueCorsPolicy", builder =>
                 {
                     builder.WithOrigins("https://localhost:8080")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                 });
            });

            // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //     .AddJwtBearer(options =>
            //     {
            //         options.Authority = Configuration["Okta:Authority"];
            //         options.Audience = "api://default";
            //     });

            services.AddSignalR(hubOptions => hubOptions.EnableDetailedErrors = true);

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Forum";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.SlidingExpiration = true;
            });

            services.AddScoped<IForum, ForumService>();
            services.AddScoped<IPost, PostService>();
            services.AddScoped<IPostReply, PostReplyService>();
            services.AddScoped<IApplicationUser, UserService>();
            services.AddScoped<IBadge, BadgeService>();
            services.AddScoped<IPoll, PollService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IAccount, AccountService>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.Configure<CaptchaKeys>(Configuration.GetSection("CaptchaKeys"));
            services.Configure<AuthMessageSenderOptions>(Configuration.GetSection("AuthMessageSenderOptions"));
            services.Configure<ImgurKeys>(Configuration.GetSection("ImgurKeys"));
        }

        // Cette méthode est appelée par le runtime. Utilisez cette méthode pour configurer le pipeline de requêtes HTTP.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAntiforgery antiforgery)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            var rules = FirewallRulesEngine
                            .DenyAllAccess()
                            .ExceptFromCloudflare()
                            .ExceptFromCountryCodes()
                            .ExceptFromLocalhost();

            app.UseFirewall(rules);

            app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });

            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseCors(options => options.SetIsOriginAllowed(x => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIpRateLimiting();

            // app.Use(next => async context =>
            // {
            //     var tokens = antiforgery.GetAndStoreTokens(context);

            //     context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions() { HttpOnly = false });

            //     await next(context);
            // });

            // app.UseCookiePolicy(new CookiePolicyOptions { HttpOnly = HttpOnlyPolicy.Always });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller}/{action=Index}/{id?}");

                endpoints.MapHub<UserHub>("/UserHub", options => options.Transports = HttpTransportType.WebSockets);
            });

            app.UseSwagger(c => c.RouteTemplate = "docs/{documentName}/docs.json");

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/docs/v1/docs.json", "API du Forum v1");
                c.RoutePrefix = "";
            });
        }
    }
}
