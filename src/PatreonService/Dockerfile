FROM ghcr.io/sitkoru/dotnet-runtime:5
   
ARG version
ENV APP_VERSION=$version
LABEL VERSION=$version

WORKDIR /app
COPY . .

CMD ["dotnet", "PatreonService.dll"]