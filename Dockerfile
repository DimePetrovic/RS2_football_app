FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ARG SERVICE_DIR
ARG SERVICE_NAME

COPY Comeback.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/building-blocks/ src/building-blocks/
COPY src/services/${SERVICE_DIR}/ src/services/${SERVICE_DIR}/

RUN dotnet restore src/services/${SERVICE_DIR}/Comeback.${SERVICE_NAME}.Api/Comeback.${SERVICE_NAME}.Api.csproj
RUN dotnet publish  src/services/${SERVICE_DIR}/Comeback.${SERVICE_NAME}.Api/Comeback.${SERVICE_NAME}.Api.csproj \
    -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ARG SERVICE_NAME
ENV SERVICE_NAME=${SERVICE_NAME}
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet Comeback.${SERVICE_NAME}.Api.dll"]
