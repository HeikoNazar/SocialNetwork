namespace SocialNetwork.ViewModels.Search
{
    public class SearchVM
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchAccountVM> Accounts { get; set; } = new();
        public List<SearchHashtagVM> Hashtags { get; set; } = new();
    }

    public class SearchAccountVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserHandle { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int SubscribersCount { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class SearchHashtagVM
    {
        public string Name { get; set; } = string.Empty;
        public int PostsCount { get; set; }
    }
}
