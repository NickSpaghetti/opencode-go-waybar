# .NET 10 SDK (Debian Bookworm) for base + dev stages.
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG DOTNET_RUNTIME_DEPS_IMAGE=mcr.microsoft.com/dotnet/runtime-deps:10.0-noble

FROM ${DOTNET_SDK_IMAGE} AS base
WORKDIR /workspace

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    NUGET_PACKAGES=/workspace/.nuget/packages

# Copy only the project files first for a cached restore.
COPY global.json Directory.Build.props* Directory.Packages.props* NuGet.Config* ./
COPY src/OpencodeGoWaybar/OpencodeGoWaybar.csproj src/OpencodeGoWaybar/OpencodeGoWaybar.csproj
COPY tests/OpencodeGoWaybar.UnitTests/OpencodeGoWaybar.UnitTests.csproj tests/OpencodeGoWaybar.UnitTests/OpencodeGoWaybar.UnitTests.csproj

RUN --mount=type=cache,target=/workspace/.nuget/packages,sharing=locked \
    dotnet restore src/OpencodeGoWaybar/OpencodeGoWaybar.csproj \
 && dotnet restore tests/OpencodeGoWaybar.UnitTests/OpencodeGoWaybar.UnitTests.csproj

# Copy the rest of the source tree. This invalidates less cache than copying it
# before the restore.
COPY . .

FROM base AS dev
# The dev stage adds no extra tooling. It exists so the Makefile can build a
# stable image name and other targets can base on it explicitly.

FROM ${DOTNET_RUNTIME_DEPS_IMAGE} AS prod
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
        ca-certificates \
 && rm -rf /var/lib/apt/lists/*

# Publish the NativeAOT binary in the base stage and copy only the published
# output into the prod verification image.
FROM base AS publish
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
        clang \
        zlib1g-dev \
 && rm -rf /var/lib/apt/lists/* \
 && dotnet publish src/OpencodeGoWaybar/OpencodeGoWaybar.csproj \
        --configuration Release \
        --runtime linux-x64

FROM prod AS final
COPY --from=publish /workspace/src/OpencodeGoWaybar/bin/Release/net10.0/linux-x64/publish/ /app/
RUN chmod +x /app/opencode-go-waybar
WORKDIR /app
ENTRYPOINT ["/app/opencode-go-waybar"]