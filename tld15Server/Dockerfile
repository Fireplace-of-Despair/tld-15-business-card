# ---------------------------------------------------------------------------
# Restore
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:11.0-preview-alpine AS restore

ARG TARGET_RUNTIME=linux-musl-x64
WORKDIR /src

COPY global.json ./
COPY StainlessGenerators/StainlessGenerators.csproj StainlessGenerators/
COPY StainlessCore/StainlessCore.csproj StainlessCore/
COPY StainlessInfrastructure/StainlessInfrastructure.csproj StainlessInfrastructure/
COPY tld15Server/tld15Server.csproj tld15Server/

RUN dotnet restore tld15Server/tld15Server.csproj --runtime $TARGET_RUNTIME

# ---------------------------------------------------------------------------
# Publish
# ---------------------------------------------------------------------------
FROM restore AS publish

ARG BUILD_CONFIGURATION=Release
ARG TARGET_RUNTIME=linux-musl-x64

COPY .editorconfig ./
# The generator writes StainlessGenerated.Registry into the build of the server. Without its sources
# the project still compiles into an empty analyzer, and tld15Server fails with CS0246.
COPY StainlessGenerators/ StainlessGenerators/
COPY StainlessCore/ StainlessCore/
COPY StainlessInfrastructure/ StainlessInfrastructure/
COPY tld15Server/ tld15Server/

RUN dotnet publish tld15Server/tld15Server.csproj \
    --configuration $BUILD_CONFIGURATION \
    --runtime $TARGET_RUNTIME \
    --self-contained false \
    -p:UseAppHost=false \
    --output /app/publish

# Two groups of files that only a developer reads. The XML documentation feeds an editor, and the
# source map feeds the JavaScript debugger of a browser. The server reads neither one.
RUN rm -f /app/publish/*.xml \
    && find /app/publish/wwwroot -name "*.map" -delete \
    && find /app/publish/wwwroot -name "*.map.br" -delete \
    && find /app/publish/wwwroot -name "*.map.gz" -delete

# ---------------------------------------------------------------------------
# Runtime
# ---------------------------------------------------------------------------
# The -extra tag carries icu-libs, icu-data-full and tzdata already. The plain tag needs an apk layer
# for the same three packages, and that layer costs more than the difference between the two tags.
FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine-extra AS final

ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/UTC /etc/localtime && echo UTC > /etc/timezone

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8

WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/logs && chown -R $APP_UID:$APP_UID /app/logs

EXPOSE 8080
USER $APP_UID

# /api/public/ping answers "pong" with a live database and "wrong" without one. Both carry HTTP 200,
# so the check reads the body.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -q -T 4 -O - http://127.0.0.1:8080/api/public/ping | grep -q pong

ENTRYPOINT ["dotnet", "tld15Server.dll"]
