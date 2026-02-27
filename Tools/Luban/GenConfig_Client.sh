#!/bin/bash

cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

WORKSPACE="../.."

# dotnet 完整路径（兼容从 Unity 启动时 PATH 不完整的情况）
DOTNET="/opt/homebrew/bin/dotnet"
if [ ! -f "$DOTNET" ]; then
    DOTNET="dotnet"
fi

LUBAN_DLL="$WORKSPACE/Tools/Luban/LubanRelease/Luban.dll"
CONF_ROOT="$WORKSPACE/Config/Excel/GameConfig"

# Client
"$DOTNET" "$LUBAN_DLL" \
    --customTemplateDir CustomTemplate \
    -t Client \
    -c cs-bin \
    -d bin \
    --conf "$CONF_ROOT/__luban__.conf" \
    -x outputCodeDir="$WORKSPACE/Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config" \
    -x bin.outputDataDir="$WORKSPACE/Config/Generate/GameConfig/c" \
    -x lineEnding=LF

echo "==================== FuncConfig : GenClientFinish ===================="
