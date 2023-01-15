using System.ComponentModel.DataAnnotations.Schema;

namespace TinderButForBarteringBackend;

class Product
{
    public int Id { get; set; }
    [ForeignKey("User")]
    public string OwnerId { get; set; }
    //public string Category { get; set; }
    //public picture PrimaryPicture { get; set; }
    //public string Description { get; set; }
    //public picture[] AdditionalPictures { get; set; }
    //public bool IsSold { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public bool RequiresSomethingInReturn { get; set; }
}

class ProductWithPictureData : Product
{
    [NotMapped]
    public byte[] PrimaryPictureData { get; set; }
}
