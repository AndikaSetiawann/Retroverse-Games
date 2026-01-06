using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RetroVerseGaming.Models
{
	public class Toy
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string Id { get; set; } = string.Empty;

		public string Title { get; set; } = string.Empty;
		public string Platform { get; set; } = string.Empty; // reused as Variant/Type
		public string Publisher { get; set; } = string.Empty; // reused as Brand
		public string Developer { get; set; } = string.Empty; // reused as Manufacturer
		public string Genre { get; set; } = string.Empty; // reused as Category
		public decimal Price { get; set; }
		public int Stock { get; set; }
		public string Description { get; set; } = string.Empty;
		public string ImageUrl { get; set; } = string.Empty;
		public DateTime ReleaseDate { get; set; } // reused as LaunchDate
		public string Rating { get; set; } = string.Empty; // reused as AgeRating
		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}

