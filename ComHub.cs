using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace TinderButForBarteringBackend;

public class ComHub : Hub
{
    private readonly ConcurrentDictionary<string, string> Connections = new ();

    public readonly BarterDatabase Db;

    public ComHub(BarterDatabase db)
    {
        Db = db;
    }

    public async Task<OnLoginData> OnLogin(User incomingUser)
    {
        User? dbUser = await Db.Users.FindAsync(incomingUser.Id);
        if (dbUser == null) // User is new
        {
            dbUser = incomingUser;
            dbUser.Wishlist = Enumerable.Range(0, Product.Categories.Length).Select(i => (byte)i).ToArray();
            Db.Users.Add(dbUser);
            await Db.SaveChangesAsync();
        }
        else if (dbUser.PictureUrl == null && incomingUser.PictureUrl != null) // User is old but picture is only supplied now
        {
            dbUser.PictureUrl = incomingUser.PictureUrl;
            await Db.SaveChangesAsync();
        }

        Product[] ownProducts = Db.Products.Where(t => t.OwnerId == dbUser.Id).ToArray();
        Product[] swipingProducts = Db.Products.Where(p => dbUser.Wishlist.Contains(p.Category) && !Db.DontShowTo.Any(d => dbUser.Id == d.UserId && p.Id == d.ProductId)).ToArray();

        Match[] matches1 = Db.Match_database
            .Include(m => m.User2)
            .Where(m => m.UserId1 == dbUser.Id)
            .Select(m => new Match(
                m.Id,
                m.User2.Name,
                m.User2.PictureUrl,
                Db.IsInterested.Where(i => i.User == m.User2 && i.Product.User == dbUser).Select(i => i.ProductId).ToArray(),
                Db.IsInterested.Where(i => i.User == dbUser && i.Product.User == m.User2).Select(i => i.Product).ToArray(),
                Db.Message_database.Where(mes => mes.MatchId == m.Id).OrderBy(mes => mes.DateTime).Select(mes => new Message(mes.User == dbUser, mes.Content, mes.DateTime)).ToArray()
            ))
            .AsSingleQuery()
            .ToArray();

        Match[] matches2 = Db.Match_database
            .Include(m => m.User1)
            .Where(m => m.UserId2 == dbUser.Id)
            .Select(m => new Match(
                m.Id,
                m.User1.Name,
                m.User1.PictureUrl,
                Db.IsInterested.Where(i => i.User == m.User1 && i.Product.User == dbUser).Select(i => i.ProductId).ToArray(),
                Db.IsInterested.Where(i => i.User == dbUser && i.Product.User == m.User1).Select(i => i.Product).ToArray(),
                Db.Message_database.Where(mes => mes.MatchId == m.Id).OrderBy(mes => mes.DateTime).Select(mes => new Message(mes.User == dbUser, mes.Content, mes.DateTime)).ToArray()
            ))
            .AsSingleQuery()
            .ToArray();

        Match[] matches = matches1.Concat(matches2).ToArray();

        OnLoginData onLoginData = new(dbUser, ownProducts, swipingProducts, Product.Categories, matches);
        return onLoginData;
    }

    public async Task AnnonceUserIdOfConnection(string userId)
    {
        Connections.TryAdd(Context.ConnectionId, userId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return Disconnect(); // how to merge with next method?
    }

    public async Task Disconnect()
    {
        Connections.TryRemove(Context.ConnectionId, out string? _);
    }

    public async Task SendMessage(string message) // merely a test method
    {
        Console.WriteLine(message);
        await Clients.All.SendAsync("MessageReceived", message);
    }
}
