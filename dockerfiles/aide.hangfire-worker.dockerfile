FROM ubuntu:22.04 AS builder
#FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-jammy AS builder

# install the .NET 6 SDK from the Ubuntu archive
# (no need to clean the apt cache as this is an unpublished stage)
RUN apt-get update && apt-get install -y dotnet6 ca-certificates

# IMPORTANT: Install icu-devtools and tzdata
# References:
# Images suitable for globalization
# https://learn.microsoft.com/en-us/dotnet/core/docker/container-images#images-suitable-for-globalization
# System.Globalization.CultureNotFoundException: Only the invariant culture is supported in globalization-invariant mode.
# https://kontext.tech/article/1271/only-the-invariant-culture-is-supported-in-globalization-invariant-mode
RUN apt-get -y install icu-devtools
RUN apt-get -y install tzdata

# add your application code
WORKDIR /source
COPY . .

# IMPORTANT:
# The .csproj needed these references to properly work in ubuntu:
# SkiaSharp.NativeAssets.Linux.NoDependencies 2.88.6
# HarfBuzzSharp.NativeAssets.Linux 7.3.0
# Reference:
# https://stackoverflow.com/questions/68990461/why-does-using-skiasharp-in-a-asp-net-core-web-api-cause-a-crash

# export your .NET app as a self-contained artefact
# IMPORTANT: Notice the configuration it's defaulted to DockerDev in order to load appsettings.DockerDev.json
# which contains all external services referencing to host.docker.internal for local dev purposes.
RUN dotnet publish Aide.Hangfire.Worker -c DockerDev -r ubuntu.22.04-x64 --self-contained true -o /app

#FROM ubuntu/dotnet-deps:6.0-22.04_beta
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-jammy

WORKDIR /app

COPY --from=builder /app ./

# Copy timezone data from the build image:
# Reference: https://github.com/dotnet/dotnet-docker/issues/4201#issuecomment-1308590491
COPY --from=builder /usr/share/zoneinfo /usr/share/zoneinfo

EXPOSE 80

ENTRYPOINT [ "/app/Aide.Hangfire.Worker" ]