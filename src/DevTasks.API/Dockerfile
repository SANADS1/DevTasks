FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/DevTasks.API/DevTasks.API.csproj", "src/DevTasks.API/"]
COPY ["src/DevTasks.Application/DevTasks.Application.csproj", "src/DevTasks.Application/"]
COPY ["src/DevTasks.Domain/DevTasks.Domain.csproj", "src/DevTasks.Domain/"]
COPY ["src/DevTasks.Infrastructure/DevTasks.Infrastructure.csproj", "src/DevTasks.Infrastructure/"]
RUN dotnet restore "src/DevTasks.API/DevTasks.API.csproj"
COPY . .
WORKDIR "/src/src/DevTasks.API"
RUN dotnet build "DevTasks.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DevTasks.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevTasks.API.dll"]