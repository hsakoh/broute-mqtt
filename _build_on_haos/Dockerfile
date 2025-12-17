ARG BUILD_FROM
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder

ARG BUILD_ARCH
ENV BUILD_ARCH=$BUILD_ARCH

WORKDIR /src
COPY /src .
RUN dotnet restore "BRouteMqttApp/BRouteMqttApp.csproj"
RUN \
  if [ "${BUILD_ARCH}" = "aarch64" ]; then \
      rid=linux-musl-arm64; \
  elif [ "${BUILD_ARCH}" = "amd64" ]; then \
      rid=linux-musl-x64; \
  else \
      rid=linux-musl-arm; \
  fi \
  && dotnet publish "BRouteMqttApp/BRouteMqttApp.csproj" -r $rid -p:PublishSingleFile=true --self-contained -c Release -o /app --no-restore

FROM $BUILD_FROM as final

ARG BUILD_ARCH
ENV BUILD_ARCH=$BUILD_ARCH

# Install requirements for add-on
RUN apk add --upgrade --no-cache bash ca-certificates-bundle libgcc libssl3 libstdc++

COPY --from=builder /app /app
ENTRYPOINT ["/app/BRouteMqttApp"]