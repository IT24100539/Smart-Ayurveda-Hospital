FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Hospital.Domain/Hospital.Domain.csproj src/Hospital.Domain/
COPY src/Hospital.Application/Hospital.Application.csproj src/Hospital.Application/
COPY src/Hospital.Infrastructure/Hospital.Infrastructure.csproj src/Hospital.Infrastructure/
COPY src/Hospital.Api/Hospital.Api.csproj src/Hospital.Api/
RUN dotnet restore src/Hospital.Api/Hospital.Api.csproj
COPY src/ src/
RUN dotnet publish src/Hospital.Api/Hospital.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
CMD ["sh", "-c", "export ASPNETCORE_URLS=http://+:${PORT:-8080} && exec dotnet Hospital.Api.dll"]
