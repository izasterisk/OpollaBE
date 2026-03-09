# Multi-stage build for optimized production deployment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files in dependency order for better caching
COPY ["BLL/BLL.csproj", "BLL/"]
COPY ["DAL/DAL.csproj", "DAL/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["OpollaBE/OpollaBE.csproj", "OpollaBE/"]

# Restore dependencies
RUN dotnet restore "OpollaBE/OpollaBE.csproj"

# Copy all source code
COPY . .

# Build and publish the application
WORKDIR "/src/OpollaBE"
RUN dotnet publish "OpollaBE.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final production image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Expose port (Render will use the PORT environment variable)
EXPOSE 8080

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "OpollaBE.dll"]
