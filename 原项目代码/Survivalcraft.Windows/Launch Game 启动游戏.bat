@echo off
rem Survivalcraft API Launcher / 生存战争插件版启动器
rem Check the .NET runtime, then launch the game / 检测 .NET 运行时后启动游戏
setlocal EnableExtensions
chcp 65001 >nul
cd /d "%~dp0"
title Survivalcraft API Launcher / 生存战争插件版启动器

echo =======================================================
echo    Survivalcraft API Launcher / 生存战争插件版启动器
echo =======================================================
echo.

rem ===== [1/2] .NET .NET Desktop Runtime / 桌面运行时 =====
echo [1/2] Checking .NET Desktop Runtime 10.0 / 检测 .NET 桌面运行时 10.0 ...
set "DOTNET_OK=1"
where dotnet >nul 2>nul || set "DOTNET_OK=0"
if "%DOTNET_OK%"=="1" (
    dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 10." >nul || set "DOTNET_OK=0"
)
if "%DOTNET_OK%"=="1" (
    echo       [OK] Installed / 已安装
) else (
    echo       [X] .NET Desktop Runtime 10.0 x64 not found
    echo       [X] 未检测到 .NET 桌面运行时 10.0 x64
    echo       Download the ".NET Desktop Runtime 10.x" x64 installer from:
    echo       请前往以下页面，下载并安装 ".NET 桌面运行时 10.x" 的 x64 版本：
    echo       https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0
    echo       Run this script again after installing.
    echo       安装完成后，重新运行本脚本。
    echo.
    echo       Press any key to open the download page / 按任意键打开下载页面  ...
    pause >nul
    start "" "https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0"
    exit /b 1
)
echo.

rem ===== [2/2] 启动 / Launch =====
echo [2/2] Starting the game / 正在启动游戏  ...
title Survivalcraft API Logs / 生存战争插件版日志
dotnet Survivalcraft.dll
set "EXITCODE=%ERRORLEVEL%"
echo.
echo Game exited with code %EXITCODE%. Press any key to close this window.
echo 游戏已退出，退出码 %EXITCODE%。按任意键关闭本窗口。
pause >nul
exit /b %EXITCODE%
