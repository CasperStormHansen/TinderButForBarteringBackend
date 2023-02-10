using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TinderButForBarteringBackend;

#nullable enable
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

public class User
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
    public string? PictureUrl { get; set; }
    public byte[]? Wishlist { get; set; }
    //[JsonIgnore]
    //public int[]? LastSwipingProductsBatch { get; set; }
}

public class Match_database
{
    [Key]
    public int Id { get; set; }
    public string UserId1 { get; set; }
    [ForeignKey("UserId1")]
    public virtual User User1 { get; set; }
    public string UserId2 { get; set; }
    [ForeignKey("UserId2")]
    public virtual User User2 { get; set; }

    public Match_database(string userId1, string userId2)
    {
        UserId1 = userId1;
        UserId2 = userId2;
    }
}

public class Message_database
{
    [Key]
    public int Id { get; set; }
    public int MatchId { get; set; }
    [ForeignKey("MatchId")]
    public virtual Match_database Match_Database { get; set; }
    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
    public string Content { get; set; }
    public DateTime DateTime { get; set; }

    public Message_database(int matchId, string userId, string content, DateTime dateTime)
    {
        MatchId = matchId;
        UserId = userId;
        Content = content;
        DateTime = dateTime;
    }
}

public class Match
{
    public int MatchId { get; set; }
    public string Name { get; set; }
    public string? PictureUrl { get; set; }
    public int[] OwnProductIds { get; set; }
    public Product[] ForeignProducts { get; set; }
    public Message[] Messages { get; set; }
    public Match(int matchId, string name, string? pictureUrl, int[] ownProductIds, Product[] foreignProducts, Message[] messages)
    {
        MatchId = matchId;
        Name = name;
        PictureUrl = pictureUrl;
        OwnProductIds = ownProductIds;
        ForeignProducts = foreignProducts;
        Messages = messages;
    }
}

public class Message
{
    public int MatchId { get; set; }
    public bool Own { get; set; }
    public string Content { get; set; }
    public DateTime? DateTime { get; set; }

    [JsonConstructor]
    public Message(int matchId, bool own, string content, DateTime? dateTime)
    {
        MatchId = matchId;
        Own = own;
        Content = content;
        DateTime = dateTime;
    }
}

public class OnLoginData
{
    public User User { get; set; }
    public Product[] OwnProducts { get; set; }
    public Product[] SwipingProducts { get; set; }
    public string[] Categories { get; set; }
    public Match[] Matches { get; set; }

    public OnLoginData(User user, Product[] ownProducts, Product[] item3, string[] categories, Match[] matches)
    {
        User = user;
        OwnProducts = ownProducts;
        SwipingProducts = item3;
        Categories = categories;
        Matches = matches;
    }
}

public class UserProductAttitude
{
    [Key]
    public int Id { get; set; }

    public string UserId { get; set; }
    [JsonIgnore]
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public int ProductId { get; set; }
    [JsonIgnore]
    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    public UserProductAttitude(string userId, int productId)
    {
        UserId = userId;
        ProductId = productId;
    }
}

public class DontShowTo : UserProductAttitude
{
    public DontShowTo(string userId, int productId) : base(userId, productId)
    {
    }
}
public class IsInterested : UserProductAttitude
{
    public IsInterested(string userId, int productId) : base(userId, productId)
    {
    }
}
public class WillPay : UserProductAttitude
{
    public WillPay(string userId, int productId) : base(userId, productId)
    {
    }
}
#nullable disable
