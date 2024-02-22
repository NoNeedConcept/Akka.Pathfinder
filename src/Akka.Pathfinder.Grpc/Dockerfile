FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Akka.Pathfinder.Grpc/Akka.Pathfinder.Grpc.csproj", "Akka.Pathfinder.Grpc/"]
COPY ["Akka.Pathfinder.Proto/Akka.Pathfinder.Proto.csproj", "Akka.Pathfinder.Proto/"]
COPY ["Akka.Pathfinder.Core/Akka.Pathfinder.Core.csproj", "Akka.Pathfinder.Core/"]
RUN dotnet restore "Akka.Pathfinder.Grpc/Akka.Pathfinder.Grpc.csproj"
COPY . .
WORKDIR "/src/Akka.Pathfinder.Grpc"
RUN dotnet build "./Akka.Pathfinder.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Akka.Pathfinder.Grpc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Akka.Pathfinder.Grpc.dll"]
