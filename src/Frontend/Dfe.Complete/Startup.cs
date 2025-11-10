using Azure.Identity;
using Dfe.Complete.Application.Mappers;
using Dfe.Complete.Configuration;
using Dfe.Complete.Infrastructure;
using Dfe.Complete.Infrastructure.Security.Authorization;
using Dfe.Complete.Logging.Middleware;
using Dfe.Complete.Security;
using Dfe.Complete.StartupConfiguration;
using Dfe.Complete.Validators;
using GovUk.Frontend.AspNetCore;
using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Middlewares.CorrelationId;
using GovUK.Dfe.CoreLibs.Security.Antiforgery;
using GovUK.Dfe.CoreLibs.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Cypress;
using GovUK.Dfe.CoreLibs.Security.Enums;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using DataProtectionOptions = Dfe.Complete.Configuration.DataProtectionOptions;

namespace Dfe.Complete;

public class Startup
{
    private readonly TimeSpan _authenticationExpiration;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        _authenticationExpiration = TimeSpan.FromMinutes(int.Parse(Configuration["AuthenticationExpirationInMinutes"] ?? "60"));
    }

    private IConfiguration Configuration { get; }

    private IConfigurationSection GetConfigurationSectionFor<T>()
    {
        string sectionName = typeof(T).Name.Replace("Options", string.Empty);
        return Configuration.GetRequiredSection(sectionName);
    }

    private T GetTypedConfigurationFor<T>() where T : class, new()
    {
        var section = GetConfigurationSectionFor<T>();
        return section == null
            ? throw new InvalidOperationException($"Configuration section for {typeof(T).Name} not found.")
            : section.Get<T>() ?? new T();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddFeatureManagement();
        services.AddHealthChecks();

        services
            .AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/");

                // Routes
                options.Conventions.AddPageRoute("/Projects/EditProjectNote", "projects/{projectId}/notes/edit");

                // ...except explicitly anonymous/public areas
                options.Conventions.AllowAnonymousToFolder("/Public");
                options.Conventions.AllowAnonymousToFolder("/Errors");
            })
            .AddViewOptions(options =>
            {
                options.HtmlHelperOptions.ClientValidationEnabled = false;
            });

        SetupApplicationInsights(services);

        services
            .AddControllersWithViews()
            .AddMicrosoftIdentityUI()
            .AddCookieTempDataProvider(options =>
            {
                options.Cookie.Name = ".Complete.TempData";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                if (string.IsNullOrEmpty(Configuration["CI"]))
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                }
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddCustomAntiForgeryHandling(opts =>
            {
                opts.CheckerGroups = [new()
                {
                    TypeNames = [nameof(HasHeaderKeyExistsInRequestValidator), nameof(CypressRequestChecker)],
                    CheckerOperator = CheckerOperator.Or
                }];
            });

        services.AddControllers().AddMicrosoftIdentityUI();

        // Configure antiforgery AFTER all services are added
        ConfigureCustomAntiforgery(services);

        SetupDataProtection(services);

        services.AddApplicationInsightsTelemetry(Configuration);
        services.AddCompleteClientProject(Configuration);
        services.AddScoped<ICorrelationContext, CorrelationContext>();

        services.AddScoped(sp =>
            sp.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available."));

        services.AddSession(options =>
        {
            options.IdleTimeout = _authenticationExpiration;
            options.Cookie.Name = ".Complete.Session";
            options.Cookie.IsEssential = true;
            options.Cookie.HttpOnly = true;
            if (string.IsNullOrEmpty(Configuration["CI"]))
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            }
        });

        services.AddHttpContextAccessor();

        services.AddApplicationAuthorization(Configuration, CustomPolicies.PolicyCustomizations);

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "MultiAuth";
                options.DefaultAuthenticateScheme = "MultiAuth";

                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCypressMultiAuthentication();

        authenticationBuilder.AddMicrosoftIdentityWebApp(
            Configuration.GetSection("AzureAd"),
            OpenIdConnectDefaults.AuthenticationScheme
        );

        // Configure the primary auth cookie: login + access denied behaviour.
        ConfigureCookies(services);

        services.AddApplicationInsightsTelemetry(Configuration);

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        RegisterClients(services);

        services.AddGovUkFrontend();

        services.AddApplicationDependencyGroup(Configuration);
        services.AddInfrastructureDependencyGroup(Configuration);

        services.AddCustomClaimProvider<CustomDatabaseClaimsProvider>();

        // AutoMapper
        services.AddAutoMapper(typeof(AutoMapping));

        services.Configure<ExternalLinksOptions>(Configuration.GetSection(ExternalLinksOptions.Section));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Errors");
            app.UseHsts();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        app.UseSecurityHeaders(
            SecurityHeadersDefinitions.GetHeaderPolicyCollection(env.IsDevelopment())
                .AddXssProtectionDisabled()
                .AddCustomHeader("Cross-Origin-Opener-Policy", "same-origin")
        );

        app.UseStatusCodePagesWithReExecute("/Errors", "?statusCode={0}");

        app.UseHttpsRedirection();
        app.UseHealthChecks("/health");

        // For Azure AD redirect uri to remain https
        var forwardOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All,
            RequireHeaderSymmetry = false
        };
        forwardOptions.KnownNetworks.Clear();
        forwardOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardOptions);

        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
        });
    }

    private void ConfigureCookies(IServiceCollection services)
    {
        services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.LoginPath = "/sign-in";
                options.AccessDeniedPath = "/access-denied";
                options.Cookie.Name = ".Complete.Login";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = _authenticationExpiration;
                options.SlidingExpiration = true;
                if (string.IsNullOrEmpty(Configuration["CI"]))
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                }
            });
    }

    private void SetupApplicationInsights(IServiceCollection services) =>
        services.Configure<ApplicationInsightsOptions>(Configuration.GetSection("ApplicationInsights"));

    private void ConfigureCustomAntiforgery(IServiceCollection services)
    {
        services.AddCustomRequestCheckerProvider<HasHeaderKeyExistsInRequestValidator>();

        services.Configure<AntiforgeryOptions>(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = string.IsNullOrEmpty(Configuration["CI"])
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
    }

    private void RegisterClients(IServiceCollection services)
    {
        services.AddHttpClient("CompleteClient", (_, client) =>
        {
            var completeOptions = GetTypedConfigurationFor<CompleteOptions>();
            client.BaseAddress = new Uri(completeOptions.ApiEndpoint);
            client.DefaultRequestHeaders.Add("ApiKey", completeOptions.ApiKey);
        });

        services.AddHttpClient("AcademiesApiClient", (sp, client) =>
        {
            var academiesApiOptions = GetTypedConfigurationFor<AcademiesOptions>();
            client.BaseAddress = new Uri(academiesApiOptions.ApiEndpoint);
            client.DefaultRequestHeaders.Add("ApiKey", academiesApiOptions.ApiKey);
        });
    }

    private void SetupDataProtection(IServiceCollection services)
    {
        var dp = services.AddDataProtection();
        var options = GetTypedConfigurationFor<DataProtectionOptions>();

        var dpTargetPath = options?.DpTargetPath ?? @"/srv/app/storage";

        if (Directory.Exists(dpTargetPath))
        {
            dp.PersistKeysToFileSystem(new DirectoryInfo(dpTargetPath));

            // If a Key Vault Key URI is defined, expect to encrypt the keys.xml
            var kvProtectionKeyUri = options?.KeyVaultKey;

            if (!string.IsNullOrWhiteSpace(kvProtectionKeyUri))
            {
                dp.ProtectKeysWithAzureKeyVault(
                    new Uri(kvProtectionKeyUri),
                    new DefaultAzureCredential()
                );
            }
        }
    }
}
