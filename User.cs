using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TinderButForBarteringBackend;

public class User
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
#nullable enable
    public string? PictureUrl { get; set; }
    public byte[]? Wishlist { get; set; }
    //[JsonIgnore]
    //public int[]? LastSwipingProductsBatch { get; set; }
#nullable disable
}
