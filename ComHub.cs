using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Imaging;
using System.Drawing;

namespace TinderButForBarteringBackend;

public class ComHub : Hub
{
    public readonly BarterDatabase Db;

    public ComHub(BarterDatabase db)
    {
        Db = db;
    }

    public async Task RegisterUserIdOfConnection(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }

    public async Task<OnLoginData> OnLogin(User incomingUser)
    {
        await RegisterUserIdOfConnection(incomingUser.Id);
        
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

    public async Task<Product[]> OnWishesUpdate(User incomingUser)
    {
        User dbUser = await Db.Users.FindAsync(incomingUser.Id);
        dbUser.Wishlist = incomingUser.Wishlist;
        await Db.SaveChangesAsync();

        Product[] swipingProducts = Db.Products.Where(p => dbUser.Wishlist.Contains(p.Category) && !Db.DontShowTo.Any(d => dbUser.Id == d.UserId && p.Id == d.ProductId)).ToArray(); // make seperate method
        return swipingProducts;
    }

    public async Task<Product> NewProduct(ProductWithPictureData product)
    {
        Db.Products.Add(product);
        await Db.SaveChangesAsync();

        DontShowTo dontShowTo = new DontShowTo(product.OwnerId, product.Id);
        Db.DontShowTo.Add(dontShowTo);
        await Db.SaveChangesAsync();

        using (Image image = Image.FromStream(new MemoryStream(product.PrimaryPictureData)))
        {
            image.Save($"Data/Images/{product.Id}.jpg", ImageFormat.Jpeg);
        }

        return product as Product;
    }

    public async Task<bool> ChangeProduct(ProductWithPictureData inputProduct)
    {
        var product = await Db.Products.FindAsync(inputProduct.Id);

        if (product is null)
        {
            return false; // TODO: Throw exception? Will that automatically be sent to frontend?
        }

        product.Category = inputProduct.Category;
        product.Title = inputProduct.Title;
        product.Description = inputProduct.Description;
        product.RequiresSomethingInReturn = inputProduct.RequiresSomethingInReturn;
        if (inputProduct.PrimaryPictureData != null)
        {
            using (Image image = Image.FromStream(new MemoryStream(inputProduct.PrimaryPictureData)))
            {
                image.Save($"Data/Images/{product.Id}.jpg", ImageFormat.Jpeg);
            }
        }

        await Db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteProduct(int productId)
    {
        var product = await Db.Products.FindAsync(productId);

        if (product is null)
        {
            return false; // TODO: Throw exception? Will that automatically be sent to frontend?
        }

        Db.Products.Remove(product);
        await Db.SaveChangesAsync();
        File.Delete($"Data/Images/{product.Id}.jpg");

        return true;
    }

    public async Task NoToProduct(UserProductAttitude userProductAttitude)
    {
        DontShowTo dontShowTo = new DontShowTo(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        await Db.SaveChangesAsync();
    }

    public async Task YesToProduct(UserProductAttitude userProductAttitude)
    {
        DontShowTo dontShowTo = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        IsInterested isInterested = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.IsInterested.Add(isInterested);

        string userId = userProductAttitude.UserId;
        Product? product = Db.Products
            .Where(p => p.Id == userProductAttitude.ProductId)
            .FirstOrDefault();
        bool productStillExists = product != null;
        if (productStillExists)
        {
            string ownerId = product.OwnerId;
            bool mutualInterest = Db.IsInterested
                .Any(i => i.UserId == ownerId && i.Product.OwnerId == userId);
            if (mutualInterest || !product.RequiresSomethingInReturn)
            {
                bool alreadyAMatch = Db.Match_database
                    .Any(m => (m.UserId1 == userId && m.UserId2 == ownerId) || (m.UserId1 == ownerId && m.UserId2 == userId));
                if (!alreadyAMatch)
                {
                    Match_database newMatch = new(userId, ownerId);
                    Db.Match_database.Add(newMatch);
                    // Then send message to the two users
                }
            }
        }

        await Db.SaveChangesAsync();
    }

    public async Task WillPayForProduct(UserProductAttitude userProductAttitude)
    {
        DontShowTo dontShowTo = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        IsInterested isInterested = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.IsInterested.Add(isInterested);
        WillPay willPay = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.WillPay.Add(willPay);

        string userId = userProductAttitude.UserId;
        Product? product = Db.Products
            .Where(p => p.Id == userProductAttitude.ProductId)
            .FirstOrDefault();
        bool productStillExists = product != null;
        if (productStillExists)
        {
            string ownerId = product.OwnerId;
            bool alreadyAMatch = Db.Match_database
                .Any(m => (m.UserId1 == userId && m.UserId2 == ownerId) || (m.UserId1 == ownerId && m.UserId2 == userId));
            if (!alreadyAMatch)
            {
                Match_database newMatch = new(userId, ownerId);
                Db.Match_database.Add(newMatch);
                // Then send message to the two users
            }
        }

        await Db.SaveChangesAsync();
    }

    public async Task SendMessage(string message) // merely a test method
    {
        Console.WriteLine(message);
        await Clients.All.SendAsync("MessageReceived", message);
    }
}
