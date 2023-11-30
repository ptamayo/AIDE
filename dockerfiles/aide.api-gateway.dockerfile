FROM ubuntu:22.04 AS builder

# install the .NET 6 SDK from the Ubuntu archive
# (no need to clean the apt cache as this is an unpublished stage)
RUN apt-get update && apt-get install -y dotnet6 ca-certificates

# Copy the ocelot.json from the release settings (this file contains the based url set to host.docker.internal)
# Notice the file will be copied to a tmp folder separated of the source code.
WORKDIR /tmp
COPY ./Release/Settings/Local/docker/ocelot.json .

# add your application code
WORKDIR /source
COPY . .

# export your .NET app as a self-contained artefact
# IMPORTANT: Notice the configuration it's defaulted to DockerDev in order to load appsettings.DockerDev.json
# which contains all external services referencing to host.docker.internal for local dev purposes.
RUN dotnet publish Aide.ApiGateway -c DockerDev -r ubuntu.22.04-x64 --self-contained true -o /app

#FROM ubuntu/dotnet-deps:6.0-22.04_beta
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-jammy

WORKDIR /app
COPY --from=builder /app ./

# IMPORTANT: This will overwrite the default ocelot.json file with another version compatible with docker (for local dev purposes).
COPY --from=builder /tmp/ocelot.json ./

EXPOSE 80

ENTRYPOINT ["/app/Aide.ApiGateway"]