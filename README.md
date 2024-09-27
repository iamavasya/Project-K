ENG | [UKR](https://github.com/iamavasya/Project-K/blob/main/README_uk.md)
# Project-K ⚡

**Project-K** is an ASP.NET Core application designed to provide kurins with a database to manage their members, including youth, mentors, influential members, and other critical information. 🛠️

## Features ✨
- 👥 Add, edit, view, and delete members.
- 🔑 Role-based user management with custom roles.
- 📝 User registration, authentication, and role assignment.
- ⚙️ Flexible codebase for easy modifications to fit specific needs.

## Installation 🖥️
1. Clone the repository:  
   `git clone https://github.com/iamavasya/Project-K.git`
2. Download and configure **MySQL Server** on your device.
3. Apply the connection string in the `DefaultConnection` setting:  
   `ConnectionString:DefaultConnection`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=your_database_name;User=root;Password=your_password;"
  },
}
```
4. **Add your Google Places API key** in the `appsettings.json` under `"ApiSettings"` as shown below:  
```json
"ApiSettings": { 
    "ApiKey": "YOUR-KEY" // Replace with your actual API key
}
```
5. Restore dependencies and build the project:  
```[README_uk.md](https://github.com/user-attachments/files/17163134/README_uk.md)

dotnet restore
dotnet build
```
6. Update the database using Entity Framework migrations:  
```
dotnet ef database update
```
8. Run the application:  
```
dotnet run
```

> [!IMPORTANT]
> Ensure you run all `dotnet` commands from the appropriate directories: the `.App` directory for application commands and the `.Infrastructure` directory for database updates.

## Autocomplete Functionality 🔍

In the project, you'll find `AutocompleteScript.cshtml` located in `Project-K.App/Views/Shared`, which pulls the API from Google Places. Ensure you insert your own API key in the `appsettings.json` under `"ApiSettings"`:
```cshtml
@* Views/Shared/_AutocompleteScripts.cshtml *@
<script src="https://maps.googleapis.com/maps/api/js?key=@ViewData["ApiKey"]&libraries=places"></script>
```

## Roadmap 🗺️

### Current Features
- User management (add, edit, delete).
- Role creation and assignment.
- User registration and authentication.

### Planned Features
- 📋 Leaderboard for user recognition.
- ⭐ Point system for member engagement.
- 📅 Event addition and management.
- 📚 Archive for member records.

### Future Improvements
- 🔍 Enhance application architecture for scalability.
- 🌐 Integrate external services.
- 🔄 Implement middleware and repository patterns.

## Requirements 📦
- .NET Core SDK
- MySQL Server

## Contributing 🤝
Feel free to open issues or submit pull requests to enhance the project.

## License 📜
This project is licensed under the MIT License.
