#!/bin/bash

cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

WORKSPACE="../.."

DOTNET="/opt/homebrew/bin/dotnet"
if [ ! -f "$DOTNET" ]; then
    DOTNET="dotnet"
fi

LUBAN_DLL="$WORKSPACE/Tools/Luban/LubanRelease/Luban.dll"
CONF_ROOT="$WORKSPACE/Config/Excel"

# Server GameConfig
"$DOTNET" "$LUBAN_DLL" \
    --customTemplateDir ServerTemplate \
    -t All \
    -c cs-bin \
    -d bin \
    --conf "$CONF_ROOT/GameConfig/__luban__.conf" \
    -x outputCodeDir="$WORKSPACE/Server/Model/Generate/Config" \
    -x bin.outputDataDir="$WORKSPACE/Config/Generate/" \
    -x lineEnding=LF

echo "==================== FuncConfig : GenServerFinish ===================="

if [ $? -ne 0 ]; then
    echo "An error occurred."
    exit 1
fi

# StartConfig Localhost (代码结构 + 数据)
"$DOTNET" "$LUBAN_DLL" \
    --customTemplateDir ServerTemplate \
    -t Localhost \
    -c cs-bin \
    -d bin \
    --conf "$CONF_ROOT/StartConfig/__luban__.conf" \
    -x outputCodeDir="$WORKSPACE/Server/Model/Generate/Config/StartConfig" \
    -x bin.outputDataDir="$WORKSPACE/Config/Generate/StartConfig/Localhost" \
    -x lineEnding=LF

echo "==================== StartConfig : GenLocalhostFinish ===================="

if [ $? -ne 0 ]; then
    echo "An error occurred."
    exit 1
fi

# # StartConfig Release
# "$DOTNET" "$LUBAN_DLL" \
#     --customTemplateDir ServerTemplate \
#     -t Release \
#     -c cs-bin \
#     -d bin \
#     --conf "$CONF_ROOT/StartConfig/__luban__.conf" \
#     -x outputCodeDir="$WORKSPACE/Server/Model/Generate/Config/StartConfig" \
#     -x bin.outputDataDir="$WORKSPACE/Config/Generate/StartConfig/Release" \
#     -x lineEnding=LF
#
# echo "==================== StartConfig : GenReleaseFinish ===================="
#
# if [ $? -ne 0 ]; then
#     echo "An error occurred."
#     exit 1
# fi

# # StartConfig Benchmark
# "$DOTNET" "$LUBAN_DLL" \
#     --customTemplateDir ServerTemplate \
#     -t Benchmark \
#     -d bin \
#     --conf "$CONF_ROOT/StartConfig/__luban__.conf" \
#     -x bin.outputDataDir="$WORKSPACE/Config/Generate/StartConfig/Benchmark"
#
# echo "==================== StartConfig : GenBenchmarkFinish ===================="
#
# if [ $? -ne 0 ]; then
#     echo "An error occurred."
#     exit 1
# fi

# # StartConfig RouterTest
# "$DOTNET" "$LUBAN_DLL" \
#     --customTemplateDir ServerTemplate \
#     -t RouterTest \
#     -d bin \
#     --conf "$CONF_ROOT/StartConfig/__luban__.conf" \
#     -x bin.outputDataDir="$WORKSPACE/Config/Generate/StartConfig/RouterTest"
#
# echo "==================== StartConfig : GenRouterTestFinish ===================="
