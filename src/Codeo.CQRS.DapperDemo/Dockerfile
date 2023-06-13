FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Codeo.CQRS.Demo/Codeo.CQRS.Demo.csproj", "Codeo.CQRS.Demo/"]
RUN dotnet restore "Codeo.CQRS.Demo/Codeo.CQRS.Demo.csproj"
COPY . .
WORKDIR "/src/Codeo.CQRS.Demo"
RUN dotnet build "Codeo.CQRS.Demo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Codeo.CQRS.Demo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Codeo.CQRS.Demo.dll"]
