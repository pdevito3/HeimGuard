namespace HeimGuard;

using Microsoft.Extensions.DependencyInjection;

public static class HeimGuardServiceRegistration
{
    /// <summary>
    /// Adds HeimGuard service to apply permissions based on the current user's policy.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns> <see cref="HeimGuardBuilder"/> </returns>
    public static HeimGuardBuilder AddHeimGuard<TUserPolicyHandler>(this IServiceCollection services)
        where TUserPolicyHandler : class, IUserPolicyHandler
    {
        services.AddScoped<IUserPolicyHandler, TUserPolicyHandler>();
        services.AddTransient<IHeimGuardClient, HeimGuardClient>();

        return new HeimGuardBuilder(services);
    }
}