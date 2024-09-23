#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Otus.Teaching.PromoCodeFactory.WebHost/Otus.Teaching.PromoCodeFactory.WebHost.csproj", "Otus.Teaching.PromoCodeFactory.WebHost/"]
COPY ["Otus.Teaching.PromoCodeFactory.Core/Otus.Teaching.PromoCodeFactory.Core.csproj", "Otus.Teaching.PromoCodeFactory.Core/"]
COPY ["Otus.Teaching.PromoCodeFactory.DataAccess/Otus.Teaching.PromoCodeFactory.DataAccess.csproj", "Otus.Teaching.PromoCodeFactory.DataAccess/"]
RUN dotnet restore "./Otus.Teaching.PromoCodeFactory.WebHost/Otus.Teaching.PromoCodeFactory.WebHost.csproj"
COPY . .
WORKDIR "/src/Otus.Teaching.PromoCodeFactory.WebHost"
RUN dotnet build "./Otus.Teaching.PromoCodeFactory.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Otus.Teaching.PromoCodeFactory.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Otus.Teaching.PromoCodeFactory.WebHost.dll"]