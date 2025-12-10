FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/StackFood.Payments.API/StackFood.Payments.API/StackFood.Payments.API.csproj", "src/StackFood.Payments.API/StackFood.Payments.API/"]
COPY ["src/StackFood.Payments.Application/StackFood.Payments.Application/StackFood.Payments.Application.csproj", "src/StackFood.Payments.Application/StackFood.Payments.Application/"]
COPY ["src/StackFood.Payments.Domain/StackFood.Payments.Domain/StackFood.Payments.Domain.csproj", "src/StackFood.Payments.Domain/StackFood.Payments.Domain/"]
COPY ["src/StackFood.Payments.Infrastructure/StackFood.Payments.Infrastructure/StackFood.Payments.Infrastructure.csproj", "src/StackFood.Payments.Infrastructure/StackFood.Payments.Infrastructure/"]
RUN dotnet restore "src/StackFood.Payments.API/StackFood.Payments.API/StackFood.Payments.API.csproj"
COPY . .
WORKDIR "/src/src/StackFood.Payments.API/StackFood.Payments.API"
RUN dotnet build "StackFood.Payments.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StackFood.Payments.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StackFood.Payments.API.dll"]
