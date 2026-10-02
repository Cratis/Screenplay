# syntax=docker/dockerfile:1

# The Screenplay tool, with its MCP server, as a container image.
#
#   docker run -i --rm -v "$PWD/specifications:/model" cratis/screenplay mcp /model
#
# Build from the repository root:
#
#   docker build --build-arg VERSION=1.2.3 -t cratis/screenplay .

####################################
# Event model board - the MCP App the server embeds
####################################
# Both build stages run on the build platform: their output (a bundled HTML page, portable assemblies) is
# the same for every architecture, so a multi-platform build needs no emulation.
FROM --platform=$BUILDPLATFORM node:23-bookworm-slim AS board

WORKDIR /src
COPY . .

RUN corepack enable && yarn install
RUN yarn workspaces foreach -Rt --from @cratis/screenplay-mcp-app run build

####################################
# Tool - compiled with the board embedded
####################################
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG VERSION=0.0.0

WORKDIR /src
COPY . .
COPY --from=board /src/Source/Screenplay/McpApp/dist ./Source/Screenplay/McpApp/dist

RUN dotnet publish Source/DotNET/Tool \
    --configuration Release \
    -p:Version=${VERSION} \
    -p:UseAppHost=false \
    -p:EnableSourceControlManagerQueries=false \
    -p:EnableSourceLink=false \
    -p:PublishRepositoryUrl=false \
    -p:EmbedUntrackedSources=false \
    -p:IncludeSource=false \
    --output /out

####################################
# Runtime
####################################
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled

LABEL org.opencontainers.image.title="Cratis Screenplay" \
      org.opencontainers.image.description="Compiler, CLI and MCP server for Cratis Screenplay (.play) models" \
      org.opencontainers.image.source="https://github.com/Cratis/Screenplay" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
COPY --from=build /out ./

# The MCP server speaks JSON-RPC on standard input and output, so the container is run with -i and no TTY.
# Mount the model directory and pass its path: `mcp /model`.
ENTRYPOINT ["dotnet", "/app/Cratis.Screenplay.Tool.dll"]
