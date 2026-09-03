using System.Text;
using Hangfire.Dashboard;

namespace DevTasks.API
{
    public class HangfireAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var header = httpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic "))
            {
                httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire\"";
                httpContext.Response.StatusCode = 401;
                return false;
            }

            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..]))
                .Split(':', 2);

            var expectedUser = Environment.GetEnvironmentVariable("HANGFIRE_DASHBOARD_USER") ?? "admin";
            var expectedPass = Environment.GetEnvironmentVariable("HANGFIRE_DASHBOARD_PASSWORD") ?? "";

            return credentials.Length == 2 && credentials[0] == expectedUser && credentials[1] == expectedPass;
        }
    }
}