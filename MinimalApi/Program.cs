using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinimalApi;
using MinimalApi.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection"));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters()
                {
                    ValidateActor = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ClockSkew = TimeSpan.Zero,
                    ValidAudience =
                        builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["Jwt:Key"]))

                };
        });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Use((context, next)
    =>
{
    return next(context);
});


//MinimalAPI 

//User

app.MapPost("/register", async (UserRegisterDto? newUser, DataContext db) =>
{

    if (newUser is null) return Results.BadRequest("Must include user object");

    var emailCheck = await db.Users.FirstOrDefaultAsync(u => u.Email == newUser.Email);
    if (emailCheck is not null) return Results.BadRequest("Email already in use");

    var user = new User()
    {
        Name = newUser.Name,
        Email = newUser.Email.ToUpper(),
        Password = newUser.Password,
        Role = "User"
    };

    try
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Results.Ok("User registered");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, "something went wrong try again");
    }

});


app.MapPost("/login", async (UserLogin userLogin, DataContext db) =>
{
    User? user = await db.Users.FirstOrDefaultAsync(u =>
        u.Email.Equals(userLogin.Email.ToUpper()) && u.Password.Equals(userLogin.Password));

    if (user is null) return Results.NotFound("User not found");

    var claimsArr = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Name),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.GivenName, user.Name),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var secretKey = builder.Configuration["Jwt:Key"];
    if (secretKey is null) return Results.StatusCode(500);

    var token = new JwtSecurityToken
    (
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claimsArr,
        expires: DateTime.UtcNow.AddHours(1),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);

});

//ToDoApi

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


app.MapPost("Todo", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "User")] async (Todo todo, DataContext db) =>
{
    try
    {
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return Results.Created($"/todo/{todo.Id}", todo);
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
    todo.IsComplete = updatedTodo.IsComplete;
    await db.SaveChangesAsync();

    return Results.Ok(todo.Name + ": updated successfully");
});

app.MapGet("/Todo/Complete", async (DataContext db) =>
{
    var todoList =
        await db.Todos.Where(todo => todo.IsComplete == true).ToListAsync();
    if (todoList.Count() < 0) return Results.NoContent();
    return Results.Ok(todoList);
});

app.Run();
