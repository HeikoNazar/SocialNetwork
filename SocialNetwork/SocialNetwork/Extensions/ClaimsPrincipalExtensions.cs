using System.Security.Claims;

namespace SocialNetwork.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public const string UserHandleClaim = "user_handle";

        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("The current user is not signed in.");
            return int.Parse(value);
        }

        public static string GetFullName(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public static string GetUserHandle(this ClaimsPrincipal user) => user.FindFirstValue(UserHandleClaim) ?? string.Empty;
    }
}
