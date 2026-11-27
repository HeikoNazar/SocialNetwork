using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data;
using SocialNetwork.Data.Helpers;
using SocialNetwork.Data.Models;
using SocialNetwork.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


//Database Configuration
var dbConnectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDBContext>(options=> options.UseSqlServer(dbConnectionString));
//Services configuration 
builder.Services.AddScoped<IPostService, PostService>();

//Authentication configuration (cookie based login)
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
    });
builder.Services.AddAuthorization();
var app = builder.Build(); 
//Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    await dbContext.Database.MigrateAsync();
    await DbInitializer.SeedAsync(dbContext);
    await DbInitializer.BackfillHashtagsAsync(dbContext);

    // DEVELOPMENT ONLY: give the seeded "nick" user a login so there is someone to sign in as.
    // Remove this (or add a registration page) before the app is used by real people.
    if (app.Environment.IsDevelopment())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var seedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserHandle == "nick" && u.PasswordHash == null);
        if (seedUser != null)
        {
            seedUser.Email = "nick@example.com";
            seedUser.PasswordHash = hasher.HashPassword(seedUser, "Password123!");
            await dbContext.SaveChangesAsync();
        }
    }
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
