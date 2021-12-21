namespace HeimGuard
{
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The handler that interacts with the policy handler to expose guard methods for your user permissions.
    /// </summary>
    public interface IHeimGuard
    {
        /// <summary>
        /// Determines whether the user has a particular permission.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <returns></returns>
        Task<bool> HasPermissionAsync(string permission);
    }

    public class HeimGuard : IHeimGuard
    {
        private readonly IUserPolicyHandler _userPolicyHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeimGuard"/> class.
        /// </summary>
        public HeimGuard(IUserPolicyHandler userPolicyHandler)
        {
            _userPolicyHandler = userPolicyHandler;
        }

        public async Task<bool> HasPermissionAsync(string permission)
        {
            var policy = await _userPolicyHandler.GetUserPermissions();
            return policy.Contains(permission);
        }
    }
}

