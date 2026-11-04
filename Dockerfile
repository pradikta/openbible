# syntax=docker/dockerfile:1

# Build stage runs on the builder's native architecture and cross-publishes for the target.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/OpenBible.Domain/OpenBible.Domain.csproj src/OpenBible.Domain/
COPY src/OpenBible.Application/OpenBible.Application.csproj src/OpenBible.Application/
COPY src/OpenBible.Infrastructure/OpenBible.Infrastructure.csproj src/OpenBible.Infrastructure/
COPY src/OpenBible.Wasm/OpenBible.Wasm.csproj src/OpenBible.Wasm/
COPY src/OpenBible.Server/OpenBible.Server.csproj src/OpenBible.Server/
RUN case "$TARGETARCH" in amd64) RID_ARCH=x64 ;; *) RID_ARCH="$TARGETARCH" ;; esac \
    && dotnet restore src/OpenBible.Server/OpenBible.Server.csproj -a "$RID_ARCH"

COPY src/ src/
RUN case "$TARGETARCH" in amd64) RID_ARCH=x64 ;; *) RID_ARCH="$TARGETARCH" ;; esac \
    && dotnet publish src/OpenBible.Server/OpenBible.Server.csproj \
        -c Release -a "$RID_ARCH" --no-restore --no-self-contained -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG REVISION=dev
ARG BUILD_DATE=unknown
WORKDIR /app
COPY --from=build /app ./

# The base image already runs as the non-root `app` user and listens on 8080.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    OPENBIBLE_REVISION=$REVISION \
    OPENBIBLE_BUILD_DATE=$BUILD_DATE
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpenBible.Server.dll"]
