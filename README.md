# LiveSwitch Hammer CLI

![build](https://github.com/liveswitch/liveswitch-hammer/workflows/build/badge.svg) ![code quality](https://app.codacy.com/project/badge/Grade/9a3b33b63b254b118fcdd80e807cba8c) ![license](https://img.shields.io/badge/License-MIT-yellow.svg) ![release](https://img.shields.io/github/v/release/liveswitch/liveswitch-hammer.svg)

The LiveSwitch Hammer CLI lets you run specific automated tests against a LiveSwitch deployment.

## Building

Use `dotnet publish` to create a single, self-contained file for a specific platform/architecture:

### Windows
```none
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -o win
```

### macOS
```none
dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -o osx
```

### Linux
```none
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -o linux
```

Alternatively, use `dotnet build` to create a platform-agnostic bundle (the .NET Core runtime must be installed):

```none
dotnet build
```

Using this approach will generate a library instead of an executable. Use `dotnet lshammer.dll` instead of `lshammer` to run it.

## Usage

```none
lsconnect [verb] [options]
```

### Verbs

```none
  cluster    Tests scenarios that evoke clustering edge cases.

  load       Tests parallel and sequential load scenarios.
```

## capture

The `cluster` verb connects two clients in parallel to the same channel and then opens two MCU connections concurrently. The content of the media flow is monitored to ensure that the generated sounds and images are sent and received as expected.

This test is intended to be used with multiple Media Servers that cluster together. To ensure this takes place, do either of the following:

1.  Set the Deployment > Clustering > Strategy to `RoundRobin`.
2.  Assign unique regions to the Media Servers and provide them as options to `lshammer`.

```none
  --iteration-count       (Default: 1000) The number of iterations to run.

  --media-timeout         (Default: 5) The number of seconds to wait for media
                          to flow.

  --user1                 (Default: user1) The user ID of the first client.

  --user2                 (Default: user2) The user ID of the second client.

  --device1               (Default: device1) The device ID of the first client.

  --device2               (Default: device2) The device ID of the second client.

  --region1               (Default: region1) The region of the first client.

  --region2               (Default: region2) The region of the second client.

  -g, --gateway-url       (Default: http://localhost:8080/sync) The Gateway URL.

  -a, --application-id    (Default: my-app-id) The application identifier.

  -s, --shared-secret     (Default: --replaceThisWithYourOwnSharedSecret--) The
                          shared secret.
```

## load

The `load` verb connects a configurable number of clients to a configurable number of channels, and then opens a configurable number of MCU connections. The degree of parallelism in each of these three stages (client register, channel join, connection open) is configurable.

There are two ways to parallelize the channel join and connection open tasks:

1.  Distribute evenly across channels. This is the default behaviour.
2.  Fill up one channel before moving to the next, enabled by `--channel-burst`.

```none
  --iteration-count              (Default: 1) The number of iterations to run.

  --client-count                 (Default: 1) The number of clients to register.

  --channel-count                (Default: 1) The number of channels for each
                                 client to join.

  --connection-count             (Default: 1) The number of connections in each
                                 channel to open.

  --parallel-client-registers    (Default: 1) The number of parallel client
                                 register operations.

  --parallel-channel-joins       (Default: 1) The number of parallel channel
                                 join operations.

  --parallel-connection-opens    (Default: 1) The number of parallel connection
                                 open operations.

  --channel-burst                (Default: false) Group traffic bursts by
                                 channel.

  --pause-timeout                (Default: 0) The number of seconds to wait
                                 before closing connections.

  -g, --gateway-url              (Default: http://localhost:8080/sync) The
                                 Gateway URL.

  -a, --application-id           (Default: my-app-id) The application
                                 identifier.

  -s, --shared-secret            (Default:
                                 --replaceThisWithYourOwnSharedSecret--) The
                                 shared secret.
```

## Contact

To learn more, visit [frozenmountain.com](https://www.frozenmountain.com) or [liveswitch.io](https://www.liveswitch.io).

For inquiries, contact [sales@frozenmountain.com](mailto:sales@frozenmountain.com).

All contents copyright © Frozen Mountain Software.