using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TinderButForBarteringBackend;

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
}

public class Match
{
    public int MatchId { get; set; }
    public string Name { get; set; }
    public string PictureUrl { get; set; }
    public List<Product> OwnProducts { get; set; } // Should this just be ids?
    public List<Product> ForeignProducts { get; set; }
    public List<Message> Messages { get; set; }
}

public class Message
{
    public bool Own { get; set; }
    public string Content { get; set; }
    public DateTime DateTime { get; set; }
}