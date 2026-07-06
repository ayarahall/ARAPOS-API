FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# OCR toolchain for the Documents feature: tesseract (Arabic + English) for text
# extraction, poppler-utils for rasterizing PDF pages to images before OCR.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr tesseract-ocr-ara poppler-utils \
    && rm -rf /var/lib/apt/lists/*

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
