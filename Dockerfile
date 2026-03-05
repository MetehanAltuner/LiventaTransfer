FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LiventaTransfer.slnx .
COPY src/LiventaTransfer.Domain/LiventaTransfer.Domain.csproj src/LiventaTransfer.Domain/
COPY src/LiventaTransfer.Application/LiventaTransfer.Application.csproj src/LiventaTransfer.Application/
COPY src/LiventaTransfer.Infrastructure/LiventaTransfer.Infrastructure.csproj src/LiventaTransfer.Infrastructure/
COPY src/LiventaTransfer.API/LiventaTransfer.API.csproj src/LiventaTransfer.API/

RUN dotnet restore src/LiventaTransfer.API/LiventaTransfer.API.csproj

COPY src/ src/
RUN dotnet publish src/LiventaTransfer.API/LiventaTransfer.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "LiventaTransfer.API.dll"]
