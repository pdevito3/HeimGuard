namespace HeimGuard
{
    using AutoPolicy;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Builder for HeimGuard DI
    /// </summary>
    public class HeimGuardBuilder
    {
        public IServiceCollection Services { get; }

        public HeimGuardBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// Automatically maps authorization attributes to ASP.NET Core authorization policies that haven't already been
        /// added.
        /// </summary>
        public HeimGuardBuilder MapAuthorizationPolicies()
        {
            Services.AddAuthorizationCore();
            Services.AddTransient<IAuthorizationPolicyProvider, HeimGuardAuthorizationPolicyProvider>();

            return this;
        }

        /// <summary>
        /// Automatically checks user permissions when an authorization attribute is used. 
        /// </summary>
        public HeimGuardBuilder AutomaticallyCheckPermissions()
        {
            Services.AddHttpContextAccessor();
            Services.AddTransient<IAuthorizationHandler, PermissionHandler>();

            return this;
        }
    }
}

