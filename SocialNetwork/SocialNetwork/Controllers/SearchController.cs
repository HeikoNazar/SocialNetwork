using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data;
using SocialNetwork.Data.Helpers;
using SocialNetwork.Extensions;
using SocialNetwork.ViewModels.Search;

namespace SocialNetwork.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private const int MaxResults = 20;

        private readonly AppDBContext _context;

        public SearchController(AppDBContext context)
        {
            _context = context;
        }

        // "kyiv" searches accounts and hashtags, "#kyiv" only hashtags, "@nick" only accounts
        public async Task<IActionResult> Index(string? q)
        {
            int currentUserId = User.GetUserId();
            var query = (q ?? string.Empty).Trim();
            var model = new SearchVM { Query = query };

            var term = query.TrimStart('#', '@').Trim();
            if (term.Length == 0)
            {
                return View(model);
            }

            if (!query.StartsWith('#'))
            {
                model.Accounts = await _context.Users
                    .Where(u => !u.IsDeleted && (u.FullName.Contains(term) || u.UserHandle.Contains(term)))
                    .OrderBy(u => u.UserHandle.StartsWith(term) || u.FullName.StartsWith(term) ? 0 : 1)
                    .ThenBy(u => u.FullName)
                    .Take(MaxResults)
                    .Select(u => new SearchAccountVM
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        UserHandle = u.UserHandle,
                        ProfilePictureUrl = u.ProfilePictureUrl,
                        SubscribersCount = u.Subscribers.Count(),
                        IsSubscribed = u.Subscribers.Any(s => s.SubscriberId == currentUserId),
                        IsCurrentUser = u.Id == currentUserId
                    })
                    .ToListAsync();
            }

            if (!query.StartsWith('@'))
            {
                var tag = HashtagParser.Normalize(term);
                if (tag.Length > 0)
                {
                    // Only hashtags that still have public, visible posts
                    model.Hashtags = await _context.Hashtags
                        .Where(h => h.Name.Contains(tag))
                        .Select(h => new SearchHashtagVM
                        {
                            Name = h.Name,
                            PostsCount = h.PostHashtags.Count(ph => !ph.Post.IsDeleted && !ph.Post.IsPrivate && ph.Post.Reports.Count < 5)
                        })
                        .Where(h => h.PostsCount > 0)
                        .OrderByDescending(h => h.PostsCount)
                        .ThenBy(h => h.Name)
                        .Take(MaxResults)
                        .ToListAsync();
                }
            }

            return View(model);
        }
    }
}
