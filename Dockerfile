# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY Shared.Common/Shared.Common.csproj Shared.Common/
COPY Shared.Infrastructure/Shared.Infrastructure.csproj Shared.Infrastructure/
COPY AuthService/AuthService.csproj AuthService/
COPY ProductService/ProductService.csproj ProductService/
COPY OrderService/OrderService.csproj OrderService/
COPY ApiGateway/ApiGateway.csproj ApiGateway/

RUN dotnet restore AuthService/AuthService.csproj
RUN dotnet restore ProductService/ProductService.csproj
RUN dotnet restore OrderService/OrderService.csproj
RUN dotnet restore ApiGateway/ApiGateway.csproj



# Copy everything and build
COPY . .

# Build argument to select which service to build
ARG SERVICE_NAME=AuthService
ARG SERVICE_PATH=AuthService

RUN dotnet publish ${SERVICE_PATH}/${SERVICE_NAME}.csproj \
    -c Release \
    -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
RUN apk --no-cache add curl icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R appuser:appgroup /app/logs

USER appuser

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ARG SERVICE_NAME=AuthService
ENV SERVICE_NAME=${SERVICE_NAME}

ENTRYPOINT ["sh", "-c", "dotnet ${SERVICE_NAME}.dll"]