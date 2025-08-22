# Rick and Morty Episode Completion Endpoint
---

THis project involves ingesting a .CSV file and finding its related information on the Rick and Morty API, populating the database with the missing information, and allowing us to return an upload's episodes, characters, and locations in a sortable, searchable, paginated response.

## Running this project
Ensure you have all the dependencies installed

create the MSSQL server with
```
docker-compose up
```

Populate the DB using
```
dotnet ef database update
```

To test endpoints, run
```
dotnet watch run
```

Due to this being a small local project, you can log into the DB using the default sa account exposed in appsettings.json


