using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Security;

namespace eBriefingWebApp.Controllers
{
    public class CustomRoleProvider : RoleProvider
    {
        public override string ApplicationName
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            var identity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var roles = identity.Claims
                                    .Where(c => c.Type == ClaimTypes.Role && c.Issuer == "Authority_Issuer") // Adjust the issuer if needed
                                    .Select(c => c.Value)
                                    .ToArray();
                return roles;
            }
            return new string[] { };
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            var identity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                return identity.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleName && c.Issuer == "Authority_Issuer"); // Adjust the issuer if needed
            }
            return false;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            var identity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                return identity.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleName && c.Issuer == "Authority_Issuer"); // Adjust the issuer if needed
            }
            return false;
        }
    }
}