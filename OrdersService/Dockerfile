# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HSEShop.sln ./
COPY OrdersService/OrdersService.csproj OrdersService/
COPY Contracts/Contracts.csproj Contracts/

RUN dotnet restore OrdersService/OrdersService.csproj

COPY . .
RUN dotnet publish OrdersService/OrdersService.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- run ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5042
ENTRYPOINT ["dotnet", "OrdersService.dll"]
