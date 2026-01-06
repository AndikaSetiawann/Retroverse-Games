using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RetroVerseGaming.Models
{
    public class Game
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // PlayStation, Xbox, Nintendo Switch, PC
        public string Publisher { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty; // Action, RPG, Sports, Racing, etc.
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty; // URL to download the game installer
        public DateTime ReleaseDate { get; set; }
        public string Rating { get; set; } = string.Empty; // E, T, M, etc.
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}