namespace HeimGuard
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    /// <summary>
    /// The handler that interacts with the policy handler to expose guard methods for your user permissions.
    /// </summary>
    public interface IHeimGuardClient
    {
        /// <summary>
        /// Checks if the user has a particular permission.
        /// </summary>
        /// <param name="permission"></param>
        /// <returns>A task that represents the asynchronous permission check. The task result contains
        /// the true if the user has the given permission and false if they do not.</returns>
        Task<bool> HasPermissionAsync(string permission);

        /// <summary>
        /// Guards against users without the given permission. Throws an Exception of type
        /// <see cref="TException"/> if the user does not have the given permission. Does nothing if the
        /// user has the given permission.
        /// </summary>
        /// <param name="permission">The name of the permission the user must have access to.</param>
        /// <typeparam name="TException">The Exception that should be thrown if a user does not have the given permission.</typeparam>
        /// <exception cref="TException">User does not have the given permission.</exception>
        Task MustHavePermission<TException>(string permission)
            where TException : Exception, new();
    }

    /// <inheritdoc />
    public class HeimGuardClient : IHeimGuardClient
    {
        private readonly IUserPolicyHandler _userPolicyHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeimGuardClient"/> class.
        /// </summary>
        public HeimGuardClient(IUserPolicyHandler userPolicyHandler)
        {
            _userPolicyHandler = userPolicyHandler;
        }

        /// <inheritdoc />
        public async Task<bool> HasPermissionAsync(string permission)
            => await _userPolicyHandler.HasPermission(permission);

        /// <inheritdoc />
        public async Task MustHavePermission<TException>(string permission) 
            where TException : Exception, new()
        {
            if (!await HasPermissionAsync(permission))
            {
                try
                {
                    throw Activator.CreateInstance(typeof(TException), permission) as TException;
                }
                catch (MissingMethodException)
                {
                    throw new TException();
                }
            }
        }
    }
}

