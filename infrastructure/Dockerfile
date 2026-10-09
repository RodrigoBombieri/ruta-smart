FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/ src/
RUN dotnet publish src/RutaSmart.Api -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
COPY mobile/rutasmart/build/web /app/wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RutaSmart.Api.dll"]
