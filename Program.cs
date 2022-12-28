using Microsoft.EntityFrameworkCore;
using TinderButForBarteringBackend;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ProductDb>(opt => opt.UseInMemoryDatabase("Products"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var products = app.MapGroup("/products");

products.MapGet("/", async (ProductDb db) =>
    await db.Products.ToListAsync());

//products.MapGet("/return", async (ProductDb db) =>
//    await db.Products.Where(t => t.RequiresSomethingInReturn).ToListAsync());

//products.MapGet("/{id}", async (int id, ProductDb db) =>
//    await db.Products.FindAsync(id)
//        is Product product
//            ? Results.Ok(product)
//            : Results.NotFound());

products.MapPost("/", async (ProductWithPictureData product, ProductDb db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    using (Image image = Image.FromStream(new MemoryStream(product.PrimaryPictureData)))
    {
        image.Save($"Data/Images/{product.Id}.jpg", ImageFormat.Jpeg);
    }

    return Results.Created($"/products/{product.Id}", product as Product);
});

products.MapPut("/{id}", async (int id, ProductWithPictureData inputProduct, ProductDb db) =>
{
    var product = await db.Products.FindAsync(id);

    if (product is null) return Results.NotFound();

    //product.OwnerId = inputProduct.OwnerId;
    //product.Category = inputProduct.Category;
    product.Title = inputProduct.Title;
    product.Description = inputProduct.Description;
    //product.IsSold = inputProduct.IsSold;
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

products.MapDelete("/{id}", async (int id, ProductDb db) =>
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

products.MapGet("/images/{filename}", async (string filename, HttpResponse response) =>
{
    var path = Path.Combine(@"Data/Images/", filename);
    var fileBytes = await File.ReadAllBytesAsync(path);
    response.ContentType = "image/jpeg";
    await response.Body.WriteAsync(fileBytes);
});

app.Run();