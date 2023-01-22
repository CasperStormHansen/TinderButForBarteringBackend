using Microsoft.EntityFrameworkCore;
using TinderButForBarteringBackend;
using System.Drawing.Imaging;
using System.Drawing;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BarterDatabase>(opt => opt.UseSqlite("Data Source=data/BarterDatabase.db"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

// add failure replies in several places

app.MapPost("/onlogin", async (User incomingUser, BarterDatabase db) =>
{
    User? dbUser = await db.Users.FindAsync(incomingUser.Id);
    if (dbUser == null) // User is new
    {
        dbUser = incomingUser;
        dbUser.Wishlist = Enumerable.Range(0, Product.Categories.Length).Select(i => (byte)i) .ToArray();
        db.Users.Add(dbUser);
        await db.SaveChangesAsync();
    }
    else if (dbUser.PictureUrl == null && incomingUser.PictureUrl != null) // User is old but picture is only supplied now
    {
        dbUser.PictureUrl = incomingUser.PictureUrl;
        await db.SaveChangesAsync();
    }

    Product[] ownProducts = db.Products.Where(t => t.OwnerId == dbUser.Id).ToArray();
    Product[] swipingProducts = db.Products.Where(t => dbUser.Wishlist.Contains(t.Category) && t.OwnerId != dbUser.Id).ToArray(); // tentative: returns ALL products in the user's wish-categories not owned by the user themself and ordered arbitrarily
    return Results.Ok(new Tuple<User, Product[], Product[], string[]>(dbUser, ownProducts, swipingProducts, Product.Categories));
});

app.MapPost("/onwishesupdate", async (User incomingUser, BarterDatabase db) =>
{
    User dbUser = await db.Users.FindAsync(incomingUser.Id);
    dbUser.Wishlist = incomingUser.Wishlist;
    await db.SaveChangesAsync();

    Product[] swipingProducts = db.Products.Where(t => dbUser.Wishlist.Contains(t.Category) && t.OwnerId != dbUser.Id).ToArray(); // make seperate method
    return Results.Ok(swipingProducts);
});

app.MapPost("/newproduct", async (ProductWithPictureData product, BarterDatabase db) =>
{
    // dont show to table: add owner

    db.Products.Add(product);
    await db.SaveChangesAsync();

    using (Image image = Image.FromStream(new MemoryStream(product.PrimaryPictureData)))
    {
        image.Save($"Data/Images/{product.Id}.jpg", ImageFormat.Jpeg);
    }

    return Results.Ok(product as Product);
});

app.MapPut("/changeproduct/{id}", async (int id, ProductWithPictureData inputProduct, BarterDatabase db) =>
{
    var product = await db.Products.FindAsync(id);

    if (product is null) return Results.NotFound();

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

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/deleteproduct/{id}", async (int id, BarterDatabase db) =>
{
    if (await db.Products.FindAsync(id) is Product product)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        File.Delete($"Data/Images/{product.Id}.jpg");
        return Results.Ok(product);
    }

    return Results.NotFound();
});

app.MapGet("/images/{filename}", async (string filename, HttpResponse response) =>
{
    var path = Path.Combine(@"Data/Images/", filename);
    var fileBytes = await File.ReadAllBytesAsync(path);
    response.ContentType = "image/jpeg";
    await response.Body.WriteAsync(fileBytes);
});

app.Run();