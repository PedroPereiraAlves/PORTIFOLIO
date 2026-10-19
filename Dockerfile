FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY PedroHub.sln ./
COPY src/PedroHub.Domain/PedroHub.Domain.csproj src/PedroHub.Domain/
COPY src/PedroHub.Shared/PedroHub.Shared.csproj src/PedroHub.Shared/
COPY src/PedroHub.Application/PedroHub.Application.csproj src/PedroHub.Application/
COPY src/PedroHub.Infrastructure/PedroHub.Infrastructure.csproj src/PedroHub.Infrastructure/
COPY src/PedroHub.Web/PedroHub.Web.csproj src/PedroHub.Web/

RUN dotnet restore src/PedroHub.Web/PedroHub.Web.csproj

COPY src/ ./src/
RUN dotnet publish src/PedroHub.Web/PedroHub.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PedroHub.Web.dll"]
