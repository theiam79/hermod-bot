#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Hermod.Bot/Hermod.Bot.csproj", "src/Hermod.Bot/"]
COPY ["src/Hermod.BGstats/Hermod.BGstats.csproj", "src/Hermod.BGstats/"]
COPY ["src/Hermod.Core/Hermod.Core.csproj", "src/Hermod.Core/"]
COPY ["src/Hermod.Data/Hermod.Data.csproj", "src/Hermod.Data/"]
RUN dotnet restore "src/Hermod.Bot/Hermod.Bot.csproj"
COPY . .
WORKDIR "/src/src/Hermod.Bot"
RUN dotnet build "Hermod.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hermod.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hermod.Bot.dll"]