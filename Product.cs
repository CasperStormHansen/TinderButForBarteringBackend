using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TinderButForBarteringBackend;

public class Product
{
    public static readonly string[] Categories = {
        "Dametøj str. S",
        "Dametøj str. M",
        "Dametøj str. L",
        "Herretøj str. S",
        "Herretøj str. M",
        "Herretøj str. L",
        "Bøger",
        "Elektronik",
        "Værktøj",
        "Sportsudstyr",
    };

    [Key]
    public int Id { get; set; }
    public string OwnerId { get; set; }
    [JsonIgnore]
    [ForeignKey("OwnerId")]
    public virtual User User { get; set; }
    public byte Category { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool RequiresSomethingInReturn { get; set; }
}

public class ProductWithPictureData : Product
{
    [NotMapped]
    public byte[] PrimaryPictureData { get; set; }
}
