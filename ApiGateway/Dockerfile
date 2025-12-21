# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HSEShop.sln ./
COPY ApiGateway/ApiGateway.csproj ApiGateway/
COPY Contracts/Contracts.csproj Contracts/

RUN dotnet restore ApiGateway/ApiGateway.csproj

COPY . .
RUN dotnet publish ApiGateway/ApiGateway.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- run ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5271
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
