FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` can be cached independently of source changes.
COPY Gym.sln .
COPY Gym.Core/Gym.Core.csproj Gym.Core/
COPY Gym.Data/Gym.Data.csproj Gym.Data/
COPY Gym.Service/Gym.Service.csproj Gym.Service/
COPY Gym.API/Gym.API.csproj Gym.API/
COPY Gym.Tests/Gym.Tests.csproj Gym.Tests/
RUN dotnet restore Gym.API/Gym.API.csproj

COPY . .
RUN dotnet publish Gym.API/Gym.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production

# Render injects $PORT at runtime (it varies), so the listen URL is resolved in the shell
# at container start rather than baked in at build time.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Gym.API.dll"]
