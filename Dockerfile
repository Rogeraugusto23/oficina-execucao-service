FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ExecucaoService.Domain/ExecucaoService.Domain.csproj src/ExecucaoService.Domain/
COPY src/ExecucaoService.Application/ExecucaoService.Application.csproj src/ExecucaoService.Application/
COPY src/ExecucaoService.Infrastructure/ExecucaoService.Infrastructure.csproj src/ExecucaoService.Infrastructure/
COPY src/ExecucaoService.API/ExecucaoService.API.csproj src/ExecucaoService.API/
RUN dotnet restore src/ExecucaoService.API/ExecucaoService.API.csproj

COPY src/ src/
RUN dotnet publish src/ExecucaoService.API/ExecucaoService.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ExecucaoService.API.dll"]
