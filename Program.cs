using Microsoft.EntityFrameworkCore;
using TinderButForBarteringBackend;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ProductDb>(opt => opt.UseInMemoryDatabase("Products"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var products = app.MapGroup("/products");

products.MapGet("/", async (ProductDb db) =>
    await db.Products.ToListAsync());

products.MapGet("/return", async (ProductDb db) =>
    await db.Products.Where(t => t.RequiresSomethingInReturn).ToListAsync());

products.MapGet("/{id}", async (int id, ProductDb db) =>
    await db.Products.FindAsync(id)
        is Product product
            ? Results.Ok(product)
            : Results.NotFound());

products.MapPost("/", async (Product product, ProductDb db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/products/{product.Id}", product);
});

products.MapPut("/{id}", async (int id, Product inputProduct, ProductDb db) =>
{
    var product = await db.Products.FindAsync(id);

    if (product is null) return Results.NotFound();

    //product.OwnerId = inputProduct.OwnerId;
    //product.Category = inputProduct.Category;
    product.ProductTitle = inputProduct.ProductTitle;
    product.Description = inputProduct.Description;
    //product.IsSold = inputProduct.IsSold;
    product.RequiresSomethingInReturn = inputProduct.RequiresSomethingInReturn;

await db.SaveChangesAsync();
     
    return Results.NoContent();
});

products.MapDelete("/{id}", async (int id, ProductDb db) =>
{
    if (await db.Products.FindAsync(id) is Product product)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return Results.Ok(product);
    }

    return Results.NotFound();
});

app.Run();