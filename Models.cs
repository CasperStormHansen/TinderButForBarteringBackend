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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string OwnerId { get; set; }
    [JsonIgnore]
    [ForeignKey("OwnerId")]
    public virtual User User { get; set; }
    public byte Category { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public bool RequiresSomethingInReturn { get; set; }
#nullable enable
    [JsonIgnore]
    public DateTime? UpdateTime { get; set; }
#nullable disable
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
#nullable enable
    public string? PictureUrl { get; set; }
    public byte[]? Wishlist { get; set; }
#nullable disable
}

public class Match_database
{
    [Key]
    public int Id { get; set; }
    public DateTime CreationTime { get; set; }
    public string UserId1 { get; set; }
    [ForeignKey("UserId1")]
    public virtual User User1 { get; set; }
    public string UserId2 { get; set; }
    [ForeignKey("UserId2")]
    public virtual User User2 { get; set; }

    public Match_database(string userId1, string userId2)
    {
        CreationTime = DateTime.Now;
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
#nullable enable
    public string? PictureUrl { get; set; }
#nullable disable
    public int[] OwnProductIds { get; set; }
    public Product[] ForeignProducts { get; set; }
    public Message[] Messages { get; set; }
#nullable enable
    public Match(int matchId, string name, string? pictureUrl, int[] ownProductIds, Product[] foreignProducts, Message[] messages)
    {
        MatchId = matchId;
        Name = name;
        PictureUrl = pictureUrl;
        OwnProductIds = ownProductIds;
        ForeignProducts = foreignProducts;
        Messages = messages;
    }
#nullable disable
}

public class Message
{
    public int MatchId { get; set; }
    public bool Own { get; set; }
    public string Content { get; set; }
#nullable enable
    public DateTime? DateTime { get; set; }

    [JsonConstructor]
    public Message(int matchId, bool own, string content, DateTime? dateTime)
    {
        MatchId = matchId;
        Own = own;
        Content = content;
        DateTime = dateTime;
    }
#nullable disable
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

public class OnReconnectionData
{
    public Match[] NewMatches { get; set; }
    public Product[] UpdatedForeignProducts { get; set; }
    public MatchIdAndProductId[] NewInterestsInOwnProducts { get; set; }
    public Message[] NewMessages { get; set; }

    public OnReconnectionData(Match[] newMatches, Product[] updatedForeignProducts, MatchIdAndProductId[] newInterestsInOwnProducts, Message[] newMessages)
    {
        NewMatches = newMatches;
        UpdatedForeignProducts = updatedForeignProducts;
        NewInterestsInOwnProducts = newInterestsInOwnProducts;
        NewMessages = newMessages;
    }
}

public class UserProductAttitude
{
    [Key]
    public int Id { get; set; }
    public DateTime CreationTime { get; set; }

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
        CreationTime = DateTime.Now;
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

public class OnSwipeData
{
    public UserProductAttitude UserProductAttitude { get; set; }
#nullable enable
    public int[]? RemainingSwipingProductIds { get; set; }

    [JsonConstructor]
    public OnSwipeData(UserProductAttitude userProductAttitude, int[]? remainingSwipingProductIds)
    {
        UserProductAttitude = userProductAttitude;
        RemainingSwipingProductIds = remainingSwipingProductIds;
    }
#nullable disable
}

public class OnRefreshMainpageData
{
    public string UserId { get; set; }
    public int[] RemainingSwipingProductIds { get; set; }

    [JsonConstructor]
    public OnRefreshMainpageData(string userId, int[] remainingSwipingProductIds)
    {
        UserId = userId;
        RemainingSwipingProductIds = remainingSwipingProductIds;
    }
}

public class UserAndLastUpdate
{
    public string UserId { get; set; }
    public DateTime LastUpdate { get; set; }

    [JsonConstructor]
    public UserAndLastUpdate(string userId, DateTime lastUpdate)
    {
        UserId = userId;
        LastUpdate = lastUpdate;
    }
}

public class MatchIdAndProductId
{
    public int MatchId { get; set; }
    public int ProductId { get; set; }

    [JsonConstructor]
    public MatchIdAndProductId(int matchId, int productId)
    {
        MatchId = matchId;
        ProductId = productId;
    }

    public MatchIdAndProductId(UserProductAttitude userProductAttitude, (int, string)[] matchIdsWithUserIds)
    {
        string relevantUserId = userProductAttitude.UserId;
        foreach ((int matchId, string userId) in matchIdsWithUserIds)
        {
            if (userId == relevantUserId)
            {
                MatchId = matchId;
                break;
            }
        }
        ProductId = userProductAttitude.ProductId;
    }
}

public class TimeStamped<T>
{
    public T Value { get; set; }
    public DateTime SendTime { get; set; }

    public TimeStamped(T value)
    {
        Value = value;
        SendTime = DateTime.Now;
    }

    [JsonConstructor]
    public TimeStamped(T value, DateTime sendTime)
    {
        Value = value;
        SendTime = sendTime;
    }
}