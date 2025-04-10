using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TestKB.Filters
{
    public class AuthenticationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var isAuthenticated = context.HttpContext.Session.GetString("IsAuthenticated");

            // Skip authentication for the Auth controller
            var controller = context.RouteData.Values["controller"]?.ToString();
            if (controller == "Auth")
            {
                return;
            }

            // Check if user is authenticated
            if (isAuthenticated != "true")
            {
                // Redirect to login page
                context.Result = new RedirectToActionResult("Login", "Auth", null);
            }
        }
    }
}