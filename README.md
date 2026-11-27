# SocialNetwork

A small social network web app built with ASP.NET Core MVC. Users can register, log in, publish posts with images, react to other people's posts, subscribe to profiles and keep a list of saved posts.

It is a learning project, made for use by me, my family and close friends.

## Features

- **Accounts**
  - Register with full name, username, email and password.
  - Log in with email and password (cookie authentication) and log out.
  - Passwords are stored hashed, never as plain text.
- **Posts**
  - Create posts with text and an optional image.
  - Like posts, comment on them, and delete your own comments.
  - Save a post to your favorites.
  - Make your own post private or public, and delete your own post.
  - Report a post. A post with 5 or more reports is hidden from everyone.
- **Subscriptions**
  - The "Recommended Profiles" box on the home page shows the 3 newest users.
  - Subscribe or unsubscribe with one click, without reloading the page.
- **Pages**
  - **Home**: all posts you are allowed to see, newest first.
  - **Subscriptions**: posts from the profiles you subscribed to.
  - **Saved**: posts you added to favorites.

## Tech stack

| Part | Technology |
| --- | --- |
| Web app | ASP.NET Core MVC, .NET 9, Razor views |
| Data access | Entity Framework Core 9 with SQL Server |
| Authentication | Cookie authentication, `PasswordHasher` from ASP.NET Core Identity (no Identity database tables) |
| Front end | Bootstrap 5, jQuery, Ionicons, plain JavaScript (`fetch`) |
| Tests | xUnit, Moq, FluentAssertions, EF Core InMemory |

## Project structure

```
SocialNetwork.sln
├── SocialNetwork/          Web app: controllers, views, view models, view components, wwwroot
├── SocialNetwork.Data/     Database: models, AppDBContext, migrations, services, seed data
└── SocialNetwork.Tests/    Unit tests
```

Main pieces:

- `Controllers/HomeController.cs`: home feed, posts, likes, favorites, reports, subscriptions, Saved and Subscriptions pages.
- `Controllers/AccountController.cs`: register, login, logout.
- `ViewComponents/RecommendedProfilesViewComponent.cs`: the recommended profiles box.
- `SocialNetwork.Data/Services/PostService.cs`: all post queries and post actions.
- `SocialNetwork.Data/Helpers/DbInitializer.cs`: seeds a sample user into an empty database.

## Getting started

### Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express or full edition)
- The EF Core command line tool: `dotnet tool install --global dotnet-ef`

### 1. Set the database connection

Open `SocialNetwork/appsettings.json` and change `ConnectionStrings:Default` so that it points to your SQL Server, for example:

```
Data Source=localhost\SQLEXPRESS;Initial Catalog=SocialNetworkDB;Integrated Security=True;Encrypt=False;Trust Server Certificate=True
```

### 2. Create the migration for the latest model changes

Migrations are stored in the `SocialNetwork.Data` project. Run this from the folder that contains `SocialNetwork.sln`:

```
dotnet ef migrations add AddLoginAndSubscriptions --project SocialNetwork.Data --startup-project SocialNetwork
```

The app applies all pending migrations automatically when it starts.

### 3. Run

```
dotnet run --project SocialNetwork
```

Then open the address printed in the console.

### First login (development only)

When the app runs in the Development environment, the seeded user gets a login:

- Email: `nick@example.com`
- Password: `Password123!`

This is only for trying the app. Remove that block from `Program.cs` before real people use it. Everybody else can create an account on the Register page.

### Run the tests

```
dotnet test
```

## Where files are stored

- **Post images** are saved on disk in `SocialNetwork/wwwroot/images/uploaded/` under a random file name. The database only stores the path in `Post.ImageUrl`.
- **Profile pictures** are not uploaded yet. `User.ProfilePictureUrl` is a text field, and users without one see the default `wwwroot/images/user.png`.

If you deploy the app, copy the `uploaded` folder together with the database, otherwise old posts will point to missing images.

## Known limitations

- There is no profile picture upload and no profile page.
- The search box, notifications, "My Account" menu item and "Trends for you" box are placeholders and do nothing yet.
- No password reset and no email confirmation.
- "Newest users" is based on the order of registration (the user id), because users have no creation date.
- The Home feed shows all posts to everyone who is logged in. It is not limited to subscriptions (that is what the Subscriptions page is for).
