namespace HeimGuard;

/// <summary>
/// Provides an abstraction for handling the role:permission mappings for a given user in am IEnumerable of strings. 
/// </summary>
public interface IUserPolicyHandler
{
    /// <summary>
    /// Returns an IEnumerable of strings that represents a distinct list of a given user's permissions.
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the list of peermissions for the current user..</returns>
    Task<IEnumerable<string>> GetUserPermissions();
}