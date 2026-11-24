namespace SocialNetwork.ViewModels.Home
{
    public class RecommendedProfileVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserHandle { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int SubscribersCount { get; set; }
        public bool IsSubscribed { get; set; }
    }
}
