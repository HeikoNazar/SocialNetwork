using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SocialNetwork.Data;
using SocialNetwork.Data.Models;
using SocialNetwork.Data.Services;
using SocialNetwork.Extensions;
using SocialNetwork.ViewModels.Home;

namespace SocialNetwork.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDBContext _context;
    private readonly IPostService _postService;
    public HomeController(ILogger<HomeController> logger, AppDBContext context, IPostService postService)
    {
        _logger = logger;
        _context = context;
        _postService = postService;
    }

    public async Task<IActionResult> Index()
    {
        int loggeedInUser = User.GetUserId();
        var allPosts = await _postService.GetAllPostAsync(loggeedInUser);

        return View(allPosts);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost(PostVM post)
    {
        // Get the logged in user
        int loggedInUser = User.GetUserId();

        // Create a new post
        var newPost = new Post
        {
            Content = post.Content,
            DateCreated = DateTime.UtcNow,
            DateUpdated = DateTime.UtcNow,
            ImageUrl = "",
            NrOfReports = 0,
            UserId = loggedInUser
        };

        await _postService.CreatePostAsync(newPost, post.Image);

        // Redirect to the index page
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePostLike(PostLikeVM postLikeVM) 
    {
        int loggedInUser = User.GetUserId();
        await _postService.TogglePostLikeAsync(postLikeVM.PostId, loggedInUser);
        var post = await _postService.GetPostByIdAsync(postLikeVM.PostId);

        return PartialView("Home/_Post",post);
        
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePostFavorite(PostFavoriteVM postFavoriteVM)
    {
        int loggedInUser = User.GetUserId();
        await _postService.TogglePostFavoriteAsync(postFavoriteVM.PostId, loggedInUser);

        var post = await _postService.GetPostByIdAsync(postFavoriteVM.PostId);

        return PartialView("Home/_Post", post);

    }

    [HttpPost]
    public async Task<IActionResult> TogglePostVisibility(PostVisibilityVM postVisibilityVM)
    {
        int loggedInUser = User.GetUserId();
        await _postService.TogglePostVisibility(postVisibilityVM.PostId, loggedInUser);
        return RedirectToAction("Index");

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPostComment(PostCommentVM postCommentVM)
    {
        int loggedInUser = User.GetUserId();

        //Creat a post object
        var newComment = new Comment()
        {
            UserId = loggedInUser,
            PostId = postCommentVM.PostId,
            Content = postCommentVM.Content,
            DateCreated = DateTime.UtcNow,
            DateUpdated = DateTime.UtcNow
        };
        await _postService.AddPostComentAsync(newComment);

        var post = await _postService.GetPostByIdAsync(postCommentVM.PostId);
        return PartialView("Home/_Post", post);
    }

    [HttpPost]
    public async Task<IActionResult> AddPostReport(PostReportVM postReportVM)
    {
        int loggedInUser = User.GetUserId();

        await _postService.ReportPostAsync(postReportVM.PostId,loggedInUser);

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePostComment(RemoveCommentVM removeCommentVM)
    {
        await _postService.RemovePostCommentAsync(removeCommentVM.CommentId);
        var post = await _postService.GetPostByIdAsync(removeCommentVM.PostId);

        return PartialView("Home/_Post", post);
    }

    [HttpPost]
    public async Task<IActionResult> PostDelete(PostDeleteVM postDeleteVM)
    {
        await _postService.RemovePostAsync(postDeleteVM.PostId);
        return  RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubscription(int userId)
    {
        int currentUserId = User.GetUserId();

        // Can't subscribe to yourself or to a user that doesn't exist
        if (userId != currentUserId && await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted))
        {
            var existing = await _context.Subscriptions.FindAsync(currentUserId, userId);
            if (existing == null)
            {
                _context.Subscriptions.Add(new Subscription
                {
                    SubscriberId = currentUserId,
                    SubscribedToId = userId,
                    DateCreated = DateTime.UtcNow
                });
            }
            else
            {
                _context.Subscriptions.Remove(existing);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Double click: the other request already made the change, nothing more to do.
            }
        }

        // Called from JavaScript: return just the updated box. Otherwise go back to the home page.
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return ViewComponent("RecommendedProfiles");
        }

        return RedirectToAction("Index");
    }
}