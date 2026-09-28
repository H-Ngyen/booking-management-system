FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/api.csproj", "src/"]
RUN dotnet restore "src/api.csproj"
COPY . .
RUN dotnet publish "src/api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# VnClock resolves "Asia/Ho_Chi_Minh" from the OS zoneinfo database at startup.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "api.dll"]
