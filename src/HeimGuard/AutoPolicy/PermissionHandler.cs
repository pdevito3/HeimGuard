namespace HeimGuard.AutoPolicy;

using Microsoft.AspNetCore.Authorization;

internal class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string name)
    {
        Name = name;
    }
    public string Name { get; private set; }
}

internal class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHeimGuardClient _guardClient;

    public PermissionHandler(IHeimGuardClient guardClient)
    {
        _guardClient = guardClient;
    }

    /// <summary>
    /// Uses HeimGuard permission check to automatically confirm that a user has the appropriate permissions given a particular context.
    /// </summary>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // no cancellation token available on AuthorizationHandler: https://github.com/aspnet/Security/issues/1598
        if (await _guardClient.HasPermissionAsync(requirement.Name))
        {
            context.Succeed(requirement);
        }
    }
}