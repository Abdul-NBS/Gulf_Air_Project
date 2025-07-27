using System.Security.Claims;
using System.Web;

namespace eBriefingWebApp.Helper
{
    public class UserHelper
    {
        public string GetUser()
        {
            string user = "";

            ClaimsPrincipal principal = HttpContext.Current.User as ClaimsPrincipal;
            if (principal != null)
            {
                user = principal.FindFirst(ClaimTypes.Name)?.Value;
            }

            return user;
        }


    }
}