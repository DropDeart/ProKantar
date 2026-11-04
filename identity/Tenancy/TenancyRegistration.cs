using System.Security.Claims;
using Identity.Accounts;
using Identity.Configuration;
using Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Identity.Tenancy;

/// <summary>Kiracı yönetimi servis kayıtları.</summary>
public static class TenancyRegistration
{
    /// <summary>Şirket onaylayan platform yöneticisi politikası.</summary>
    public const string ApproverPolicy = "tenancy.approver";

    /// <summary>Servisler ve onaylayan politikası (roller config'ten).</summary>
    public static IServiceCollection AddTenancy(this IServiceCollection services, AuthOptions options)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITenantAdminGuard, TenantAdminGuard>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IMemberAdministrationService, MemberAdministrationService>();
        services.AddScoped<ITenantTokenResolver, TenantTokenResolver>();

        // Roller boş olamaz (auth.yaml doğrulaması); şirket onayı ve üye yönetiminde platform yöneticisini bu politika belirler.
        services.AddAuthorizationBuilder().AddPolicy(ApproverPolicy, policy => policy
            .AddAuthenticationSchemes(AuthSchemes.CookieOrBearer.Split(','))
            .RequireRole(options.MultiTenancy.ApproverRoles));
        return services;
    }

    /// <summary>İsteği yapan; platform yöneticisi = şirket onaylayan politika (üye olmadan da yönetebilir).</summary>
    public static async Task<TenantActor> GetTenantActorAsync(
        this IAuthorizationService authorization, UserManager<ApplicationUser> userManager, ClaimsPrincipal user)
        => new(userManager.GetRequiredUserId(user), (await authorization.AuthorizeAsync(user, ApproverPolicy)).Succeeded);
}
