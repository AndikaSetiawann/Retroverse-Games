using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RetroVerseGaming.Models
{
    public class Cart
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string CustomerId { get; set; } = string.Empty;

        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class CartItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string GameId { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}