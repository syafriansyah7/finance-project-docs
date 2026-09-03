FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Finance.sln ./
COPY src/Finance.Domain/Finance.Domain.csproj src/Finance.Domain/
COPY src/Finance.Application/Finance.Application.csproj src/Finance.Application/
COPY src/Finance.Infrastructure/Finance.Infrastructure.csproj src/Finance.Infrastructure/
COPY src/Finance.Api/Finance.Api.csproj src/Finance.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Finance.Api/Finance.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Finance.Api.dll"]
