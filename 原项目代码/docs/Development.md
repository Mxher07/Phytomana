# Survivalcraft API 开发文档

## 开始

1. 如果未安装 .NET 10 SDK，请到 [官网](https://dotnet.microsoft.com/download/dotnet/10.0) 下载安装
2. 如果此仓库不在本地，请使用 Git 克隆此仓库

    ```bat
    git clone https://gitee.com/SC-SPM/SurvivalcraftApi.git
    ```

   > 还没有 Git？[官网下载](https://git-scm.com/downloads)

3. 进入此仓库，使用 [Visual Studio](https://visualstudio.microsoft.com/) 或 [Rider](https://www.jetbrains.com/zh-cn/rider/) 打开 `SurvivalcraftApi` 目录中的 `SurvivalCraftApi.sln`
4. 如果只是在 Windows 系统上进行调试，请在 Windows 以外的文件夹上右键，点击 `卸载项目`，之后在 Windows 文件夹中的 `Survivalcraft.Windows` 项目上右键，点击 `构建所选项目`，最后启动调试即可

## 构建游戏本体

**Windows、Linux**：  
直接构建相应的 `Survivalcraft.Windows`、`Survivalcraft.Linux` 项目即可

**Android**：  
要生成 Android 系统上的 `APK` 安装包，请在 `Survivalcraft.Android` 项目上右键，点击 `加载项目`，再点击`归档以用于发布`，之后按提示操作

> 过程中，如果报错未安装相应功能，请按提示完成安装，以下其他平台同理

**网页版**：  
直接构建相应的 `Survivalcraft.Browser` 项目即可  
如果要生成最终用于发布的网页版，请在解决方案根目录运行 `.\scripts\PublishSurvivalcraftBrowser.bat`

**iOS**：  
另见 [BuildIOS.md](BuildIOS.md)

**nupkg 引用包**：  
在解决方案根目录运行 `.\scripts\PackNugetPackages.bat`

## 模组开发者引用

1. 首先复制本存储库根目录的 `nuget.config` 文件到您的解决方案目录（和 `.sln` 文件同一层级）

2. 有两种常规方式添加引用包 (nupkg)，请选择您喜欢的方式

    * **推荐：** 在解决方案目录运行以下命令：

    ```bat
    dotnet add package SurvivalcraftAPI.Survivalcraft
    ```

    * 或者手动在`.csproj`文件的`<Project>...</Project>`中添加以下行（下面的版本号可能不是最新的）

    ```xml
    <ItemGroup>
      <PackageReference Include="SurvivalcraftAPI.Survivalcraft" Version="1.9.3.1"/>
    </ItemGroup>
    ```

3. **不推荐以上方法之外的引用方式**
4. 如果网络实在不通畅无法自动完成 nupkg 的下载，可从 [发布页](https://gitee.com/SC-SPM/SurvivalcraftApi/releases/latest) 下载前缀为`[Nupkgs]`，后缀为`.7z`的压缩包，将其中的所有`nupkg`文件解压到您喜欢的目录，之后按照 [微软官方教程](https://learn.microsoft.com/zh-cn/nuget/hosting-packages/local-feeds) 手动添加
5. 当然还有更麻烦的引用方式，按照上一步提到的方式或其他方式得到 nupkg 后，将其逐一解压，找到其中的`Engine.dll`、`EntitySystem.dll`、`Survivalcraft.dll`，将它们放到你喜欢的位置，在`.csproj`文件的`<Project>...</Project>`中添加以下行（大部分 IDE 支持在图形界面进行该操作，最终达成相同的效果就好）

```xml
<ItemGroup>
  <Reference Include="Engine" HintPath="（在此填写Engine.dll的文件路径，不要括号）" />
  <Reference Include="EntitySystem" HintPath="（EntitySystem.dll的文件路径，不要括号）" />
  <Reference Include="Survivalcraft" HintPath="（Survivalcraft.dll的文件路径，不要括号）" />
</ItemGroup>
```

## AI Agent 开发示例

```
请阅读此文档，帮我做一个在游戏中显示当前天气+距离晴天/下雨还有多久的生存战争插件版模组
https://gitee.com/SC-SPM/SurvivalcraftApi/raw/SCAPI1.9/docs/HowToDevelopAMod.md
```

> AI Agent： 例如 [OpenClaw](https://openclaw.ai/)、[Claude Code](https://claude.com/product/claude-code)、[OpenCode](https://opencode.ai/)、[CodeBuddy](https://www.codebuddy.cn/) 等

## 开发建议

### 对于插件版开发者
* 新增公开方法需考虑易用性
* 避免修改、删除已有公开方法，导致旧版模组不兼容
* 避免引入任何与原版不一致的特性
* 提交前至少构建并启动游戏进入存档，作为最基本的测试
* 修改 `Engine/` 目录下的代码时，注意跨平台兼容性，推荐使用 `#if` 条件编译处理平台差异
* 不要使用 IDE 的项目属性编辑功能，请手动编辑`.csproj` 文件和 `build/` 目录下的 `.props` 文件

### 对于模组开发者
* 优先考虑新增子系统、组件的形式来添加新功能
* 继承 `ModLoader` 类，使用钩子方法高效介入游戏逻辑
* 使用已内置的 `HarmonyX` 库来注入游戏方法
* 避免覆盖已有类，导致模组不兼容
* 如果有条件，建议使用 IDE 调试运行游戏，开启捕获任何异常，这有助于发现模组中存在但不会出现在日志中的错误
* 如有精力，建议提供国际化字符串（母语 + 英语）

> 接下来建议阅读架构文档：[docs/Architecture.md](Architecture.md)  
> 推荐模组开发者使用示例模组项目开始新模组的开发：[SC-SPM/SurvivalcraftTemplateModForAPI](https://gitee.com/SC-SPM/SurvivalcraftTemplateModForAPI)

## 本仓库代码风格

### 格式化（源自 .editorconfig）

* **缩进**：4 个空格，不使用 Tab
* **换行符**：CRLF
* **大括号**：同行放置 (K&R 风格)
* **最大行宽**：150 个字符
* **截断分行**：数组元素、参数数量超过 6 个时将它们截断并分行
* **编码**：UTF-8

### 命名规范

| 元素 | 规范 | 示例 |
| --- | --- | --- |
| 属性/公有静态字段/常量 | PascalCase (大驼峰) | `float LastFrameTime { get; set; }` |
| 实例字段 | `m_` 前缀 + camelCase (小驼峰) | `float m_frameBeginTime` |
| 方法 | PascalCase (大驼峰) | `public void Update()` |
| 参数/局部变量 | camelCase (小驼峰) | `float deltaTime` |

### C# 偏好设置

* **避免使用 `var`**：请使用显式类型，例如 `string path = ...` 而非 `var path = ...`
* **禁用可空类型 (Nullable)**：项目使用 `<Nullable>disable</Nullable>`
* **允许不安全代码**：`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`
* **语言版本**：preview (允许使用 C# 最新特性)

## 错误处理

* 使用 `Log.Error()` 和 `Log.Information()` 进行日志记录
* Mod 中的异常应当被捕获并记录，而不应导致游戏崩溃
* 在文件操作和外部代码调用处使用 `try/catch`

```csharp
try {
    // 操作
}
catch (Exception e) {
    Log.Error($"Failed to load: {e}");
}
```