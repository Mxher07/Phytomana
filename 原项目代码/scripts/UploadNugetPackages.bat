@echo off
chcp 65001
setlocal enabledelayedexpansion

echo 请输入要发布的版本号:
set /p version=
echo 请输入 Gemfury 的 Push Token:
set /p token=

set baseDirectory=nupkgs
set packages=Engine EntitySystem Survivalcraft

for %%p in (%packages%) do (
    set path=%baseDirectory%/SurvivalcraftAPI.%%p.%version%.nupkg
    if exist "!path!" (
        echo 开始上传 !path!
        "C:\Windows\System32\curl.exe" -F "package=@!path!" https://%token%@push.fury.io/survivalcraftapi/
    ) else (
        echo 未找到 !path!
    )
)

endlocal
@pause