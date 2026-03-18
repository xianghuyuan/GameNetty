@echo off
cd /d %~dp0
echo %CD%

set WORKSPACE=..\..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\LubanRelease\Luban.dll
set CONF_ROOT=%WORKSPACE%\Config\Json

echo ==================== 开始生成 JSON 配置 ====================

:: 生成客户端 JSON 配置
dotnet %LUBAN_DLL% ^
    -t Client ^
    -c cs-bin ^
    -d bin ^
    --conf %CONF_ROOT%\__luban__.conf ^
    -x outputCodeDir=%WORKSPACE%\Unity\Assets\GameScripts\HotFix\GameProto\Generate\JsonConfig ^
    -x bin.outputDataDir=%WORKSPACE%\Unity\Assets\AssetRaw\Configs ^
    -x lineEnding=CRLF

if %ERRORLEVEL% NEQ 0 (
    echo 生成失败！
    pause
    exit /b
)

echo ==================== JSON 配置生成完成 ====================
pause
