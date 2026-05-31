FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AyaPOS.Api/AyaPOS.Api.csproj", "AyaPOS.Api/"]
RUN dotnet restore "AyaPOS.Api/AyaPOS.Api.csproj"
COPY . .
WORKDIR "/src/AyaPOS.Api"
RUN dotnet publish "AyaPOS.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AYAPOS.Api.dll"]
