# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HSEShop.sln ./
COPY PaymentsService/PaymentsService.csproj PaymentsService/
COPY Contracts/Contracts.csproj Contracts/

RUN dotnet restore PaymentsService/PaymentsService.csproj

COPY . .
RUN dotnet publish PaymentsService/PaymentsService.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- run ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5108
ENTRYPOINT ["dotnet", "PaymentsService.dll"]
