using SocialNetwork.Data.Models;

namespace SocialNetwork.ViewModels.Account
{
    public class ProfileVM
    {
        public string FullName { get; set; } = string.Empty;
        public string UserHandle { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int SubscribersCount { get; set; }
        public int SubscriptionsCount { get; set; }
        public List<Post> Posts { get; set; } = new();
    }
}
