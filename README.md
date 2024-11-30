ENG | [UKR](https://github.com/iamavasya/Project-K/blob/main/README_uk.md)
# Project-K ⚡

**Project-K** is an ASP.NET Core application designed to provide kurins with a database to manage their members, including youth, mentors, influential members, and other critical information. 🛠️

## Features ✨
- 👥 Add, edit, view, and delete members.
- 🔑 Role-based user management with custom roles.
- 📝 User registration, authentication, and role assignment.
- ⚙️ Flexible codebase for easy modifications to fit specific needs.

## (❗) Installation 🖥️
1. Clone the repository:  
   `git clone https://github.com/iamavasya/Project-K.git`

2. Run the application with Docker:  
   Use the following command to build and start the application along with its dependencies (including MySQL):
   
   `docker compose up --build`

This command automatically configures and launches the app, database, and other services.

> [!NOTE]
> In the `Program.cs`, all migrations are automatically applied to the database upon building the application. If an error occurs during the first migration, it is recommended to drop the database in Docker container via sh and restart the application to resolve the issue.

## Optional: Autocomplete Functionality 🔍

In the project, you'll find `AutocompleteScript.cshtml` located in `Project-K.App/Views/Shared`, which pulls the API from Google Places. Ensure you insert your own API key in the `appsettings.json` under `"ApiSettings"`:
```cshtml
@* Views/Shared/_AutocompleteScripts.cshtml *@
<script src="https://maps.googleapis.com/maps/api/js?key=@ViewData["ApiKey"]&libraries=places"></script>
```
Add your Google Places API key in the `appsettings.json` under `"ApiSettings"` as shown below:  
```json
"ApiSettings": { 
    "ApiKey": "YOUR-KEY" // Replace with your actual API key
}
```

## Roadmap 🗺️

### Current Features
- User management (add, edit, delete).
- Role creation and assignment.
- User registration and authentication.
- User Profiles.
  
(❗) **_New:_**
- 🏗️Improved architecture and patterns.
- 🚢 Project Containerization

### Planned Features
- 📋 Leaderboard for user recognition.
- ⭐ Point system for member engagement.
- 📅 Event addition and management.
- 📚 Archive for member records.

### (❗) Future Improvements
- 📘 Add supplementary **project documentation** to assist developers and users in understanding and utilizing the project effectively.
- 🐛 **Refactoring** and debugging to enhance code readability, maintainability, and reliability.
- ⚙️ Implement **CI/CD** pipelines using GitHub Actions and Docker to streamline development, testing, and deployment processes.
- 🌟 Transition the frontend to **Angular** for a modern and dynamic user experience.

## (❗) Requirements 📦
- Docker

## Contributing 🤝
Feel free to open issues or submit pull requests to enhance the project.

## License 📜
This project is licensed under the Proprietary License. You are allowed to view and use the code for personal or internal business purposes only. You may not modify, distribute, or create derivative works from the code without explicit permission from the author. For more details, please refer to the LICENSE file in this repository.
