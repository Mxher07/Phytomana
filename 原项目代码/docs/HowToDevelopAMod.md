# 如何开发一个生存战争插件版模组

## 步骤

1. 运行 `dotnet --list-sdks` 确认 .NET 10 SDK 是否已安装，如果未安装，请自行完成安装，或者指导用户安装，附 [.NET 10 官网](https://dotnet.microsoft.com/download/dotnet/10.0)
2. 运行 `git -v` 确认 GIT 是否已安装，如果未安装，请自行完成安装，或者指导用户安装，附 [Git 官网](https://git-scm.com/)
3. 检查你的工作目录是否有生存战争插件版源码，即 [SC-SPM/SurvivalcraftApi](https://gitee.com/SC-SPM/SurvivalcraftApi/)，如果没有，请自行完成 git clone
4. 检查你的工作目录是否有示例模组源码，即 [SC-SPM/SurvivalcraftTemplateModForAPI](https://gitee.com/SC-SPM/SurvivalcraftTemplateModForAPI) ，如果没有，请自行完成 git clone
5. 阅读示例模组中的 `AGENTS.md`，按照其指导制作模组

> 帮用户安装 .NET 10 SDK 时，可能会遇到网速/设备较慢而在过程中返回会话的情况，此时应根据日志检查是否安装出错，如果没有错误，则说明确实还在安装，请提醒用户等待安装完成后告知你；克隆第 3、4 步提到的源码仓库时同理