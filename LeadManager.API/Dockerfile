FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY lead-management-platform.sln ./
COPY LeadManager.API/LeadManager.API.csproj LeadManager.API/
COPY LeadManager.Application/LeadManager.Application.csproj LeadManager.Application/
COPY LeadManager.Domain/LeadManager.Domain.csproj LeadManager.Domain/
COPY LeadManager.Infrastructure/LeadManager.Infrastructure.csproj LeadManager.Infrastructure/
RUN dotnet restore LeadManager.API/LeadManager.API.csproj

COPY LeadManager.API/. LeadManager.API/
COPY LeadManager.Application/. LeadManager.Application/
COPY LeadManager.Domain/. LeadManager.Domain/
COPY LeadManager.Infrastructure/. LeadManager.Infrastructure/
RUN dotnet publish LeadManager.API/LeadManager.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LeadManager.API.dll"]
