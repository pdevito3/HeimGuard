namespace HeimGuard
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an abstraction for handling the role:permission mappings for a given user in am IEnumerable of strings. 
    /// </summary>
    public interface IUserPolicyHandler
    {
        /// <summary>
        /// Returns an IEnumerable of strings that represents a distinct list of a given user's permissions.
        /// </summary>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the list of permissions for the current user.</returns>
        Task<IEnumerable<string>> GetUserPermissions();

        /// <summary>
        /// Returns a boolean value indicating whether the current user has the specified permission.
        /// </summary>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing a boolean indicating whether the current user has the specified permission.</returns>
        async Task<bool> HasPermission(string permission) => (await GetUserPermissions()).Contains(permission);
    }
}
