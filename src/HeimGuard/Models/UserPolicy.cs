namespace HeimGuard.Models;

/// <summary>
/// The list of roles and permissions for a given user.
/// </summary>
public class UserPolicy
{
    public UserPolicy(IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        Roles = roles;
        Permissions = permissions;
    }

    public IEnumerable<string> Roles { get; set; }
    public IEnumerable<string> Permissions { get; set; }
}