namespace SocialNetwork.Data.Models
{
    public class Hashtag
    {
        public int Id { get; set; }

        // Stored lowercase and without the '#'
        public string Name { get; set; } = string.Empty;

        public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
    }
}
