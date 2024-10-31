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

// Category Endpoints
app.MapGet("/category", () => Results.Ok(categories));

app.MapPost("/category", (Category category) =>
{
    category.Id = categories.Count > 0 ? categories.Max(c => c.Id) + 1 : 1;
    categories.Add(category);
    return Results.Created($"/category/{category.Id}", category);
});

app.MapDelete("/category/{categoryId}", (int categoryId) =>
{
    var category = categories.FirstOrDefault(c => c.Id == categoryId);
    if (category is not null)
    {
        categories.Remove(category);
        return Results.Ok();
    }
    return Results.NotFound();
});

// Record Endpoints
app.MapGet("/record/{recordId}", (int recordId) =>
{
    var record = records.FirstOrDefault(r => r.Id == recordId);
    return record is not null ? Results.Ok(record) : Results.NotFound();
});

app.MapDelete("/record/{recordId}", (int recordId) =>
{
    var record = records.FirstOrDefault(r => r.Id == recordId);
    if (record is not null)
    {
        records.Remove(record);
        return Results.Ok();
    }
    return Results.NotFound();
});

app.MapPost("/record", (Record record) =>
{
    record.Id = records.Count > 0 ? records.Max(r => r.Id) + 1 : 1;
    records.Add(record);
    return Results.Created($"/record/{record.Id}", record);
});

app.MapGet("/record", ([Microsoft.AspNetCore.Mvc.FromQuery] int? userId, [Microsoft.AspNetCore.Mvc.FromQuery] int? categoryId) =>
{
    if (userId == null && categoryId == null)
        return Results.BadRequest("Must provide user_id or category_id as filter.");

    var filteredRecords = records.Where(r =>
        (!userId.HasValue || r.UserId == userId) &&
        (!categoryId.HasValue || r.CategoryId == categoryId)).ToList();

    return Results.Ok(filteredRecords);
});

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
