using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TinderButForBarteringBackend;

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