using Microsoft.EntityFrameworkCore;
using MinimalApi;
using MinimalApi.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataContext>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/Todo", async (DataContext db) =>
{
    if (db.Todos.Count() < 0) return Results.NoContent();

    var todoList = await db.Todos.ToListAsync();
    return Results.Ok(todoList);
});

app.MapGet("/Todo/{id}", async (int id, DataContext db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo == null) return Results.NotFound();

    return Results.Ok(todo);

});


app.MapPost("Todo", async (Todo todo, DataContext db) =>
{
    try
    {
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return Results.Created($"/todo/{todo.id}", todo);
    }
    catch (Exception e)
    {
        return Results.Conflict($"Error: {e} " + "Failed to add todo, try again");
    }
});

app.MapDelete("/Todo/{id}", async (int id, DataContext db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo.Name + ": deleted successfully");
    }
    return Results.NotFound($"Todo-id:{id} not found in database, check id and try again.");

});

app.MapPut("Todo/{id}", async (int id, Todo updatedTodo, DataContext db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound($"Todo-id:{id} not found in database, check id and try again.");

    todo.Name = updatedTodo.Name;
    todo.isComplete = updatedTodo.isComplete;
    await db.SaveChangesAsync();

    return Results.Ok(todo.Name + ": updated successfully");
});

app.MapGet("/Todo/Complete", async (DataContext db) =>
{
    var todoList =
        await db.Todos.Where(todo => todo.isComplete == true).ToListAsync();
    if (todoList.Count() < 0) return Results.NoContent();
    return Results.Ok(todoList);
});

app.Run();
