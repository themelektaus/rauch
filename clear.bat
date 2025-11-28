dotnet clean
rmdir /S /Q .vs
rmdir /S /Q bin
rmdir /S /Q Build
rmdir /S /Q obj
del rauch.csproj.user
del Properties\PublishProfiles\FolderProfile.pubxml.user
del Properties\launchSettings.json
dotnet build
