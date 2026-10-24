# PeladaBot image: compiled with the .NET SDK, run on the ASP.NET Core runtime only.
# Local usage (with WAHA): see the README.

# --- Stage 1: build ----------------------------------------------------------
# --platform=$BUILDPLATFORM keeps the SDK native when building an image for
# another CPU (an amd64 image on an Apple Silicon Mac, for example). The output
# is portable IL, so it runs on the runtime image of any architecture.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Restore from the files that define the build only, so the NuGet layer is reused
# while just the source code changes. global.json pins the SDK (10.0.302,
# rollForward latestFeature): an SDK image that does not satisfy it fails here.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/PeladaBot/PeladaBot.csproj src/PeladaBot/
RUN dotnet restore src/PeladaBot/PeladaBot.csproj

# .editorconfig takes part in the build (EnforceCodeStyleInBuild with
# TreatWarningsAsErrors), so the image is built under the same rules as CI.
COPY .editorconfig ./
COPY src/ src/

# ImageSharp 4 fails a Release build without a Six Labors license key.
# Railway passes the service variable of the same name as a build argument;
# docker compose passes it from .env. It exists only in this build stage, so
# the runtime image never carries it. MSBuild reads SixLaborsLicenseKey from
# the environment.
ARG SIXLABORS_LICENSE_KEY
RUN SixLaborsLicenseKey="$SIXLABORS_LICENSE_KEY" dotnet publish src/PeladaBot/PeladaBot.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# --- Stage 2: runtime --------------------------------------------------------
# Full Ubuntu 24.04 (noble) image: .NET 10 no longer ships Debian images, and the
# plain 10.0 tag points to this same image. Not Alpine or chiseled, because the
# bot needs ICU (accent-insensitive name comparison) and tzdata (IANA time zones);
# this image already ships both (libicu74 and tzdata), so nothing is installed.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

# Fail fast instead of comparing names wrongly: with invariant globalization
# off, the runtime refuses to start when ICU is missing from the image.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# Kestrel listens on 8080 (already the image default, written down on purpose).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# The non-root user that comes with the image (app, UID 1654). The published
# files stay owned by root, so the process cannot change its own binaries.
USER $APP_UID

ENTRYPOINT ["dotnet", "PeladaBot.dll"]
