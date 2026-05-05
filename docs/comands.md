dotnet run -c Debug

dotnet publish -c Release

dotnet publish -c Release /p:PublishProfile=LocalPortable

dotnet publish -c Release /p:PublishProfile=LocalSlim