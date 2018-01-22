FROM microsoft/aspnetcore-build:2.0.5-2.1.4 AS builder
WORKDIR /source

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish --output /app/ --configuration Release

FROM microsoft/aspnetcore:2.0.5
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "PatreonService.dll"]