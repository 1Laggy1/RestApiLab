using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory data
var users = new List<User>();
var categories = new List<Category>();
var records = new List<Record>();

// User Endpoints
app.MapGet("/user/{userId}", (int userId) =>
{
    var user = users.FirstOrDefault(u => u.Id == userId);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapDelete("/user/{userId}", (int userId) =>
{
    var user = users.FirstOrDefault(u => u.Id == userId);
    if (user is not null)
    {
        users.Remove(user);
        return Results.Ok();
    }
    return Results.NotFound();
});

app.MapPost("/user", (User user) =>
{
    user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);
    return Results.Created($"/user/{user.Id}", user);
});

app.MapGet("/users", () => Results.Ok(users));


app.Run();

// Models
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Record
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
}
