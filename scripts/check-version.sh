#!/usr/bin/env bash
# Checks that the CLI version matches the LiveSwitch SDK release it is built against.
# With a tag argument (e.g. v1.27.0), also checks that the tag matches the CLI version.
set -euo pipefail

PROJECT=src/FM.LiveSwitch.Hammer/FM.LiveSwitch.Hammer.csproj
VERSION=$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version)
SDK=$(dotnet msbuild "$PROJECT" -nologo -getProperty:LiveSwitchSdkVersion)
echo "Project version: $VERSION  SDK: $SDK"

# The first three components of the CLI version must be the SDK release (1.27.0.22319 -> 1.27.0).
if [ "$(echo "$VERSION" | cut -d. -f1-3)" != "$(echo "$SDK" | cut -d. -f1-3)" ]; then
  echo "Project version $VERSION does not match SDK release $SDK" >&2
  exit 1
fi

if [ $# -ge 1 ]; then
  TAG="${1#v}"
  if [ "$TAG" != "$VERSION" ]; then
    echo "Tag $1 does not match project version $VERSION" >&2
    exit 1
  fi
  echo "Tag $1 matches project version $VERSION"
fi
