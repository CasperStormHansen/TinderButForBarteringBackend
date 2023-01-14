using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TinderButForBarteringBackend;

public class User
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
#nullable enable
    public string? PictureUrl { get; set; }
#nullable disable
}
