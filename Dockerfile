FROM microsoft/aspnetcore:2.0.0

ENV ASPNETCORE_ENVIRONMENT Production

COPY ./entrypoint.sh /

RUN chmod +x /entrypoint.sh

WORKDIR /app

COPY ./bin/Release/netcoreapp2.0/publish /app

CMD ["dotnet", "PatreonService.dll"]