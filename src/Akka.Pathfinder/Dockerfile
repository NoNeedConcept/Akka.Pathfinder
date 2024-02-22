FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_HTTP_PORTS=5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Akka.Pathfinder/Akka.Pathfinder.csproj", "Akka.Pathfinder/"]
COPY ["Akka.Pathfinder.Core/Akka.Pathfinder.Core.csproj", "Akka.Pathfinder.Core/"]
COPY ["Akka.Pathfinder.Proto/Akka.Pathfinder.Proto.csproj", "Akka.Pathfinder.Proto/"]
RUN dotnet restore "Akka.Pathfinder/Akka.Pathfinder.csproj"
COPY . .
WORKDIR "/src/Akka.Pathfinder"
RUN dotnet build "./Akka.Pathfinder.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Akka.Pathfinder.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Akka.Pathfinder.dll"]
