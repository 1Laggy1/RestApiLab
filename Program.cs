using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite("Data Source=finance.db"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

var app = builder.Build();

app.MapPost("/user", async (FinanceDbContext db, User user) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/user/{user.Id}", user);
});

app.MapGet("/user/{userId}", async (FinanceDbContext db, int userId) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/deposit", async (FinanceDbContext db, int userId, decimal amount) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    user.Balance += amount;
    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapPost("/withdraw", async (FinanceDbContext db, int userId, decimal amount) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    if (user.Balance < amount) return Results.BadRequest("Insufficient funds.");

    user.Balance -= amount;
    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapPost("/record", async (FinanceDbContext db, Record record) =>
{
    var user = await db.Users.FindAsync(record.UserId);
    if (user is null) return Results.NotFound("User not found.");

    if (record.IsExpense && user.Balance < record.Amount)
        return Results.BadRequest("Insufficient funds for expense.");

    user.Balance += record.IsExpense ? -record.Amount : record.Amount;
    db.Records.Add(record);
    await db.SaveChangesAsync();
    return Results.Created($"/record/{record.Id}", record);
});

app.MapGet("/record/{recordId}", async (FinanceDbContext db, int recordId) =>
{
    var record = await db.Records.Include(r => r.Category).FirstOrDefaultAsync(r => r.Id == recordId);
    return record is not null ? Results.Ok(record) : Results.NotFound();
});

app.MapGet("/category/{categoryId}", async (FinanceDbContext db, int categoryId) =>
{
    var category = await db.Categories.FindAsync(categoryId);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

app.MapPost("/category", async (FinanceDbContext db, Category category) =>
{
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/category/{category.Id}", category);
});

app.Run();

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Record> Records { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Balance { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Record
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public bool IsExpense { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int? CategoryId { get; set; }
    public Category Category { get; set; }
}
