using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data;
using SocialNetwork.ViewModels.Home;

namespace SocialNetwork.ViewComponents
{
    public class PopularHashtagsViewComponent : ViewComponent
    {
        private const int HowManyToShow = 5;

        private readonly AppDBContext _context;

        public PopularHashtagsViewComponent(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Most used hashtags, counting only public posts that are still visible
            var hashtags = await _context.Hashtags
                .Select(h => new PopularHashtagVM
                {
                    Name = h.Name,
                    PostsCount = h.PostHashtags.Count(ph => !ph.Post.IsDeleted && !ph.Post.IsPrivate && ph.Post.Reports.Count < 5)
                })
                .Where(h => h.PostsCount > 0)
                .OrderByDescending(h => h.PostsCount)
                .ThenBy(h => h.Name)
                .Take(HowManyToShow)
                .ToListAsync();

            return View(hashtags);
        }
    }
}
