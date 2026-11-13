FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY FM.LiveSwitch.Hammer.sln FM.LiveSwitch.Hammer.sln
COPY src src
RUN dotnet restore
RUN dotnet publish src/FM.LiveSwitch.Hammer/FM.LiveSwitch.Hammer.csproj -c Release -r linux-x64 --self-contained false -o lib
RUN rm -rf src
RUN rm FM.LiveSwitch.Hammer.sln

# Pin the OS to noble (Ubuntu 24.04) so the base does not change under us.
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble
WORKDIR /app
COPY --from=build /app/lib .

ENTRYPOINT ["dotnet", "lshammer.dll"]
