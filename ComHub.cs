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

    private static readonly int MaxSwipingProducts = 10;

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
        Product[] swipingProducts = Db.Products
            .Where(p => dbUser.Wishlist.Contains(p.Category) && !Db.DontShowTo.Any(d => dbUser.Id == d.UserId && p.Id == d.ProductId))
            .Take(MaxSwipingProducts)
            .ToArray(); // use method below

        Match[] matches1 = Db.Match_database
            .Include(m => m.User2)
            .Where(m => m.UserId1 == dbUser.Id)
            .Select(m => new Match(
                m.Id,
                m.User2.Name,
                m.User2.PictureUrl,
                Db.IsInterested.Where(i => i.User == m.User2 && i.Product.User == dbUser).Select(i => i.ProductId).ToArray(),
                Db.IsInterested.Where(i => i.User == dbUser && i.Product.User == m.User2).Select(i => i.Product).ToArray(),
                Db.Message_database.Where(mes => mes.MatchId == m.Id).OrderBy(mes => mes.DateTime).Select(mes => new Message(mes.MatchId, mes.User == dbUser, mes.Content, mes.DateTime)).ToArray()
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
                Db.Message_database.Where(mes => mes.MatchId == m.Id).OrderBy(mes => mes.DateTime).Select(mes => new Message(mes.MatchId, mes.User == dbUser, mes.Content, mes.DateTime)).ToArray()
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

        Product[] swipingProducts = Db.Products
            .Where(p => dbUser.Wishlist.Contains(p.Category) && !Db.DontShowTo.Any(d => dbUser.Id == d.UserId && p.Id == d.ProductId))
            .Take(MaxSwipingProducts)
            .ToArray(); // use method below
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

        SendUpdatedProductToInterestedUsers(product);

        return true;
    }

    private async Task SendUpdatedProductToInterestedUsers(Product product)
    {
        (int,string)[] matchesAndUsers1 = Db.Match_database
            .Where(m => m.UserId1 == product.OwnerId && Db.IsInterested.Any(i => i.UserId == m.UserId2 && i.ProductId == product.Id))
            .Select(m => new { m.Id, m.UserId2 })
            .AsEnumerable()
            .Select(m => (m.Id, m.UserId2))
            .ToArray();

        (int, string)[] matchesAndUsers2 = Db.Match_database
            .Where(m => m.UserId2 == product.OwnerId && Db.IsInterested.Any(i => i.UserId == m.UserId1 && i.ProductId == product.Id))
            .Select(m => new { m.Id, m.UserId1 })
            .AsEnumerable()
            .Select(m => (m.Id, m.UserId1))
            .ToArray();

        (int, string)[] matchesAndUsers = matchesAndUsers1.Concat(matchesAndUsers2).ToArray();

        foreach ((int matchId, string userId) in matchesAndUsers)
        {
            await Clients.Group(userId).SendAsync("UpdateForeignProductInMatch", product, matchId);
        }
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

    public async Task<Message> SendMessage(Message message, string userId)
    {
        message.DateTime = DateTime.Now; // TODO: return should happen already here and the rest placed in a non-awaited async method

        Message_database message_database = new (message.MatchId, userId, message.Content, (DateTime)message.DateTime);
        Db.Message_database.Add(message_database);
        await Db.SaveChangesAsync();

        Message messageForOtherUser = message;
        messageForOtherUser.Own = false;

        Match_database match = await Db.Match_database.FindAsync(message.MatchId);
        string otherUserId = match.UserId1 == userId ? match.UserId2 : match.UserId1;

        await Clients.Group(otherUserId).SendAsync("ReceiveMessage", messageForOtherUser);

        return message;
    }

    public async Task<Product[]?> NoToProduct(OnSwipeData onSwipeData)
    {
        UserProductAttitude userProductAttitude = onSwipeData.UserProductAttitude;
        int[]? remainingSwipingProductIds = onSwipeData.RemainingSwipingProductIds;

        DontShowTo dontShowTo = new DontShowTo(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        await Db.SaveChangesAsync();

        return SwipingProducts(remainingSwipingProductIds, userProductAttitude.UserId);
    }

    public async Task<Product[]?> YesToProduct(OnSwipeData onSwipeData)
    {
        UserProductAttitude userProductAttitude = onSwipeData.UserProductAttitude;
        int[]? remainingSwipingProductIds = onSwipeData.RemainingSwipingProductIds;

        DontShowTo dontShowTo = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        IsInterested isInterested = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.IsInterested.Add(isInterested);
        await Db.SaveChangesAsync();

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
                if (alreadyAMatch)
                {
                    int matchId = Db.Match_database
                        .First(m => (m.UserId1 == userId && m.UserId2 == ownerId) || (m.UserId1 == ownerId && m.UserId2 == userId))
                        .Id;
                    await SendAddedProductToMatchToTwoUsers(userId, ownerId, product, matchId);
                }
                else
                {
                    Match_database newMatch = new(userId, ownerId);
                    Db.Match_database.Add(newMatch);
                    await Db.SaveChangesAsync();
                    await SendNewMatchToTwoUsers(userId, ownerId);
                }
            }
        }

        return SwipingProducts(remainingSwipingProductIds, userProductAttitude.UserId);
    }

    public async Task<Product[]?> WillPayForProduct(OnSwipeData onSwipeData)
    {
        UserProductAttitude userProductAttitude = onSwipeData.UserProductAttitude;
        int[]? remainingSwipingProductIds = onSwipeData.RemainingSwipingProductIds;

        DontShowTo dontShowTo = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        IsInterested isInterested = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.IsInterested.Add(isInterested);
        WillPay willPay = new(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.WillPay.Add(willPay);
        await Db.SaveChangesAsync();

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
            if (alreadyAMatch)
            {
                int matchId = Db.Match_database
                    .First(m => (m.UserId1 == userId && m.UserId2 == ownerId) || (m.UserId1 == ownerId && m.UserId2 == userId))
                    .Id;
                await SendAddedProductToMatchToTwoUsers(userId, ownerId, product, matchId);
            }
            else
            {
                Match_database newMatch = new(userId, ownerId);
                Db.Match_database.Add(newMatch);
                await Db.SaveChangesAsync();
                await SendNewMatchToTwoUsers(userId, ownerId);
            }
        }

        return SwipingProducts(remainingSwipingProductIds, userProductAttitude.UserId);
    }

    public async Task<Product[]?> OnRefreshMainpage(OnRefreshMainpageData onRefreshMainpageData)
    {
        return SwipingProducts(onRefreshMainpageData.RemainingSwipingProductIds, onRefreshMainpageData.UserId);
    }

    public Product[]? SwipingProducts(int[]? remainingSwipingProductIds, string userId)
    {
        if (remainingSwipingProductIds == null)
        {
            return null;
        }
        else
        {
            int numberOfRequestedProducts = MaxSwipingProducts - remainingSwipingProductIds.Length;
            User user = Db.Users.Find(userId);

            return Db.Products
                .Where(p => 
                    user.Wishlist.Contains(p.Category) 
                    && !Db.DontShowTo.Any(d => userId == d.UserId && p.Id == d.ProductId)
                    && !remainingSwipingProductIds.Any(pId => pId == p.Id)
                )
                .Take(numberOfRequestedProducts)
                .ToArray();
        }
    }

    private async Task SendAddedProductToMatchToTwoUsers(string interestedUserId, string ownerId, Product product, int matchId)
    {
        await Clients.Group(interestedUserId).SendAsync("AddForeignProductToMatch", product, matchId);
        await Clients.Group(ownerId).SendAsync("AddOwnProductToMatch", product.Id, matchId);
    }

    private async Task SendNewMatchToTwoUsers(string userId1, string userId2)
    {
        Match_database match_database = Db.Match_database
            .Where(m => m.UserId1 == userId1 && m.UserId2 == userId2)
            .FirstOrDefault();
        User user1 = Db.Users.Find(userId1);
        User user2 = Db.Users.Find(userId2);

        Match matchForUser1 = new(
            match_database.Id,
            user2.Name,
            user2.PictureUrl,
            Db.IsInterested.Where(i => i.UserId == userId1 && i.Product.OwnerId == userId2).Select(i => i.ProductId).ToArray(),
            Db.IsInterested.Where(i => i.UserId == userId2 && i.Product.OwnerId == userId1).Select(i => i.Product).ToArray(),
            Array.Empty<Message>()
        );

        await Clients.Group(userId1).SendAsync("ReceiveMatch", matchForUser1);

        Match matchForUser2 = new(
            match_database.Id,
            user1.Name,
            user1.PictureUrl,
            Db.IsInterested.Where(i => i.UserId == userId2 && i.Product.OwnerId == userId1).Select(i => i.ProductId).ToArray(),
            Db.IsInterested.Where(i => i.UserId == userId1 && i.Product.OwnerId == userId2).Select(i => i.Product).ToArray(),
            Array.Empty<Message>()
        );

        await Clients.Group(userId2).SendAsync("ReceiveMatch", matchForUser2);
    }
}
