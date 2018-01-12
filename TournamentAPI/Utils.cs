using System.Security.Claims;

namespace TournamentAPI
{
    public static class Utils
    {
        public static string GetUserName(ClaimsPrincipal user)
        {
            string username = user.Identity.Name;
            return username.ToLower().Replace("mgsops\\", "");
        }
    }
}
