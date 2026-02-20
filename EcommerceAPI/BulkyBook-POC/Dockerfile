# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy central build files first so restore can be cached
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy project files
COPY ["Ecommerce.API/Ecommerce.API.csproj", "Ecommerce.API/"]
COPY ["Ecommerce.Application/Ecommerce.Application.csproj", "Ecommerce.Application/"]
COPY ["Ecommerce.Domain/Ecommerce.Domain.csproj", "Ecommerce.Domain/"]
COPY ["Ecommerce.Infrastructure/Ecommerce.Infrastructure.csproj", "Ecommerce.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Ecommerce.API/Ecommerce.API.csproj"

# Copy all source code
COPY . .

# Publish the application
WORKDIR "/src/Ecommerce.API"
RUN dotnet publish "Ecommerce.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Ecommerce.API.dll"]
