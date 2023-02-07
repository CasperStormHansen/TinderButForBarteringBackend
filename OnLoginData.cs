using System.Text.Json.Serialization;

namespace TinderButForBarteringBackend;

public class OnLoginData
{
    public User Item1 { get; set; }
    public Product[] Item2 { get; set; }
    public Product[] Item3 { get; set; }
    public string[] Item4 { get; set; }
    public Match[] Item5 { get; set; }

    public OnLoginData(User item1, Product[] item2, Product[] item3, string[] item4, Match[] item5)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        Item5 = item5;
    }
}
