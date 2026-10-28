# Lost and Found

Lost and Found is an ASP.NET Core MVC web application for reporting lost or found items and contacting the person who created a report. Users can publish a report with photos, browse active reports, manage their own posts, and exchange messages in an inbox with live SignalR updates.

## Features

- Browse active lost and found reports without signing in.
- Register, sign in, sign out, and update a profile.
- Create and edit reports, then mark them resolved or closed.
- Upload up to five JPG, JPEG, PNG, or WebP photos per submission (up to 5 MB each).
- Start a conversation from an active report, review conversations in an inbox, and receive live message updates.
- Choose whether a report uses in-app messaging or shows a phone contact preference.

Posting, profile management, and messaging require an authenticated account.

## Requirements

- .NET 10 SDK
- SQL Server (LocalDB is suitable for local Windows development)

## Run locally

1. Clone the repository and open a terminal in the repository root.
2. Configure the database connection string as described below.
3. Start the web application:

   ```powershell
   dotnet run --project .\LostandFound\LostAndFound.Ui.csproj
   ```

The application applies pending Entity Framework Core migrations during startup. Open the local URL printed by `dotnet run` in your browser.

To build the solution:

```powershell
dotnet build .\LostandFound.slnx
```

## Database configuration

The application reads the SQL Server connection string named `DefaultConnection` (`ConnectionStrings:DefaultConnection`). Supply it through .NET User Secrets or an environment variable. For example, in PowerShell for a LocalDB instance:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=LostAndFound;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project .\LostandFound\LostAndFound.Ui.csproj
```

Use an appropriate SQL Server connection string for your environment. Keep credentials out of source control; use User Secrets for local development and a managed secret store or environment configuration for deployment.

## Main routes

| Route | Purpose | Access |
| --- | --- | --- |
| `/Posts` | Browse active reports | Public |
| `/Account/Register` | Create an account | Public |
| `/Account/Login` | Sign in | Public |
| `/Account/Profile` | View and update profile | Signed-in users |
| `/Posts/Create` | Create a lost or found report | Signed-in users |
| `/Posts/MyPosts` | Manage your reports | Signed-in users |
| `/Posts/Inbox` | View conversations | Signed-in users |
| `/hubs/messages` | SignalR messaging hub | Signed-in users |

## Project structure

```text
LostAndFound.slnx
LostandFound/                 ASP.NET Core MVC UI, controllers, views, and SignalR hub
  Controllers/                Account, post, and home workflows
  Hubs/                       Real-time message hub
  Models/                     MVC view models
  Views/                      Razor pages
  wwwroot/                    Static assets and uploaded post photos
LostAndFound.Core/            Domain entities, enums, and account DTOs
LostAndFound.Infrastructure/  EF Core DbContext, SQL Server setup, and migrations
output/pdf/                   Generated project documentation
```

The UML overview is available in [output/pdf/lost-and-found-uml.pdf](output/pdf/lost-and-found-uml.pdf).

## Data model

- `ApplicationUser` extends ASP.NET Identity and adds a name and city.
- `ItemPost` belongs to a user and stores the lost/found type, item details, contact preference, status, and timestamps.
- `ItemPostImage` stores a file path for an image attached to a report. The image files are saved under `LostandFound/wwwroot/uploads/posts/`.
- `ItemPostMessage` stores a conversation message linked to a report and sender and recipient users.

The database schema is maintained by EF Core migrations in `LostAndFound.Infrastructure/Migrations`. The web application applies them automatically at startup.

## Messaging

The MVC inbox and conversation pages use the message records in SQL Server. Live messaging is provided by the authorized SignalR hub at `/hubs/messages`. The hub validates conversation membership, saves a message, and broadcasts message and inbox updates to the relevant SignalR groups.
