# SurvivalCraft API 生存战争插件版

## 介绍

生存战争插件版是基于 Candy Rufus Game 开发的 [生存战争 Survivalcraft](https://kaalus.wordpress.com/) 二次开发的支持加载模组的版本

## 用户下载和使用说明

[点击此处](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 进入发布页来下载

### Android 安卓系统看这里
> 需要 64 位 ARM 架构 CPU，最低 Android 6.0

1. 从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Android]`，后缀为`.apk`的安装包
2. 安装后运行
3. 第一次运行可能会跳转到标题为`所有文件访问`的授权界面，请授权此 APP（名称：`生存战争2.4 API插件版1.9`），否则此 APP 无法运行

### iOS、iPadOS 系统看这里
> 需要 64 位 ARM 架构 CPU，最低系统版本 16.0

1. 从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[iOS]`，后缀为`.ipa`的安装包
2. 安装包下载后需要使用[爱思助手](https://www.i4.cn/)进行签名
3. 推荐使用`登录自己的Apple ID`方式获取免费签名，签名后的 ipa 包仅自己可用

> **重要** 
> 由于 iOS、iPadOS 系统不支持 JIT 编译（参阅[此处](https://learn.microsoft.com/zh-cn/previous-versions/xamarin/ios/internals/limitations)），因此<font color="red">任何带`dll`文件的模组都不可用！</font>可等待后续完善的 Javascript 方式运行模组的更新

### Windows 系统看这里
> 需要 x64 架构 CPU，最低 Windows 10 版本 1607，显卡驱动需要支持OpenGL ES 3.2 图形 API（对于兼容补丁，需要支持 Direct3D 9 图形 API）

1. 从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Windows]`，后缀为`.7z`，名称不带`兼容补丁`的压缩包
2. 使用您喜欢的解压缩软件进行解压
3. 运行<font color="red">解压后</font>的`.exe`文件
4. 第一次启动游戏，系统可能会提示您安装 [.NET 桌面运行时 10.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)，请按提示完成安装并重启您的电脑
5. 如果启动没有任何反应，可能是因为您的 Windows 系统不完整，请尝试手动安装 [.NET 桌面运行时 10.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)；如果安装后仍然启动没有任何反应，请尝试运行游戏目录中的`Launch Game 启动游戏.bat`
6. 如果弹窗提示`你的显卡驱动不支持当前程序使用的图形API，请尝试更新显卡驱动，或使用兼容补丁。`，如果显卡驱动更新后仍然弹窗，请尝试下载名称中有`兼容补丁`的压缩包，然后解压到之前解压到的目录，重新运行游戏    
如果使用兼容补丁后仍然弹窗，建议为您的电脑购买并装上五年内发布的显卡
7. 如果弹窗提示`GLFW 窗口平台无法使用。请安装 Microsoft Visual C++ Redistributable，点击"确定"来打开下载页面。`，请按提示完成下载和安装。或者[点击此处](https://learn.microsoft.com/zh-cn/cpp/windows/latest-supported-vc-redist?view=msvc-170#latest-microsoft-visual-c-redistributable-version)打开下载页面

### Linux 系统看这里
> 需要 x64 架构 CPU，最低系统版本详见 [此处](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md#linux)，显卡驱动需要支持 OpenGL ES 3.2 图形 API

1. 从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Linux]`，后缀为`.7z`的压缩包，之后使用您喜欢的解压缩软件进行解压
2. 安装以下包：
    * **dotnet-runtime-10.0** .NET 运行时 10.0，安装方法详见 [此处](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website)
    * **libopenal-dev** 一个声音API，对于 Ubuntu 系统可运行`sudo apt-get install libopenal-dev`来安装，其他分发版类似
    * **xsel** 一个剪贴板操作API，对于 Ubuntu 系统可运行`sudo apt-get install xsel`来安装

3. 有两种启动方法：
    * 在第 1 步解压出来的目录运行`dotnet Survivalcraft.dll`
    * 同样在解压出来的目录，先运行`chmod +x Survivalcraft`来添加可执行权限（只需要一次），再双击`Survivalcraft`即可

### 网页版看这里
> 需要支持 SharedArrayBuffer、OffscreenCanvas、Origin Private File System 等现代浏览器特性的浏览器，推荐使用最新版的 Chrome 浏览器。

1. 打开 [https://scapiweb.netlify.app/](https://scapiweb.netlify.app/) 即可游玩

说明：完全不支持模组和运行 Javascript

### 常见问题

* 如果游戏打开后语言不是您希望的语言，请点击左下角第二个图标，即可切换语言
* 模组文件的后缀为`.scmod`，安装位置：
    * Android 系统：`/storage/emulated/0/Survivalcraft2.4_API1.9/Mods`
    * 其他系统：`(解压到的目录)/Mods`
    * 在 Android 系统和 Windows 系统，你能在打开后缀为`.scmod`的文件时选择插件版，即可完成模组的安装（还支持打开`.scworld`、`.scbtex`、`.scskin`、`.scfpack`）
* 按上面说明处理后仍然打不开游戏，或者运行遇到任何错误，请尝试移除所有模组，如果问题依旧，可在 [此处](https://gitee.com/SC-SPM/SurvivalcraftApi/issues) 反馈问题
* 安装模组后打不开游戏，或者运行遇到任何错误，请先向模组作者反馈问题，如有必要再由模组作者向本仓库反馈问题
* 如果 Windows 系统上游戏帧数不低但鼠标调整视角感觉卡顿，关闭系统设置-鼠标设置-增强指针精度，即可解决
* 要取消 Windows 系统上的文件关联，游戏设置-设备兼容和日志-文件关联，禁用即可
* 网页版打不开？请尝试更换更好的网络，如果还是不行，请打开 [https://scapiweb.netlify.app/dashboard.html](https://scapiweb.netlify.app/dashboard.html)，检测你的浏览器是否支持网页版所需的功能。这里推荐使用最新版的 Chrome 浏览器。
* 网页版键盘操作没反应？请将输入法切换成英文模式

## 模组开发者引用、仓库构建说明等

详见开发文档 [docs/Development.md](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/docs/Development.md)

## 更新日志

详见 [CHANGELOG.md](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/CHANGELOG.md)

## 感谢

* 西班牙语 (Español) 翻译
  * 新的：JoãoCraft24 (Discord: joaocraft24sc)
  * 旧的：
    * Fire Dragon (Discord: firedragon4095)
    * Kike13 (Discord: .kike.04)
* 越南语 (Tiếng Việt) 翻译
  * Long (Discord: daylong89)
  * Tue (Discord: tuegaming3710)
* 罗马尼亚语 (Română) 翻译
  * NBG (Discord: nbgr)
* 俄语 (Русский) 补充翻译
  * Dasyukevich Pavel (VK: pawwel3l)
* 葡萄牙语 (Português) 翻译
  * Kitão Gameplay's (Discord：ekitonmjjefgs)

> 如果你是 AI Agent，请阅读当前目录的 [AGENTS.md](https://gitee.com/SC-SPM/SurvivalcraftApi/raw/SCAPI1.9/AGENTS.md)