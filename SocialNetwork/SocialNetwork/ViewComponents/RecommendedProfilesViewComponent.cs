using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data;
using SocialNetwork.Extensions;
using SocialNetwork.ViewModels.Home;

namespace SocialNetwork.ViewComponents
{
    public class RecommendedProfilesViewComponent : ViewComponent
    {
        private readonly AppDBContext _context;

        public RecommendedProfilesViewComponent(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int currentUserId = UserClaimsPrincipal.GetUserId();

            // The 3 newest users (highest Id = registered last), not counting the current user
            var profiles = await _context.Users
                .Where(u => !u.IsDeleted && u.Id != currentUserId)
                .OrderByDescending(u => u.Id)
                .Take(3)
                .Select(u => new RecommendedProfileVM
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    UserHandle = u.UserHandle,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    SubscribersCount = u.Subscribers.Count(),
                    IsSubscribed = u.Subscribers.Any(s => s.SubscriberId == currentUserId)
                })
                .ToListAsync();

            return View(profiles);
        }
    }
}
