using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TinderButForBarteringBackend;

class Product
{
    [Key]
    public int Id { get; set; }
    [ForeignKey("User")] // should something be added to indicate that it's the Id column of the User table it refers to?
    public string OwnerId { get; set; }
    public byte Category { get; set; }
    //public picture PrimaryPicture { get; set; }
    //public picture[] AdditionalPictures { get; set; }
    //public bool IsSold { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public bool RequiresSomethingInReturn { get; set; }

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
}

class ProductWithPictureData : Product
{
    [NotMapped]
    public byte[] PrimaryPictureData { get; set; }
}
