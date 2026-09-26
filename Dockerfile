# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["dentist-clinic-api.csproj", "./"]
RUN dotnet restore "dentist-clinic-api.csproj"

COPY . .

RUN dotnet publish "dentist-clinic-api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "dentist-clinic-api.dll"]