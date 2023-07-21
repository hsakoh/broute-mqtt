dotnet restore "src/BRouteMqttApp/BRouteMqttApp.csproj"
dotnet publish "src/BRouteMqttApp/BRouteMqttApp.csproj" -r linux-musl-arm64 -p:PublishSingleFile=true --self-contained false -c Release -o "./_compile_self/aarch64" --no-restore
dotnet publish "src/BRouteMqttApp/BRouteMqttApp.csproj" -r linux-musl-x64 -p:PublishSingleFile=true --self-contained false -c Release -o "./_compile_self/amd64" --no-restore
