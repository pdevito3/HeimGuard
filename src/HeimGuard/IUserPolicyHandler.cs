namespace HeimGuard;

using Models;

/// <summary>
/// Provides an abstraction for handling the role:permission mappings for a given user in a <see cref="UserPolicy"/>. 
/// </summary>
public interface IUserPolicyHandler
{
    /// <summary>
    /// Returns a <see cref="UserPolicy"/> to track a distinct list of a given user's roles and permissions.
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="UserPolicy"/> for the current user..</returns>
    Task<UserPolicy> GetUserPolicy();
}