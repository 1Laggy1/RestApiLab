using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
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

// Генерація токену
app.MapPost("/register", async (FinanceDbContext db, User user) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = GenerateJwtToken(user.Id.ToString());
    return Results.Ok(new { Token = token });
});

app.MapPost("/login", async (FinanceDbContext db, UserLoginRequest request) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Name == request.Name);
    if (user == null) return Results.Unauthorized();

    var token = GenerateJwtToken(user.Id.ToString());
    return Results.Ok(new { Token = token });
});

// Захищені ендпоїнти
app.MapPost("/deposit", async (HttpContext context, FinanceDbContext db, int userId, decimal amount) =>
{
    if (!ValidateJwt(context)) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId);
    if (user == null) return Results.NotFound();

    user.Balance += amount;
    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapPost("/withdraw", async (HttpContext context, FinanceDbContext db, int userId, decimal amount) =>
{
    if (!ValidateJwt(context)) return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId);
    if (user == null) return Results.NotFound();

    if (user.Balance < amount) return Results.BadRequest("Insufficient funds.");

    user.Balance -= amount;
    await db.SaveChangesAsync();
    return Results.Ok(user);
});
// Робота з категоріями
app.MapPost("/category", async (HttpContext context, FinanceDbContext db, Category category) =>
{
    if (!ValidateJwt(context)) return Results.Unauthorized();
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/category/{category.Id}", category);
});

app.MapGet("/category/{categoryId}", async (HttpContext context, FinanceDbContext db, int categoryId) =>
{
    var category = await db.Categories.FindAsync(categoryId);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

// Робота з записами
app.MapPost("/record", async (HttpContext context,FinanceDbContext db, Record record) =>
{
    if (!ValidateJwt(context)) return Results.Unauthorized();
    var user = await db.Users.FindAsync(record.UserId);
    if (user is null) return Results.NotFound("User not found.");

    if (record.IsExpense && user.Balance < record.Amount)
        return Results.BadRequest("Insufficient funds for expense.");

    user.Balance += record.IsExpense ? -record.Amount : record.Amount;
    db.Records.Add(record);
    await db.SaveChangesAsync();
    return Results.Created($"/record/{record.Id}", record);
});

app.MapGet("/record/{recordId}", async (HttpContext context,FinanceDbContext db, int recordId) =>
{
    if (!ValidateJwt(context)) return Results.Unauthorized();
    var record = await db.Records.Include(r => r.Category).FirstOrDefaultAsync(r => r.Id == recordId);
    return record is not null ? Results.Ok(record) : Results.NotFound();
});

app.Run();

string GenerateJwtToken(string userId)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSuperSecureKeySuperSecureKey"));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: "yourapp",
        audience: "yourapp",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

bool ValidateJwt(HttpContext context)
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var tokenHeader))
        return false;

    var token = tokenHeader.ToString().Replace("Bearer ", string.Empty);

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes("SuperSuperSecureKeySuperSecureKey");
    try
    {
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "yourapp",
            ValidAudience = "yourapp",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        }, out SecurityToken validatedToken);

        return true;
    }
    catch
    {
        return false;
    }
}

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

public class UserLoginRequest
{
    public string Name { get; set; }
}
