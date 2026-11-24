namespace SocialNetwork.Data.Models
{
    public class Subscription
    {
        // The user who subscribes
        public int SubscriberId { get; set; }
        // The user being subscribed to
        public int SubscribedToId { get; set; }

        public DateTime DateCreated { get; set; }

        // Navigation properties
        public User Subscriber { get; set; }
        public User SubscribedTo { get; set; }
    }
}
