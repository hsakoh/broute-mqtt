ARG BUILD_FROM
FROM $BUILD_FROM

ARG BUILD_ARCH
ENV BUILD_ARCH=$BUILD_ARCH

# Install requirements for add-on
RUN \
  apk add --no-cache \
    bash ca-certificates-bundle libgcc libssl3 libstdc++ zlib dotnet9-runtime
    
COPY /$BUILD_ARCH /app
ENTRYPOINT ["/app/BRouteMqttApp"]