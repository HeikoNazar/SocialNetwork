using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetwork.Data.Services
{
    public class PostService : IPostService
    {
        private readonly AppDBContext _context;
        public PostService(AppDBContext context)
        {
            _context = context;
        }
        public async Task AddPostComentAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<Post> CreatePostAsync(Post post, IFormFile Image)
        {
            // Check and save the image
            if (Image != null && Image.Length > 0)
            {
                string rootFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (Image.ContentType.Contains("image"))
                {
                    string rootFolderPathImages = Path.Combine(rootFolderPath, "images/uploaded");
                    Directory.CreateDirectory(rootFolderPathImages);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);
                    string filePath = Path.Combine(rootFolderPathImages, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await Image.CopyToAsync(stream);

                    // Set the URL to the newPost object
                    post.ImageUrl = "/images/uploaded/" + fileName;
                }
            }
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
            return post;
        }

        // Posts the given user is allowed to see, with everything the post card needs
        private IQueryable<Post> VisiblePostsQuery(int userId)
        {
            return _context.Posts
                .Where(n => (!n.IsPrivate || n.UserId == userId) && n.Reports.Count < 5 && !n.IsDeleted)
                .Include(n => n.User)
                .Include(n => n.Comments).ThenInclude(c => c.User)
                .Include(n => n.Likes)
                .Include(n => n.Favorites)
                .ThenInclude(n => n.User)
                .Include(n => n.Reports);
        }

        public async Task<List<Post>> GetAllPostAsync(int userId)
        {
            var allPosts = await VisiblePostsQuery(userId)
                .OrderByDescending(n => n.DateCreated)
                .ToListAsync();
            return allPosts;
        }

        public async Task<List<Post>> GetSubscribedPostsAsync(int userId)
        {
            // Posts written by the profiles this user is subscribed to
            var subscribedToIds = _context.Subscriptions
                .Where(s => s.SubscriberId == userId)
                .Select(s => s.SubscribedToId);

            return await VisiblePostsQuery(userId)
                .Where(n => subscribedToIds.Contains(n.UserId))
                .OrderByDescending(n => n.DateCreated)
                .ToListAsync();
        }

        public async Task<List<Post>> GetSavedPostsAsync(int userId)
        {
            // Posts this user has added to favorites
            return await VisiblePostsQuery(userId)
                .Where(n => n.Favorites.Any(f => f.UserId == userId))
                .OrderByDescending(n => n.DateCreated)
                .ToListAsync();
        }

        public async Task<Post> GetPostByIdAsync(int postId)
        {
            var postDb = await _context.Posts
               .Include(n => n.User)
               .Include(n => n.Likes)
               .Include(n => n.Favorites)
               .Include(n => n.Comments).ThenInclude(n => n.User)
               .FirstOrDefaultAsync(n => n.Id == postId);

            return postDb;
        }

        public async Task RemovePostAsync(int postId)
        {
            var posDb = await _context.Posts.FirstOrDefaultAsync(n => n.Id == postId);

            if (posDb != null)
            {
                posDb.IsDeleted = true;
                _context.Posts.Update(posDb);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemovePostCommentAsync(int commentId)
        {
            var commentDb = await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId);

            if (commentId != null)
            {
                _context.Comments.Remove(commentDb);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReportPostAsync(int postId, int userId)
        {
            var newReport = new Report()
            {
                PostId = postId,
                UserId = userId,
                DateCreated = DateTime.UtcNow
            };
            await _context.Reports.AddAsync(newReport);
            await _context.SaveChangesAsync();
        }

        public async Task TogglePostFavoriteAsync(int postId, int userId)
        {
            // chek if the user has already favorited the post
            var favorite = await _context.Favorites.Where(n => n.PostId == postId && n.UserId == userId).FirstOrDefaultAsync();
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }
            else
            {
                var newFavorite = new Favorite()
                {
                    PostId = postId,
                    UserId = userId,
                };
                await _context.Favorites.AddAsync(newFavorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task TogglePostLikeAsync(int postId, int userId)
        {
            // chek if the user has already liked the post
            var like = await _context.Likes.Where(n => n.UserId == userId && n.PostId == postId).FirstOrDefaultAsync();
            if (like != null)
            {
                _context.Likes.Remove(like);
                await _context.SaveChangesAsync();
            }
            else
            {
                var newlike = new Like()
                {
                    PostId = postId,
                    UserId = userId
                };
                await _context.Likes.AddAsync(newlike);
                await _context.SaveChangesAsync();
            }
        }

        public async Task TogglePostVisibility(int postId, int userId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(n => n.Id == postId && n.UserId == userId);
            if (post != null)
            {
                post.IsPrivate = !post.IsPrivate;
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();
            }
        }
    }
}