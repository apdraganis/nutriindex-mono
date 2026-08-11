FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY src/NutriIndex.Core/NutriIndex.Core.csproj src/NutriIndex.Core/
COPY src/NutriIndex.Api/NutriIndex.Api.csproj src/NutriIndex.Api/
COPY src/NutriIndex.Web/NutriIndex.Web.csproj src/NutriIndex.Web/

RUN dotnet restore src/NutriIndex.Api/NutriIndex.Api.csproj

COPY src/ src/

RUN dotnet publish src/NutriIndex.Api/NutriIndex.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "NutriIndex.Api.dll"]
