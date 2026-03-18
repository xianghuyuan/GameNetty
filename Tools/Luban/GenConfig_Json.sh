#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

WORKSPACE=../..
LUBAN_DLL=$WORKSPACE/Tools/Luban/LubanRelease/Luban.dll
CONF_ROOT=$WORKSPACE/Config/Json

echo "==================== 开始生成 JSON 配置 ===================="

# 生成客户端 JSON 配置
dotnet $LUBAN_DLL \
    -t Client \
    -c cs-bin \
    -d bin \
    --conf $CONF_ROOT/__luban__.conf \
    -x outputCodeDir=$WORKSPACE/Unity/Assets/GameScripts/HotFix/GameProto/Generate/JsonConfig \
    -x bin.outputDataDir=$WORKSPACE/Unity/Assets/AssetRaw/Configs \
    -x lineEnding=LF

if [ $? -ne 0 ]; then
    echo "生成失败！"
    exit 1
fi

echo "==================== JSON 配置生成完成 ===================="
