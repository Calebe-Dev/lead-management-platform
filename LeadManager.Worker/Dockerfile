FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY lead-management-platform.sln ./
COPY LeadManager.Worker/LeadManager.Worker.csproj LeadManager.Worker/
COPY LeadManager.Application/LeadManager.Application.csproj LeadManager.Application/
COPY LeadManager.Domain/LeadManager.Domain.csproj LeadManager.Domain/
COPY LeadManager.Infrastructure/LeadManager.Infrastructure.csproj LeadManager.Infrastructure/
RUN dotnet restore LeadManager.Worker/LeadManager.Worker.csproj

COPY LeadManager.Worker/. LeadManager.Worker/
COPY LeadManager.Application/. LeadManager.Application/
COPY LeadManager.Domain/. LeadManager.Domain/
COPY LeadManager.Infrastructure/. LeadManager.Infrastructure/
RUN dotnet publish LeadManager.Worker/LeadManager.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LeadManager.Worker.dll"]
