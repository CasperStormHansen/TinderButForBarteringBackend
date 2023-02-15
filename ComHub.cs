using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;

namespace TinderButForBarteringBackend;

public class ComHub : Hub
{
    public readonly BarterDatabase Db;

    public ComHub(BarterDatabase db)
    {
        Db = db;
    }

    private static readonly int MaxSwipingProducts = 10;

    private async Task RegisterUserIdOfConnection(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }

    public async Task<TimeStamped<OnLoginData>> OnLogin(User incomingUser)
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
            .Where(p => dbUser!.Wishlist.Contains(p.Category) && !Db.DontShowTo.Any(d => dbUser.Id == d.UserId && p.Id == d.ProductId))
            .Take(MaxSwipingProducts)
            .ToArray(); // use method below

        Match[] matches = Matches(dbUser.Id);

        OnLoginData onLoginData = new(dbUser, ownProducts, swipingProducts, Product.Categories, matches);
        return new TimeStamped<OnLoginData>(onLoginData);
    }

    public async Task<TimeStamped<OnReconnectionData>> OnReconnection(UserAndLastUpdate userAndLastUpdate)
    {
        string userId = userAndLastUpdate.UserId;
        DateTime lastUpdate = userAndLastUpdate.LastUpdate;

        await RegisterUserIdOfConnection(userId);

        Match[] newMatches = Matches(userId, lastUpdate);

        (int, string)[] oldMatchesAndUsers1 = Db.Match_database
            .Where(m => m.UserId1 == userId && m.CreationTime <= lastUpdate)
            .Select(m => new { m.Id, m.UserId2 })
            .AsEnumerable()
            .Select(m => (m.Id, m.UserId2))
            .ToArray();
        (int, string)[] oldMatchesAndUsers2 = Db.Match_database
            .Where(m => m.UserId2 == userId && m.CreationTime <= lastUpdate)
            .Select(m => new { m.Id, m.UserId1 })
            .AsEnumerable()
            .Select(m => (m.Id, m.UserId1))
            .ToArray();
        (int, string)[] oldMatchesAndUsers = oldMatchesAndUsers1.Concat(oldMatchesAndUsers2).ToArray();
        int[] oldMatchesIds = oldMatchesAndUsers.Select(x => x.Item1).ToArray();
        string[] oldMatchesOtherUserIds = oldMatchesAndUsers.Select(x => x.Item2).ToArray();

        Product[] updatedForeignProducts = Db.Products
            .Where(p => oldMatchesOtherUserIds.Contains(p.OwnerId) && p.UpdateTime > lastUpdate && Db.IsInterested.Any(i => i.UserId == userId && i.ProductId == p.Id))
            .ToArray();

        int[] ownProductIds = Db.Products
            .Where(p => p.OwnerId == userId)
            .Select(p => p.Id)
            .ToArray();

        MatchIdAndProductId[] newInterestsInOwnProducts = Db.IsInterested
            .Where(i => oldMatchesOtherUserIds.Contains(i.UserId) && i.CreationTime > lastUpdate && ownProductIds.Contains(i.ProductId))
            .Select(i => new MatchIdAndProductId(i, oldMatchesAndUsers))
            .ToArray();

        Message[] newMessages = Db.Message_database
            .Where(m => oldMatchesIds.Contains(m.MatchId) && m.DateTime > lastUpdate)
            .Select(m => new Message(m.MatchId, m.UserId == userId, m.Content, m.DateTime))
            .ToArray();

        OnReconnectionData onReconnectionData = new(newMatches, updatedForeignProducts, newInterestsInOwnProducts, newMessages);
        return new TimeStamped<OnReconnectionData>(onReconnectionData);
    }

    private Match[] Matches(string userId, DateTime createdAfter = default)
    {
        Match[] matches1 = Db.Match_database
            .Include(m => m.User2)
            .Where(m => m.UserId1 == userId && m.CreationTime > createdAfter)
            .Select(m => new Match(
                m.Id,
                m.User2.Name,
                m.User2.PictureUrl,
                Db.IsInterested.Where(i => i.User == m.User2 && i.Product.OwnerId == userId).Select(i => i.ProductId).ToArray(),
                Db.IsInterested.Where(i => i.UserId == userId && i.Product.User == m.User2).Select(i => i.Product).ToArray(),
                Db.Message_database.Where(mes => mes.MatchId == m.Id).OrderBy(mes => mes.DateTime).Select(mes => new Message(mes.MatchId, mes.UserId == userId, mes.Content, mes.DateTime)).ToArray()
            ))
            .AsSingleQuery()
            .ToArray();

        Match[] matches2 = Db.Match_database
            .Include(m => m.User1)
            .Where(m => m.UserId2 == userId && m.CreationTime > createdAfter)
            .Select(m => new Match(
                m.Id,
                m.User1.Name,
                m.User1.PictureUrl,
                Db.IsInterested.Where(i => i.User == m.User1 && i.Product.OwnerId == userId).Select(i => i.ProductId).ToArray(),
                Db.IsInterested.Where(i => i.UserId == userId && i.Product.User == m.User1).Select(i => i.Product).ToArray(),
                Db.Message_database.Where(mes => mes.MatchId == m.Id).OrderBy(mes => mes.DateTime).Select(mes => new Message(mes.MatchId, mes.UserId == userId, mes.Content, mes.DateTime)).ToArray()
            ))
            .AsSingleQuery()
            .ToArray();

        Match[] matches = matches1.Concat(matches2).ToArray();
        return matches;
    }

    public async Task<TimeStamped<Product[]>> OnWishesUpdate(User incomingUser)
    {
        User dbUser = await Db.Users.FindAsync(incomingUser.Id);
        dbUser.Wishlist = incomingUser.Wishlist;
        await Db.SaveChangesAsync();

        Product[] swipingProducts = Db.Products
            .Where(p => dbUser.Wishlist.Contains(p.Category) && !Db.DontShowTo.Any(d => dbUser.Id == d.UserId && p.Id == d.ProductId))
            .Take(MaxSwipingProducts)
            .ToArray(); // use method below

        return new TimeStamped<Product[]>(swipingProducts);
    }

    public async Task<TimeStamped<Product>> NewProduct(ProductWithPictureData product)
    {
        product.UpdateTime = DateTime.Now;

        Db.Products.Add(product);
        await Db.SaveChangesAsync();

        DontShowTo dontShowTo = new DontShowTo(product.OwnerId, product.Id);
        Db.DontShowTo.Add(dontShowTo);
        await Db.SaveChangesAsync();

        using (Image image = Image.FromStream(new MemoryStream(product.PrimaryPictureData)))
        {
            image.Save($"Data/Images/{product.Id}.jpg", ImageFormat.Jpeg);
        }

        Product returnProduct = product as Product;

        return new TimeStamped<Product>(returnProduct);
    }

    public async Task<TimeStamped<bool>> ChangeProduct(ProductWithPictureData inputProduct)
    {
        var product = await Db.Products.FindAsync(inputProduct.Id);

        if (product is null)
        {
            return new TimeStamped<bool>(false); // TODO: Throw exception? Will that automatically be sent to frontend?
        }

        product.UpdateTime = DateTime.Now;
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

        return new TimeStamped<bool>(true);
    }

    private async Task SendUpdatedProductToInterestedUsers(Product product)
    {
        (int, string)[] matchesAndUsers1 = Db.Match_database
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
            await Clients.Group(userId).SendAsync("UpdateForeignProductInMatch", DateTime.Now, product, matchId);
        }
    }

    public async Task<TimeStamped<bool>> DeleteProduct(int productId)
    {
        var product = await Db.Products.FindAsync(productId);

        if (product is null)
        {
            return new TimeStamped<bool>(false);// TODO: Throw exception? Will that automatically be sent to frontend?
        }

        Db.Products.Remove(product);
        await Db.SaveChangesAsync();
        File.Delete($"Data/Images/{product.Id}.jpg");

        return new TimeStamped<bool>(true);
    }

    public async Task<TimeStamped<Message>> SendMessage(Message message, string userId)
    {
        message.DateTime = DateTime.Now; // TODO: return should happen already here and the rest placed in a non-awaited async method

        Message_database message_database = new(message.MatchId, userId, message.Content, (DateTime)message.DateTime);
        Db.Message_database.Add(message_database);
        await Db.SaveChangesAsync();

        Message messageForOtherUser = message;
        messageForOtherUser.Own = false;

        Match_database match = await Db.Match_database.FindAsync(message.MatchId);
        string otherUserId = match.UserId1 == userId ? match.UserId2 : match.UserId1;

        await Clients.Group(otherUserId).SendAsync("ReceiveMessage", DateTime.Now, messageForOtherUser);

        return new TimeStamped<Message>(message);
    }

    public async Task<TimeStamped<Product[]?>> NoToProduct(OnSwipeData onSwipeData)
    {
        UserProductAttitude userProductAttitude = onSwipeData.UserProductAttitude;
        int[]? remainingSwipingProductIds = onSwipeData.RemainingSwipingProductIds;

        DontShowTo dontShowTo = new DontShowTo(userProductAttitude.UserId, userProductAttitude.ProductId);
        Db.DontShowTo.Add(dontShowTo);
        await Db.SaveChangesAsync();

        return new TimeStamped<Product[]?>(SwipingProducts(remainingSwipingProductIds, userProductAttitude.UserId));
    }

    public async Task<TimeStamped<Product[]?>> YesToProduct(OnSwipeData onSwipeData)
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

        return new TimeStamped<Product[]?>(SwipingProducts(remainingSwipingProductIds, userProductAttitude.UserId));
    }

    public async Task<TimeStamped<Product[]?>> WillPayForProduct(OnSwipeData onSwipeData)
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

        return new TimeStamped<Product[]?>(SwipingProducts(remainingSwipingProductIds, userProductAttitude.UserId));
    }

    public async Task<TimeStamped<Product[]?>> OnRefreshMainpage(OnRefreshMainpageData onRefreshMainpageData)
    {
        return new TimeStamped<Product[]?>(SwipingProducts(onRefreshMainpageData.RemainingSwipingProductIds, onRefreshMainpageData.UserId));
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
        await Clients.Group(interestedUserId).SendAsync("AddForeignProductToMatch", DateTime.Now, product, matchId);
        await Clients.Group(ownerId).SendAsync("AddOwnProductToMatch", DateTime.Now, product.Id, matchId);
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

        await Clients.Group(userId1).SendAsync("ReceiveMatch", DateTime.Now, matchForUser1);

        Match matchForUser2 = new(
            match_database.Id,
            user1.Name,
            user1.PictureUrl,
            Db.IsInterested.Where(i => i.UserId == userId2 && i.Product.OwnerId == userId1).Select(i => i.ProductId).ToArray(),
            Db.IsInterested.Where(i => i.UserId == userId1 && i.Product.OwnerId == userId2).Select(i => i.Product).ToArray(),
            Array.Empty<Message>()
        );

        await Clients.Group(userId2).SendAsync("ReceiveMatch", DateTime.Now, matchForUser2);
    }
}
