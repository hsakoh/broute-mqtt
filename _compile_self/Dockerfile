ARG BUILD_FROM
FROM $BUILD_FROM

ARG BUILD_ARCH
ENV BUILD_ARCH=$BUILD_ARCH

# Install requirements for add-on
RUN \
  apk add --no-cache \
    gcompat bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib dotnet6-runtime
    
COPY /$BUILD_ARCH /app
ENTRYPOINT ["/app/BRouteMqttApp"]