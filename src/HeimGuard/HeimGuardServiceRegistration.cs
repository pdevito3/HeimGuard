namespace HeimGuard;

using Microsoft.Extensions.DependencyInjection;

public static class HeimGuardServiceRegistration
{
    /// <summary>
    /// Adds HeimGuard service to apply permissions based on the current user's policy.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns></returns>
    public static HeimGuardBuilder AddHeimGuard(this IServiceCollection services)
    {
        services.AddTransient<IHeimGuard, HeimGuard>();

        return new HeimGuardBuilder(services);
    }
}