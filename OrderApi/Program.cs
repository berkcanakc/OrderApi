using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Gerçek bir proje olmadığı için ve denemenizde kolaylık olması açısından In-memory veritabanı yaklaşımı kullandım
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("OrderDb"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger aktif et
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


// PRODUCT ENDPOINT'LERİ
// Projemiz ufak bir proje olduğu için Minimal API yaklaşımı kullandım

// Yeni ürün ekleme
app.MapPost("/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
});

// Tüm ürünleri listeleme
app.MapGet("/products", async (AppDbContext db) =>
{
    return await db.Products.ToListAsync();
});


// ORDER ENDPOINT'LERİ

// Sipariş ekleme (stok kontrolüyle)
app.MapPost("/orders", async (Order order, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(order.ProductId);
    if (product == null)
        return Results.NotFound("Ürün bulunamadı.");

    if (product.Stock < order.Quantity)
        return Results.BadRequest("Yeterli stok yok.");

    product.Stock -= order.Quantity;

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});

// Siparişleri listeleme
app.MapGet("/orders", async (AppDbContext db) =>
{
    return await db.Orders
        .ToListAsync();
});

// Sipariş detayı getirme
app.MapGet("/orders/{id}", async (int id, AppDbContext db) =>
{
    var order = await db.Orders
        .FirstOrDefaultAsync(o => o.Id == id);

    return order != null ? Results.Ok(order) : Results.NotFound();
});

// Sipariş silme
app.MapDelete("/orders/{id}", async (int id, AppDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order == null) return Results.NotFound();

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.Ok("Sipariş silindi.");
});

app.Run();