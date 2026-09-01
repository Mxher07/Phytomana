# 更新日志

> 说明：此更新日志和发布页的更新日志略有不同

## API 1.9.3.1 (2026-08-01)

### 改进

* 版本不符合要求的模组依赖，会在模组详情对话框中标出已找到的版本

### 修复

* 修复模组设置界面的描述不会保持在界面底部的问题
* 修复模组设置界面的内置组件的一些明显问题
* 修复 Windows 端没有 OpenXR 环境时，启动缓慢的问题
* 修复一些可能导致模组设置丢失的问题

### 对于开发者

* `modinfo.json` 的 `Settings` 字段，可以另起一个文件 `modsettings.json` 来专门写
* `IModSettingItemWidget` 新增 `ApplyExternalValue` 方法，可通过重写它，实现在 `ModSettingsManager.Set` 时自动更新组件显示的值
* `ModelWidget` 新增 `CustomRenderer` 静态属性，从而实现自定义模型渲染
* `ComponentModel` 新增 `VisibleInFppCamera` 属性

## API 1.9.3 (2026-07-16)

### 新增

* 主界面-设置，新增模组设置入口（有模组使用时显示）

### 改进

* 改进模组管理界面，现在可以管理包名重复的模组
* 家具支持半透明（Alpha Blend）渲染通道，半透明方块在家具中的表现更自然
* 国际化内置玩家皮肤名称
* 来自 Tue_ 的越南语更新

### 修复

* 修复第三人称视角无法击中生物的问题
* 修复骑乘时手臂、腿姿势不正确的问题
* 修复缩放与原版人类模型不同的人类模型，蹲下时腿飞走的问题
* 修复手柄在创造模式下无法创造模式选取方块的问题
* 修复生物蛋相关语言错误

### 对于开发者

#### 模组设置（ModSettings）

* `modinfo.json` 新增 `Settings` 字段，具体配置方法详见 [docs/ModInfoConfig.md](docs/ModInfoConfig.md)
* `ModSettingsManager` 新增 `Get`、`TryGet` 方法来获取通过上述方法配置的设置
* `ModLoader` 新增 `OnSettingChanged` 虚方法，设置项变更时回调

#### 模型动画系统

* ⚠️ **不兼容变更**：
  * 删除 `StateTrack` 子系统（无用）
  * `StateTrackConfig` 重命名为 `StateLayerConfig`（旧名称误导）
  * 动画名改为精确匹配（删除 `Contains` 模糊查找）
* `template.json` 模板配置文件更新
  * `boneMask` 支持子树展开，并新增 `boneMaskExclude` 字段
  * `layer` 新增 `weight` 字段
* `json` 配置文件更新
  * 新增 `boneAliases` 字段，可用于把 glTF 模型的骨骼名映射到原版生物骨骼名
  * `rules` 支持嵌套 `rules` 决策树，可表达更复杂的状态判定逻辑
  * `animation` 新增根骨骼平移、旋转（`rootBoneTranslation`、`rootBoneRotation`）字段
  * `animation` 新增 `onInterrupt` 事件，被打断时触发
  * `animation` 的 `source` 字段支持 `[param]` 动态选取要播放的动画
  * `animation` 字段支持字符串直接写动画别名（`"animation": { "source": "xxx"}` -> `"animation": "xxx"`）
  * `rootMotion` 的 `translation` 新增 `ImpulseSpeedOverride` 字段，支持覆写冲量速度
  * 修复 `animation` 的 `source` 使用别名时，`speed` 等字段不生效的问题
* 新增组件 `ComponentAnimationParticipant` 动画参与者机制，便于多方协作驱动模型动画
* 修复了非蒙皮骨骼错位、停用层动画滞留、切回 driver 时 `Weight` 残留致动画不显示等问题
* Root Motion 剥离逻辑下沉到 *per-source 采样点*，新增 `RootStripInfo` / `RootMotionStrip` 剥离原语；Root Motion 的物理副作用改为**数据驱动**
* `ModelWidget` 支持 `AnimationController` / `AnimationPlayer` 驱动骨骼动画
* 单帧姿势动画（即 `StartPhase == EndPhase`）取该相位帧即停，使 `onComplete` 正常触发
* `ComponentModel` 新增 `DisableAnimation` 属性，用于禁止该组件的模型动画

#### ComponentMiner 全动作延迟支持

* 新增 `PendingAction` 枚举：`Attack` / `Place` / `Use` / `Interact` / `Aim`，其中 `Aim` 是指瞄准后的发射，而不是瞄准这一行为本身
* 通过 `RequiresPending` / `SetRequiresPending` / `ClearRequiresPending` 配置哪些动作走 Pending（延迟执行）
* 通过 `IsPending` / `AnyIsPending` / `ClearIsPending` / `ClearAllIsPending` 查询与清除运行时状态，AnyIsPending 期间无法执行其他动作
* 新增 `ExecuteHit` / `ExecutePlace` / `ExecuteUse` / `ExecuteInteract` / `ExecuteAim` 方法，将在执行后清除对应的 `IsPending`

#### 人形模型相关

* `ComponentHumanModel` 第三人称手持物品的基础偏移/旋转参数化（`BaseInhandItemOffset`、`BaseInhandItemRotation`），以支持自定义模型适配
* `ComponentModel` 新增 `DisableDrawing` 属性；衣物使用的人类模型允许从数据库读取 `ModelNameForClothing`
* `ComponentClothing` 新增 `ShowClothedTexture`、`DrawClothedTexture` 字段
* `CharacterSkinsManager` 新增 `AddEmptySkin` 字段，模组可通过设置该字段为 true，让玩家能选择留空的皮肤，从而让模型内置纹理得以被使用；还新增了 `UseEmptySkinAsDefault` 字段，默认即使用前述留空的皮肤

#### 模组框架

* 改进单模组多 ModLoader 的支持
* `ModEntity` 的相等判断改为指针是否相同

#### 其他

* ⚠️ **不兼容变更**：删除 `IsFurnitureAlphaTest`，改为重写 `GetFurnitureSubsetType(int value)` 返回 `GeometrySubsetType`（如 `Opaque`、`AlphaTest`），配合新的家具半透明渲染通道。注意 `IsFurnitureAlphaTest` 是 1.9.2.1 刚引入的，若你已重写它，请迁移到 `GetFurnitureSubsetType`
* `Block` 新增 `DefaultIsUseable`，由 `SubsystemBlockBehaviors` 在加载时设置，如需手动设置，需要在它之后设置
* 方块网格（`BlockMesh`）支持 glTF 蒙皮顶点
* 修复 `ModelMesh.IsVisible` 在实例化/蒙皮模型上不生效的问题
* 修复 `ModLoader.OnAnimateModel` 钩子中 `Animated` 标志没有生效的问题

## API 1.9.2.1 (2026-06-13)

### 修复

* 修复 VR 控制器映射设置在启动时被重置的问题
* 修复鸟类进食时不低头的问题
* 安卓端重启应用只会退出，而不重启的问题
* 来自 Fire Dragon 的西班牙语修正

### 对于开发者

* Block 新增多个属性：
  * **GetDefaultTexture**: 获取默认使用的纹理。之前，你需要重写 GenerateTerrainVertices、DrawBlock 来实现使用外置纹理，现在只需要重写这个方法，返回要使用的外置纹理即可
  * **IsFurnitureAlphaTest**: 家具中此方块的体素是否使用 alpha-test 渲染（透明镂空）
  * **IsFurnitureEmissive**: 家具中此方块是否为发光体素
* 恢复 Game.VrEye 来减少不兼容问题

## API 1.9.2 (2026-06-03)

### 新增

* 支持虚拟现实（VR）
  * 需要 OpenXR 运行环境，仅限 Windows、Android 平台
  * Windows 平台第一次运行 VR，需要在`设置-图形`中手动启用`虚拟现实 (VR)`
  * Android 平台会自动检测当前设备是否支持 OpenXR，并自动进入 VR 模式
  * 相比较原版游戏自带的 VR 模式，添加了以下新特性：
    * Windows 端支持多人游戏
    * `设置-图形` 新增 `VR 视角高度调整` 滑块，方便坐姿游玩，对于高于角色身高的玩家，则能降低高度来提高游玩舒适度
    * `设置-控制` 新增 `VR 移动控制方式` 选项，默认为 `连续`，可以切换为 `瞬移`：玩家在左手柄摇动摇杆/滑动触摸板时，会发出抛物线，松开即可传送到目标位置；向后摇/滑动则会使角色向后移动；非飞行模式时只能移动到可碰撞方块上面、梯子方块侧面；有持续 0.2 秒的黑屏，大幅避免眩晕
    * `设置-控制` 新增 `VR 视角控制方式` 选项，默认为 `连续`，可以切换为 `瞬转`：玩家右手柄左右摇动摇杆/滑动触摸板时，会立即旋转 90 度；有 0.2 秒的黑屏，大幅避免眩晕
    * `设置-控制` 新增 `VR 手柄按键映射` 界面入口，你可以在此自定义 VR 手柄按键的功能，还可以在进入游戏世界后的`游戏菜单-更多操作`中找到该界面的入口
    * `设置-用户界面` 新增 `VR 界面大小` 滑块和 `VR 界面在左手停靠` 选项
    * 改善手持物品显示效果

### 修复

* 修复蹲下能轻易透视的问题
* 修复能滚动部件滚动过于灵敏的问题

### 对于开发者

* 给 PlayerData 所有方法添加 virtual 修饰符
* 为支持 VR，`GameWidget`、`ComponentInput` 有较多调整，可能会导致兼容问题，欢迎反馈

## API 1.9.1.3 (2026-05-22)

### 修复

* 修复在一些设备上无法正常绘制生物模型的问题
* 修复生物不会播放 RareIdleSound 的问题
* 修复四足生物脚步声过于频繁的问题
* 修复 ComponentModel.ModelScale 无效的问题
* 来自 JoãoCraft24 的葡萄牙语修正

## API 1.9.1.2 (2026-05-20)

### 修复

* 修复多处模组兼容问题
* 修复一处导致手机端严重掉帧的问题
* 修复中文翻译中的“生病”为“恶心”

## API 1.9.1 (2026-05-17)

### 新增

* 几乎完整的 glTF 格式模型支持。  
几乎完整是因为游戏自带着色器限制，部分功能仅通过游戏本体无法展示，为此我们开发了一个配套模组 [ModelPbrShader](https://github.com/XiaofengdiZhu/ModelPbrShaderForSC)，推荐下载使用（需要设备支持 OpenGL ES 3.2）

> 因为新的动画系统（兼容原版生物模型硬编码动画与 glTF 模型动画），现在第一次加载原版生物也会比之前慢的多，会偶尔产生非常轻微的卡顿

### 改进

* 如果有模组因非手动原因禁用，会在游戏启动时弹出提示
* 模组详情对话框会显示哪些依赖模组缺失了
* 采用来自JoãoCraft24的新西班牙语翻译
* 删除旧中文语言包
* 改进一些翻译

### 修复

* 修复一些界面打不开的问题
* 修复窗口缩放计算错误导致 Linux 平台鼠标可能无法正常使用的问题
* 修复 Linux 平台原生文件对话框库加载失败的问题
* 修复一个可能导致玩家创建失败的问题
* 修复一个导致导出的存档无法被原版导入的问题
* 避免因移动日志文件失败而导致不将日志写入日志文件

### 对于开发者

* 对于新增的 glTF 格式模型支持，你可以参考 [SurvivalcraftTemplateModForAPI](https://github.com/XiaofengdiZhu/SurvivalcraftTemplateModForAPI) 和 [AdvancedGltfFoxMod](https://github.com/XiaofengdiZhu/AdvancedGltfFoxModForSC) 来添加模型、配置动画；通过前面两个示例模组项目来学习是最快的，你还可以选择阅读以下文档来学习：
  * [glTF 生物模组开发教程](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/docs/GltfCreatureModTutorial.md)
  * [动画配置 JSON 参考](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/docs/AnimationConfigReference.md)
  * [动画系统高级主题](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/docs/AnimationAdvancedTopics.md)
* 为方块添加可供重写的方法 `GetIsCollidable`，可判断身体组件与某特定方块是否可发生碰撞

> 本次更新几乎完全由 [Claude Code](https://claude.com/product/claude-code) + [智谱 GLM-5.1](https://docs.bigmodel.cn/cn/guide/models/text/glm-5.1) 完成

## API 1.9.0.2 (2026-04-03)

### 修复

* 触摸屏视角控制方式为准星时，点右下角能跳跃。注意：这是插件版特性，原版无此特性
* 修复网页版触屏经常失效的问题
* 修复网页版导出、分享存档失败的问题
* 修复破坏方块时的裂痕在水下不显示的bug
* 修复自定义截图宽度滑动条的范围错误
* 修复西班牙语翻译中的多处错误
* 修复文本框在一些操作中可能抛出异常的问题
* 修复键盘映射界面返回上一个界面不正确的问题
* 尝试修复生物模型绘制时使用的纹理不正确的问题

### 对于开发者

* 修复 ModEntity.Equals 中未判断 null 导致的一系列错误
* 修复一个可能导致 ModLoader 注册顺序错乱的问题
* 修复 OnVitalStatsUpdateXXX 系列钩子第二个参数不发生作用的问题，并完善其注释
* `Storage.ProcessPath` 现在处理本地路径不会抛出异常
* [地形系统技术文档](docs/TerrainSystem.md) 添加生物群系生成机制章节

## API 1.9 (2026-03-14)

### 新增

* 新增网页版，性能约为各系统专用版的一半，需要支持 SharedArrayBuffer、OffscreenCanvas、Origin Private File System 等现代浏览器特性的浏览器，例如最新的 Chrome。打开 [https://scapiweb.netlify.app/](https://scapiweb.netlify.app/) 即可游玩（感谢 Nome Criativo 的初步技术验证和过程帮助）
* 新增手动添加/选择社区服务器的功能，并更新中文社区默认网址
* “设备兼容和日志”界面新增“管理类替换”界面的入口，在这个新界面，你可以管理子系统、实体组件使用哪个模组中的版本，游戏启动时的弹窗也得到改进
* 游戏启动时传入 `-play <存档所在文件夹名称>` 参数，将在启动游戏后自动进入存档
* 模组管理界面新增手动调整模组加载顺序的功能
* 感谢 Kitão Gameplay's（Discord：ekitonmjjefgs）添加葡萄牙语

### 提示

* 最低安卓版本要求提升至 6.0

### 修复

* 完全修复 Android 系统鼠标相关问题
* 修复隐藏移动查看图标后不能跳跃的问题
* 修复一些方块下方是半砖时，底面不渲染的问题
* 修复模组包名不合法时仍然会加载等多个模组加载问题
* 修复游戏过程中启用展示加载日志后，进入存档时会出错的问题
* 修复一个可能导致文本输入框无法使用的问题
* Linux 系统可能的鼠标位置错误问题
* 修复一些界面显示问题

### 改进

* 新的基于 [ANGLE](https://github.com/google/angle) 的 Windows 系统专用兼容包，它通过更广泛支持的 Direct3D 图形接口来接管 OpenGL ES 图形接口，以实现在不支持 OpenGL ES 的显卡驱动上运行游戏；但只支持到 OpenGL ES 3.0
* Android 系统点击文本输入框后会立即弹出键盘
* 性能图示的绿线位置现在会根据屏幕刷新率调整
* 安全模式也能在“模组管理界面”管理模组了
* 导出文件后会显示其路径（之前只显示文件名）
* `modinfo.json` 损坏/不合法时会输出更友好的错误日志
* 不再允许导入文件名相同的模组
* 改善一些中文翻译、越南语翻译

### 对于开发者

**<center>欢迎来到 AI 模组开发时代！</center>**
**<center>推荐使用 HarmonyX 注入方法！</center>**

* 新增适合 AI Agent 从零开始开发模组的文档 [docs/HowToDevelopAMod.md](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/docs/HowToDevelopAMod.md)，你可以尝试向你的 AI Agent 这么说，就能快速得到一个可用的模组（可能存在少量问题）

```
请阅读此文档，帮我做一个在游戏中显示当前天气+距离晴天/下雨还有多久的生存战争插件版模组，最后把做好的 .scmod 文件发给我
https://gitee.com/SC-SPM/SurvivalcraftApi/raw/SCAPI1.9/docs/HowToDevelopAMod.md
```
> AI Agent： 例如 [OpenClaw](https://openclaw.ai/)、[Claude Code](https://claude.com/product/claude-code)、[OpenCode](https://opencode.ai/)、[CodeBuddy](https://www.codebuddy.cn/)

* 项目结构已重构，详见新增的 [架构文档](https://gitee.com/SC-SPM/SurvivalcraftApi/blob/SCAPI1.9/docs/Architecture.md)
* 新增多篇 AI 生成技术文档，详见 [docs](https://gitee.com/SC-SPM/SurvivalcraftApi/tree/SCAPI1.9/docs)
* 新增 [HarmonyX](https://github.com/BepInEx/HarmonyX) 包引用，通过它你可以非常方便地注入、修改、替换方法，使用方法详见此处：[HarmonyX Wiki](https://github.com/BepInEx/HarmonyX/wiki)
* 改进以下三个 xml 数据文件的合并方式（旧写法仍然兼容），同时新增一个专门用来移除继承来的组件的空组件 `ComponentNoEffect`

| 数据类型 | 游戏内文件 | 模组内文件 | 合并方式 |
|----------|------------|------------|----------|
| 衣物表 | `Clothes.xml` | `*.clo` | 按 Index 匹配，支持 `New-` 前缀修改多个属性、`Remove` 删除元素 |
| 合成表 | `CraftingRecipes.xml` | `*.cr` | 追加新合成配方，支持 `New-` 前缀修改单个属性、`Remove` 删除元素（需要其他每个属性都相同） |
| 数据库 | `Database.xml` | `*.xdb` | 追加新数据，按 Guid 匹配，支持 `New-` 前缀修改单个属性、`Remove` 删除元素 |

* `ModsManager.HookAction` 方法新增带有 `int priority` 参数的重载，越小优先级越高
* `ModLoader.OnMinerPlace` 接口方法新增带有 `BlockPlacementData placementData` 参数的重载
* `ModLoader` 类新增以下接口：`OnSwitchScreen`、`OnShowDialog`、`OnHideDialog`
* `Engine.Touch` 新增 `IsTouched` 字段
* `Engine.Window` 新增 `Scale` 字段，用以表示窗口实际渲染大小 / 系统返回的窗口大小
* `ListPanelWidget` 新增 `SwapItems` 方法
* 游戏启动时传入的参数将储存到 `Program` 类的 `Dictionary<string, string> StartupParameters` 字段中，例如：
```bat
Survivalcraft.exe -play World1 -yourCustomParameter 123
```
将设置 `Program.StartupParameters["play"]` 为 `"World1"`，`Program.StartupParameters["yourCustomParameter"]` 为 `"123"`  
对于 Android，等效的代码是
```kotlin
startActivity(Intent().apply {
    component = ComponentName("com.candy.survivalcraftAPI1_9", "com.candy.survivalcraftAPI1_9.crc64251ea0d6925f8f9e.MainActivity")
    putExtra("play", "World1")
    putExtra("yourCustomParameter", "123")
    addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
})
```
* `modinfo.json` 新增 `GameplayImpactLevel`（玩法影响等级）字段，用于标识模组对游戏平衡性的影响程度，会保存到存档中，默认为 `Cosmetic`，有以下选项：

| 可选值 | 中文名 | 示例 |
| :----: | :----: | :--- |
| `Cosmetic` | 纯装饰品 | 材质包、字体包、光影 |
| `Assist` | 轻度辅助 | 小地图（不透视）、箱子整理、显示生物血量、合适成本提升玩家能力 |
| `Turbo` | 强力辅助 | 一键撸树、自动化、矿物雷达、低成本提升玩家能力、合适成本的规则破坏 |
| `Break` | 规则破坏 | 无/低成本地大幅提升玩家能力、飞行、传送门、掉落物/产量倍增 |
| `Godmode` | 上帝模式 | 无敌、瞬移、无限资源 |

* 新增 `modinfo-scheme.json` 文件，能在 IDE 中验证 `modinfo.json` 的数据结构是否正确
* 支持在 `.xdb` 数据库中直接编写生物生成规则，具体且查看 `Game.StandardCreatureSpawnRule` 类，写法举例：

```xml
<ParameterSet Name="CreatureSpawnRules" Guid="98c19e0b-ff62-4acc-8e58-def30e6257a3">
  <ParameterSet Name="TemplateRuleWerewolf">
    <Parameter Name="Name" Value="Duck" Type="string" /><!-- 必填，你要生成的 EntityTemplate 的名称 -->
    <Parameter Name="Class" Value="Game.StandardCreatureSpawnRule" Type="string" /><!-- 必填，生成规则的类 -->
    <Parameter Name="SpawnLocationType" Value="Surface" Type="Game.SpawnLocationType" /><!-- 默认: Surface -->
    <Parameter Name="RandomSpawn" Value="True" Type="bool" /><!-- 默认: False -->
    <Parameter Name="ConstantSpawn" Value="false" Type="bool" /><!-- 默认: False -->
    <Parameter Name="MinTemperature" Value="5" Type="int" /><!-- 默认: 0 -->
    <Parameter Name="MaxTemperature" Value="15" Type="int" /><!-- 默认: 15 -->
    <Parameter Name="MinHumidity" Value="9" Type="int" /><!-- 默认: 0 -->
    <Parameter Name="MaxHumidity" Value="15" Type="int" /><!-- 默认: 15 -->
    <Parameter Name="AboveTopBlock" Value="True" Type="bool" /><!-- 默认: False -->
    <Parameter Name="MinShoreDistance" Value="40" Type="float" /><!-- 默认: -Infinity -->
    <Parameter Name="MaxShoreDistance" Value="Infinity" Type="float" /><!-- 默认: Infinity -->
    <Parameter Name="Blocks" Value="LeavesBlock;WaterBlock;GrassBlock;DirtBlock" Type="string" /><!-- 默认: （无） -->
    <Parameter Name="Suitability" Value="2.5" Type="float" /><!-- 默认: 1 -->
    <Parameter Name="Count" Value="1" Type="int" /><!-- 默认: 1 -->
  </ParameterSet>
</ParameterSet>
```

* 不再删除着色器中的 `highp`、`mediump` 、`lowp`
* 移除无人使用且不完善的 ModList 功能

## API 1.8.2.3 (2026-01-01)

### 新增
* 方块详情界面显示动态 ID 和特殊值（需进入存档）
* 创造模式的游戏菜单额外显示玩家重生位置坐标，以及所在位置的温度、湿度
* 支持 ASTC 格式的方块材质

### 修复
* 安卓端鼠标、手柄输入问题全面修复
* 低版本安卓可能闪退的问题
* 生物出生立即死亡后，尸体不消失的问题

### 对于开发者
* 新纹理类：CompressedTexture2D，可加载 ASTC 格式的压缩纹理。  
  优点：无需解码，直接传递给 GPU，压缩率和质量损失尚可。  
  官方工具：[https://github.com/ARM-software/astc-encoder](https://github.com/ARM-software/astc-encoder)  
  推荐命令：`.\astcenc-avx2.exe -cl 输入路径 输出路径 8x8 -exhaustive`
* 新增重启游戏的方法：Window.Restart

## API 1.8.2.2 (2025-12-24)

### 新增
* 全新中文翻译
* 适配安卓全面屏（设置-用户界面-适配全面屏，刘海屏默认开启）

### 修复
* 已禁用模组的资源仍然会加载的问题
* 蹲下仍然会知道跳跃的问题
* Android 端鼠标操作异常的问题
* Android 端编辑文字弹窗的标题和描述没翻译的问题
* 在高版本 Android 运行时，返回键会直接返回到桌面的问题
* Windows 端启用“增强指针精度”时，视角异常跳动的问题
* 一些翻译问题

### 对于开发者
* ModLoader 新增 OnVitalStatsEat，OnVitalStatsUpdateFood，OnVitalStatsUpdateStamina，OnVitalStatsUpdateSleep，OnVitalStatsUpdateTemperature，OnVitalStatsUpdateWetness，OnInventorySlotWidgetCalculateSplitCount 接口
* FastDebugMod 能加载非 Mods 根目录的各类资源文件了
* Widget 新增四个方向的 Margin
* ScreensManager 新增 HistoryStack、GoBack 等
* ComponentFirstPersonModel 新增 ForceDrawHandOnly
* CellFace 新增 FaceToTangents、GetFourVertices、GetSixVertices
* Terrain 等相关类新增大量参数类型为 Point3 的方法（以前只有int x, int y, int z 形式的）

## API 1.8.2.1 (2025-11-24)

### 新增
* 兼容设置中新增安全模式，启用后将停止加载所有模组

### 优化
* 全面优化 Mod 管理界面
* 将原版社区、中文社区按钮合并，并增加新中文社区网页版入口

### 修复
* Android 系统上鼠标能正常在游戏中使用了
* 攻击硬直累加计算导致霰弹超长硬直的问题
* 一个不应该的破坏性变更导致的旧模组不兼容（在插件版1.8.2，`ClothData.Texture` 从字段变成了属性，现已恢复为字段）

### 对于开发者
* 给`HumanReadableConverter`加上了 Nullable 支持
* `Mouse`新增`SetCursorType`方法（其中 Android 系统不生效，原因不明）
* 如前所述，`ClothData.Texture`已恢复为字段

## API 1.8.2 (2025-11-16)

### 新增
* Android 系统打开或分享`.scworld`、`.scmod`等格式文件时，如选择插件版，文件将自动导入到合适的位置，不再需要手动在文件管理器手动复制粘贴了；Windows 系统同理，第一次运行插件版后，将可以直接双击这些格式的文件来导入
* Android 系统上导出存档、家具包等文件后，能立即进行分享
* 再次支持 iOS 系统，安装注意事项详见[此处](https://gitee.com/SC-SPM/SurvivalcraftApi#iosipados-%E7%B3%BB%E7%BB%9F%E7%9C%8B%E8%BF%99%E9%87%8C)
* 创建世界时可编辑真种子（并在多个位置显示真种子），可选择地形生成器版本
* 扩大创建世界时多个选项的范围，包括：岛屿大小、生物群落大小、年天数、平坦地面方块、海平面高度
* 界面设置新增自定义截图分辨率
* 控制设置新增手柄相关设置，且可使用组合键；新增物品快捷栏循环滚动设置
* 视角设置会显示具体角度
* Mod 管理界面新增“查看主页”按钮
* 启动游戏失败会有更具体的提示
* 日志文件能直接从游戏内打开
* 插件版在 Android 系统退出后，会自动从多任务界面移除
* 给 Android 系统的自适应图标添加单色图标

### 修复
* 玩家皮肤显示错误的问题
* 背包空间不足，捡起过多的掉落物时，多余的掉落物会消失的问题
* 南瓜能种在不合适方块上的问题
* 蹲着不能跳的问题
* 创造模式使用桶时的行为与原版不一致的问题
* Windows 系统上文本框中光标与输入法位置不匹配、鼠标点击后光标位置错误、左右滚动过头的问题
* 使用手柄时，打开对话框会把实际鼠标也居中的问题
* Android 系统上第一次启动游戏，跳转到管理所有应用的所有文件访问权限的界面的问题（应该跳转到管理自身的所有文件访问权限的界面）
* 其他小问题

### 对于 mod 开发者
* .Net SDK 升级到 10.0（如果您开发的 mod 没有使用新版增加的东西，可以不用升级引用包和目标框架版本）
* `Engine.Storage`新增多个实用方法：`OpenFileWithExternalApplication`（使用其他应用打开文件）、`ShareFile`（分享文件到其他应用）、`ChooseFile`（使用系统文件选择器选择文件）
* `Engine.Display` 新增 `MaxTextureSize`
* `ClothingData` 新增 `UseLazyLoading`，用于设置纹理是否直接加载或者按需加载，当mod的高清纹理衣服过多时，可以采用按需加载，加快游戏启动速度
* 读取 json 时允许注释和尾随逗号
* 将优先读取 scmod 压缩包根目录的`icon.webp`作为 mod 图标（仍然支持`icon.png`）
* 建议积极设置`modinfo.json`中的 Link，因为玩家将能从 Mod 管理界面打开该链接

#### `modinfo.json`重大更新：

详见 [docs/ModInfoConfiguration.md](docs/ModInfoConfiguration.md) "Dependencies 依赖关系" 章节

## API 1.8.1.3 (2025-8-31)

这是一个修复了很多问题的小型更新

### 新增
* Windows端新增使用Direct3D11图形API的兼容补丁
* 从这个版本开始正式支持Linux系统
* 家具上限翻倍（4096→8192）
* 罗马尼亚语，感谢Discord用户NBG

### 优化
* 扩宽视角设置的范围
* 新增隐藏十字准星的设置
* JS运行对话框添加复制输出的按钮
* 同时只能一个线程保存设置，避免冲突
* 调整一些提示的文字，意思更清晰

### 修复
* 中文以外语言基本不可用的问题
* Android端应用在后台会自杀的问题
* Android端日志超2MB不创建新日志文件的问题
* 主界面更新按钮图标颜色错误的问题
* Windows端切换全屏/窗口会导致视角突变的问题
* ogg音频文件只会播放一半的问题
* 存档选择界面不显示世界数量和总大小的问题
* 南瓜灯不能放在一般方块上的问题
* 射弹反弹速度过大的问题
* 一个mod有多个mod图标导致modinfo加载失败的问题
* ContentManager无法直接读取JsonArray、JsonObject的问题
* Widget.IsUpdateEnabled没用的问题
* 部分界面内容溢出屏幕、内容框的问题

### 对于开发者
* ModLoader新增接口：ClothingProcessSlotItems、IsLevelSufficientForTool、OnAppendModelMeshPart、OnAppendModelMesh
* 新增TransformedShader类
* ModelWidget支持自定义着色器
* 更多图形相关的类中的private改为public，给很多方法加上virtual，部分类新增Tag
* 可以通过EnableDressLimit去除衣服穿戴等级限制
* TextBoxWidget新增属性EnterAsNewLine、MaxLinesCount
* 攻击新增参数ArmorProtectionDivision，射弹新增参数MinVelocityToAttack
* Mount增加allowToStartRange，ScoreMount返回负数时禁止骑乘，ComponentRider增加DetectSurroundingMountRange
* 增加使用旧版参数的DrawCubeBlock的重载方法
* 项目源码已大幅调整结构，解决警告，并统一代码格式

## API 1.8.1.2 (2025-7-01)

### 优化
* Configs文件加载、时间子系统加入错误处理机制
* 重构了熔炉组件
* 更完善的界面国际化；未找到相应语言的字符串时，将使用英语语言中的字符串
* 以同时显示标识牌数量为代价提升标识牌渲染效果


### 修复
* 解决电脑版导出存档不正确的问题
* 修复SubsystemTime.GameTimeFactor变化异常问题
* 修复重置按键可能报错的问题
* 修复mod缺字段时可能无法进入存档问题
* 修复绘制渐变矩形异常问题
* 修复有时加载旧存档时，方块ID不按已有旧档分配方式分配的问题
* 修复找不到图像会导致界面组件停止加载的问题
* 修复烟花、南瓜汤、颜料桶合成信息错误
* 修复方块高亮边框绘制在第一人称手臂之上的问题
* 修复射弹不能打水漂的问题
* 修复部分情况下玩家模型组件显示错误的问题


### 新增
* 添加界面：API更新界面
* 添加性能设置：材质刷新频率


### 面向Mod开发者
* 添加重新计算摄像机投影矩阵的接口
* 添加新的EditBlockDescriptionScreen接口，传入BlockValue
* SubsystemDrawing，SubsystemElectricity，SubsystemUpdate加入性能分析
* 增加接口ClothingSlotProcessCapacity
* RectangleWidget添加BlendState属性
* 数据库的Model组件模板新增Transparent，ModelScale属性
* 增加接口OnProjectileRaycastBody，射弹更新寻找下一个可以命中的Body时执行
* 增加接口IComponentEscapeBehavior，表示水路空三种生物的逃跑行为

## API 1.8.1.1 (2025-5-25)

### 优化和修复
* 日志文件被占用时会显示原因
* 修复Windows端按键盘F11切换全屏为窗口时，窗口大小变为0的bug


### 面向Mod开发者的更新
* PlayerData等待地形的最大时间可修改
* 统一Windows端和Android端ModsManager内ExternalPath等多个成员为属性

## API 1.8.1 (2025-5-20)

### 优化
* 部分安卓设备游戏帧率提高4倍，大幅提升流畅度
* D板编辑后数量不变，即不会变为1
* 主界面布局调整
* 英文和中文排在语言列表前面
* 安卓版启动即横屏
* 伸长活塞能正常绘制
* 部分翻译补充和调整
* 解除掉落物最大可视距离仅为30的限制
* 日志显示界面加上省略号避免过长时卡死，调小网络连接类报错占的篇幅

### 修复
* 对地面使用家具锤可能导致死循环的问题
* 进入损坏存档再进入正常存档会多一个假玩家的问题
* 家具方块电路无法连接的问题
* 部分模型界面显示问题
* 部分区块植物不生长的问题
* 温度条显示错误问题
* 合成台在放入物品后会吞掉副产品的问题
* 文本框跨行显示与光标定位问题
* 电脑按F11全屏经常被恢复的问题
* 星星绘制错误的问题
* 运行时相关文件被其他程序锁死导致游戏无法正常运行

### 新增
* 新增语系（越南语、西班牙语）
* 更新对话框添加“前往发布页”按钮
* 进入缺失模组的世界时弹出警示的功能
* 电脑版键位设置
* 游戏摄像机列表设置
* 删除世界需要输入确认（设置里，默认关闭）
* 手动更新功能

> 以下更新日志来自[把红色赋予黑海的仓库](https://gitee.com/THPRC/survivalcraft-api/)

## API 1.80 (2025-1-16)

跟进原版2.4并修复大量bug

## API 1.72 (2024-11-06)

MOD接口方面（具体用法详见md文件）
### ModInfo结构
模组增加LoadOrder字段

建议：
主题模组的LoadOrder在-100000~-10000的范围，辅助模组的LoadOrder在10000~100000的范围

示例：
```json
{
  "Name": "Gigavolt 十亿伏特",
  "Version": "1.0",
  "ApiVersion": "1.72",
  "Description": "这是一个为生存战争游戏带来十亿伏特电力系统的mod，将原版的16个电压级别（0~1.5V）扩展到2^32个（0~2^32-1V）",
  "ScVersion": "2.3.0.0",
  "Link": "https://github.com/XiaofengdiZhu/Gigavolt",
  "Author": "销锋镝铸",
  "PackageName": "xfdz.Gigavolt",
  "Dependencies": [],
  "LoadOrder": 10000
}
```

### 动态方块ID

为提高模组之间的兼容性，减少方块ID冲突，API1.72采用 **动态方块ID**，方块Index在300及以上的方块会被采用动态方块ID分配，方块定义的Index不再是正确的方块ID。

使用方块的正确Index需要采用
* `BlocksManager.GetBlockIndex(string BlockName)`
* `BlocksManager.GetBlock(string BlockName)`
* `block.BlockIndex`

Block.Index由常量改为静态变量，会随着动态方块ID的分配而改变。建议模组将```Block.Index```修饰类型由const改为static，这样可以通过```Block.Index```来引用方块

SubsystemBlockBehavior不建议使用HandledBlocks，而是在方块的csv里面定义该行为掌控的方块

```csharp
public class xxxBlock : Block
{
    /*public const int Index = 400;*///以前的版本，固定ID
    public static int Index = 400;//改为static变量，使用可变方块ID
}
public class SubsystemXXXBlockBehavior : SubsystemBlockBehavior
{
      //仍然建议使用csv来编写HandledBlocks
      public override int[] HandledBlocks => new int[4]
      {
          BlocksManager.GetBlockIndex<EmptyBucketBlock>(),
          BlocksManager.GetBlockIndex<WaterBucketBlock>(),
          BlocksManager.GetBlockIndex<MagmaBucketBlock>(),
          BlocksManager.GetBlockIndex<MilkBucketBlock>()
      };
}
```

对于升级到1.72的mod，重点排查`Terrain.MakeBlockValue()`的调用，将这些调用均改为动态ID调用

### 常用组件调整
为了保证模组之间的兼容性，不同模组共同发展。API1.72对组件覆盖的行为进行了限制。  
当有模组覆盖了常用组件，或者不同的模组对同一个Guid进行了new-覆盖操作时，会在加载屏幕进行报错。玩家将会选择是否禁用覆盖操作。因此不再建议模组作者对数据库**尤其是常用组件**进行覆盖

覆盖警告的常用组件包含以下组件和guid：
```cs
ModifiedElement["7347a83f-2d46-4fdf-bce2-52677de0b568"] = "Game.ComponentBody";
ModifiedElement["4e14ce27-fdef-46ca-8ea0-26af43c215e5"] = "Game.ComponentHealth";
ModifiedElement["7ecfafc4-4603-424c-87dd-1df59e7ef413"] = "Game.ComponentPlayer";
ModifiedElement["9dc356e5-7dc8-45f6-8779-827ddee9966c"] = "Game.ComponentMiner";
ModifiedElement["6f538db3-f1fe-4e91-8ef5-627c0b1a74ba"] = "Game.ComponentRunAwayBehavior";
ModifiedElement["8b3d07dc-6498-4691-9686-cf4edabb8f3f"] = "Game.ComponentGui";
ModifiedElement["e2636c38-f179-4aa1-b087-ed6920d66e8e"] = "Game.SubsystemTerrain";
ModifiedElement["1c95cd40-26be-44cf-938a-157b318ff086"] = "Game.SubsystemPlantBlockBehavior";
ModifiedElement["937999c9-9570-4cbd-8390-23f1e4609cdd"] = "Game.SubsystemPistonBlockBehavior";
ModifiedElement["96e79f99-a082-4190-9ab6-835dc49ebbdd"] = "Game.SubsystemExplosions";
ModifiedElement["dafb8e14-11b9-44b7-a208-424b770aeaa9"] = "Game.SubsystemProjectiles";
ModifiedElement["32d392de-69c1-4d04-9e0b-5c7463201892"] = "Game.SubsystemPickables";
ModifiedElement["54a4f6d5-98dd-4dc3-bf6d-04dfd972c6b7"] = "Game.SubsystemTime";
```

#### ComponentPlayer
当Interact生效（玩家点击屏幕，或鼠标右键）时，逻辑会进行改变。首先通过方块的新增属性`GetPriorityUse, GetPriorityInteract, GetPriorityPlace`和`ModLoader.OnPlayerInputInteract`来确定<使用手中方块><交互面前方块><放置手中方块>这三个行为的优先级。之后根据三者优先级，选择优先级最高且大于0的行为执行。  
例如：口哨方块放置优先级为0，箱子方块交互优先级为2000，口哨方块使用优先级为3000。那么手持口哨点击箱子时，由于口哨方块使用优先级最高，则会执行“使用口哨”，即吹响口哨。

对玩家的输入行为增加了大量的ModLoader接口，详见下方的ModLoader接口介绍。

#### ComponentLevel
增加了一个ComponentFactors，用于控制生物的力量乘数、防御乘数、移速乘数。而ComponentLevel被改为只和经验值相关的组件。  
所有非玩家生物都拥有了ComponentFactors，玩家的ComponentLevel继承ComponentFactors。

ComponentLocomotion, ComponentMiner, ComponentHealth中引用到的力量、防御、移速乘数被改为引用ComponentFactors。

#### ComponentFactors
新增OnFactorsUpdate接口: 用于更新非玩家生物的力量、防御、移速倍率。

#### SubsystemProjectiles
Update进行了重大调整。射弹的更新逻辑被放到Projectile.Update()中执行。  
这意味着你可以在`SubsystemProjectiles.m_projectiles`容纳各种各样的Projectile。它们有不同的更新行为。  
新增了OnProjectileAdded、SaveProjectile、OnProjectileUpdate接口，用于控制弹射物的行动规律

#### Projectile
增加了以下字段和方法：
```csharp
public bool IsFireProof = false;//该弹射物和掉落物防火，不会被火焰或熔岩烧毁

public float? MaxTimeExist;

public float ExplosionMass = 20f;//处于爆炸中时，进行爆炸计算的质量

public GameEntitySystem.Entity OwnerEntity; //以Entity的形式指明弹射物来源（比如发射器）

public float Damping = -1f;//射弹的速度衰减

public float DampingInFluid = 0.001f;

public float Gravity = 10f;

public float TerrainKnockBack = 0.3f;

public bool StopTrailParticleInFluid = true;

public int DamageToPickable = 1;//弹射物结算时掉的耐久

public bool TerrainCollidable = true;

public bool BodyCollidable = true;

public List<ComponentBody> BodiesToIgnore = new List<ComponentBody>();//弹射物飞行的时候会忽略List中的ComponentBody

public virtual void Update(float dt) //执行弹射物的更新操作

public virtual void OnProjectileFlyOutOfLoadedChunks() //弹射物飞离加载区块时执行的操作

public override void UnderExplosion(Vector3 impulse, float damage)//弹射物受到爆炸时执行的操作
```

#### SubsystemProjectiles
增加了以下ModLoader接口
* OnProjectileHitBody: 弹射物在命中生物时执行。可以控制攻击力、命中后改变弹射物的移速。还可以设置ignoreBody = true来跳过弹射物判定当前Body。模组可以在此接口中增加其他行为。
* OnProjectileHitTerrain: 弹射物在命中方块时执行。可以控制是否摧毁方块，弹射物命中后移速等行为。模组可以在此接口中增加其他行为。
* SavePickable: 存储掉落物

#### SubsystemPickables
* 掉落物系统进行了重大调整。掉落物的更新逻辑被放到Pickable.Update()中执行。  
  这意味着你可以在`SubsystemPickables.m_pickables`容纳各种各样的Pickable。它们有不同的更新行为。
* 掉落物被玩家吸取的逻辑被转移到ComponentPickableGatherer中，拥有此组件并进行合理配置的实体（如玩家）可以收集掉落物。  
  如方块在吸取时并不是直接进入物品栏，而是进行其他操作（如经验球），特殊操作在SubsystemBlockBehavior.OnPickableGathered()中执行。
* 模组作者建议参考ComponentPickableGatherer, ComponentPickableGathererPlayer, SubsystemExperienceBlockBehavior仿照经验球来定义掉落物在拾起的特殊行为。
* 增加了以下ModLoader接口
    * OnPickableAdded：掉落物在创建世界时，或者生成时执行。可以在此时更改掉落物的属性，甚至类别。
    * OnPickableDraw: 控制掉落物的绘制
    * SavePickable
    * OnPickableUpdate

#### Pickable
增加了以下字段和方法：
```cs
    public bool IsFireProof = false;//该弹射物和掉落物防火，不会被火焰或熔岩烧毁
    
    public float? MaxTimeExist;
    
    public float ExplosionMass = 20f;//处于爆炸中时，进行爆炸计算的质量
    public virtual double TimeWaitToAutoPick => m_timeWaitToAutoPick;//掉落物只有在存在时长超过这个时间（初始值为0.5秒）后才能被捡起。
    public virtual float DistanceToPick => m_distanceToPick;//掉落物在距离目标这个范围（初始值为1格）内时，会立即进入玩家背包。
    public virtual float DistanceToFlyToTarget => m_distanceToFlyToTarget;//掉落物在距离目标这个单位（初始值为1.75格）内时，会飞向玩家。
    
    public override void UnderExplosion(Vector3 impulse, float damage)//处于爆炸状态下执行的操作
```

#### SubsystemBodies
增加BodyCountInRaycast接口：可以决断是否将一个ComponentBody纳入Raycast结果当中


#### SubsystemTime
新增ChangeGameTimeDelta接口，用于控制每帧的时间

#### SubsystemUpdate
增加OnIUpdateableAddOrRemove接口，用于控制更新队列

#### SubsystemDrawing
增加OnIDrawableAdded接口，用于控制绘制更新队列

#### Entity
增加RemoveComponent、ReplaceComponent接口，用于控制实体的组件

#### SubsystemFurnitureBlockBehavior
增加OnFurnitureDesigned接口：能够控制创建家具时，产生的家具个数、是否摧毁原有模型，以及家具锤掉落的耐久。如果家具锤耐久不足，则创建家具失败。Mod可以通过该接口修复“家具锤敲已有家具后，能够复制家具”的bug

#### DamageItem
提供接口用于控制物品掉耐久时执行的操作

#### ComponentFactors
增加OnFactorsUpdate接口，用于更新非玩家生物的力量、防御、移速倍率。

#### SubsystemExplosions
增加CalculateExplosionPower接口用于控制爆炸的爆炸强度

#### SubsystemSky
增加OnLightningStrike接口，用于控制雷电劈下时的爆炸强度，距离上一次闪电的时间间隔等属性，模组也可以在这个接口里面添加其他行为。

#### TerrainContentsGenerator23
增加OnTerrainBrushesGenerated接口，用于调整原版已有矿物、水域、植物等地形地貌的生成，例如减少原版矿物生成量。  
模组自身的建筑等生成建议调用OnTerrainContentsGenerated接口

#### ComponentBody
增加以下ModLoader接口：
* OnComponentBodyExploded：用于控制动物在受到爆炸时的伤害、击退等属性
* UpdateComponentBody: 进行实体的更新。建议只对自己mod的实体进行更新，不要插手其他mod的实体。

增加了以下变量：
* TerrainCollidable: 该ComponentBody是否能与地形碰撞。关闭状态意味着该生物能穿墙。
* BodyCollidable: 该ComponentBody是否能与其他动物碰撞
* FluidCollidable: 该ComponentBody是否能受到流体影响
* IsRaycastTransparent: 该生物不能被Raycast选中

#### ComponentHealth
有以下接口变化：
* 弃用ComponentHealth中OnCreatureInjure接口，改为使用以下接口：
    * CalculateCreatureInjuryAmount：用于控制受到伤害的数量
    * OnCreatureDying：在生物生命值低于0濒死时触发，如果将生命值改为大于0的数则取消死亡判定。用于mod生物的免死机制
    * OnCreatureDied：在生物死亡判定时触发，可以控制是否计入击杀、以及经验球的掉落数量
* 增加接口：
    * ChangeVisualEffectOnInjury: 用于控制玩家在受伤后的红屏效果、是否会叫
    * OnCreatureSpiked: 动物被仙人掌、海胆、钉板扎的时候执行，可以控制相关伤害。控制该伤害是否被护甲结算等操作。

增加了以下变量：
* m_regenerateLifeEnabled: 是否允许回血
* HealFactor: 在Heal中，回血的倍数
* VoidDamageFactor: y轴过高或者过低造成的伤害系数。该值为0时，不会显示"Come Back!"
* AirLackResilience: 溺水/搁浅伤害抗性，初始值为8.33
* MagmaResilience: 熔岩伤害抗性，初始值为0.5。当该值为Infinity时，生物寻路不再视熔岩为危险方块。
* CrushResilience: 挤压伤害抗性，初始值为6.67
* SpikeResilience: 尖刺伤害抗性，初始值为10
* ExplosionResilience: 爆炸伤害抗性，初始值为1
* StackExperienceOnKill: 生物在死亡时掉落的经验球的掉落物个数不超过100，超出部分进行堆叠。能够降低卡顿。

#### ComponentMiner
有以下接口变化：
* ComponentMiner.AddHitValueParticleSystem允许非玩家攻击：当攻击来源并非玩家时，仍然会创建一个透明的伤害显示粒子。Mod可以调整这个粒子的颜色来令其显现。
* ComponentMiner.AttackBody：变量attacker的类型由ComponentCreature改为Entity
* Raycast方法调整：可以忽略动物、方块或移动方块，达到更灵活的投影
* 增加OnBlockDug接口：用于控制在方块被挖掘完毕后触发的行为，也可以控制挖掘方块后损失的耐久数量

#### ComponentPlayer
增加以下接口：
* OnPlayerControlSteed, OnPlayerControlBoat, OnPlayerControlWalk：用于控制玩家的移动
* OnPlayerInputInteract：能够控制玩家通过点击屏幕、鼠标右键执行Interact操作的时间间隔，以及改变放置方块、使用手中方块、交互面前方块三个操作的优先级，以及模组在其中执行其他操作。
* UpdatePlayerInputAim：每帧都进行更新，可以控制玩家执行瞄准操作的时间间隔。（可以简单理解为投掷攻速），以及模组在其中执行其他操作。
* OnPlayerInputHit：控制玩家在执行攻击输入后，执行的操作。可以控制距离上一次的输入间隔（注意不是玩家攻击间隔）、近战攻击距离。当近战攻击距离≤0时，玩家无法再该情况下执行近战攻击。通常用于枪械等远程武器使用左键攻击。
* UpdatePlayerInputDig：每帧都进行更新，可以控制玩家挖掘方块之间的时间间隔，以及模组在其中执行其他操作。
* OnPlayerInputDrop：控制玩家在按下Q进行丢弃后，执行的操作

增加m_aimStartTime变量：瞄准操作开始的时间

#### InventorySlotWidget
增加以下接口：
* OnInventorySlotWidgetDefined：在InventorySlotWidget中添加模组自定义的元素
* InventorySlotWidgetMeasureOverride：控制物品格子的绘制，绘制耐久度、腐烂图标、物品数量等信息。模组可以在其中绘制模组自带的内容
* HandleMoveInventoryItem：控制物品栏移动物品执行的操作
* HandleInventoryDragProcess：控制物品栏拖动物品执行的操作（比如火药上枪）
* HandleInventoryDragMove：控制物品栏拖动物品，且Process不执行时进行的操作

### 新增变量
* ComponentOnFire增加了以下变量：`m_hideFireParticle`: 为true时，该生物会隐藏火焰音效和粒子
* ComponentRunAwayBehavior：增加变量`LowHealthToEscape`，用于调整动物在生命值低于多少时才触发逃跑行为
* ComponentMiner: 增加变量`HitInterval`，用于控制玩家和一般生物的攻击间隔
* SubsystemTime：将`MaxGameTimeDelta、MaxFixedGameTimeDelta、DefaultFixedTimeStep、DefaultFixedUpdateStep、GameMenuDialogTimeFactor`由常量改为可修改的变量
* SubsystemUpdate：增加`UpdateTimeDebug`字段，用于控制是否在日志中输出各组件更新性能，便于mod对卡顿进行调试
* ComponentHealth：部分字段改为virtual
* 在存档中增加了APIVersion字段
* SubsystemBlockBehavior：不需要再声明HandledBlocks，默认值为空表
* Entity.Components、Project.Subsystems权限改为可写。


### 方块调整
新增方法
* IsCollapseSupportBlock、IsCollapseDestructibleBlock：用于控制方块是否能够承受沙子等重力方块
* IsMovableByPiston、IsBlockingPiston：用于控制方块能否被活塞推动
* IsSuitableForPlants：用于控制方块能否种植植物（如扩展mod的黑土）
* IsFaceSuitableForElectricElements：用于控制方块是否能够放置电路元件（比如允许玻璃上面放置电路元件）
* ShouldBeAddedToProject：是否进入游戏文件动态ID列表
* CanBlockBeBuiltIntoFurniture：方块是否能被计入家具
* ShouldAvoid(int value, ComponentPilot componentPilot)：可以根据ComponentPilot不同来决定是否调整
* IsNonAttachable(int value)：方块能否附着其他方块。该方法是旧版本IsTransparent_的变体。设置为true时，该方块即使是透明方块，也能够放置门、火堆等以前不能放置在透明方块上的物体。
* GetPriorityUse(int value, ComponentMiner componentMiner)
* GetPriorityInteract(int value, ComponentMiner componentMiner)
* GetPriorityPlace(int value, ComponentMiner componentMiner)：用于控制在ComponentPlayer中，使用手中方块、交互面前的方块、放置手中方块这三个行为的优先级。

数据调整
* 空气方块在Content不为0时，显示为白色铁栅栏，名为“错误方块+方块完整值”，便于Mod作者进行调试
* 玩家穿着的衣物方块不必是ClothingBlock或者203号方块，模组可以控制玩家穿戴其他方块

### 其他调整
* 生物图鉴中，战利品数量为0的占位掉落物不显示
* ComponentHealth.Attacked，从受到伤害触发改为受到AttackBody时触发。部分ComponentBehavior也据此改为由ComponentHealth.Injured触发
* PlayerData中SpawnPlayer()执行出现异常时，会返回世界选择界面并记录游戏日志
* 桶堆叠使用bug修复：使用堆叠状态下的桶时，桶的数量正常变化，而不是强制让手上桶的个数减少为1。
* 游戏运行出现错误时，会直接弹出日志窗口，更方便协助定位错误。
* ComponentCraftingTable只在物品栏改变并进行更新后，才执行配方更新操作，降低卡顿。
* 在超平坦世界中，如果地表高度<0，那么会移除最底部的基岩层，达到类似空岛的效果

### 设置调整
设置选项增加：
* 水平创造飞行：开启时飞行方式和MC相同，关闭时飞行方式和移动版相同
* 创造模式拖动整组方块：在创造模式下拖动方块时，会一次拖动一组方块
* Split状态下拖动一半方块：在物品栏中长按一个格子进入Split状态时，拖动该格子的方块会转移该格子的一半数量的方块。
* 删除世界输入"yes"确认：在删除世界时，需要输入yes来确认删除。关闭后和旧版删除方式相同。

## API 1.71A (2024-08-10)

api1.7准正式版

> 中间更新记录丢失

## API 1.63 (2024-04-25)

* 得益于使用第三方图片库ImageSharp，现在额外支持Webp、Gif、Pbm、Qoi、Tiff、Tga图片格式，且Jpg格式不再偏色
   使用无损Webp格式，能比之前使用Png格式减少50%体积，加载快20%，现在Content.zip中的所有Png已替换为Webp；另外，加载Png格式要比之前快50%  
   开发者可以在项目中引用Nuget包SixLabors.ImageSharp来使用该库的完整功能，例如裁剪、缩放、滤镜等，注意最后不要将SixLabors.ImageSharp.dll打包进mod
* Image.Pixels从字段改成只读属性，新增字段m_trueImage，类型是SixLabors.ImageSharp.Image<Rgba32>，新增两个方法：
    * ProcessPixels(Func<Rgba32, Rgba32> pixelFunc, bool shouldUpdatePixelsCache = true)
      用法举例（将图片的所有像素的透明度改成128）：
      image.ProcessPixels(pixel => new Rgba32(pixel.R, pixel.G, pixel.B, 128), true);
    * ProcessPixelRows(PixelAccessorAction<Rgba32> accessorAction, bool shouldUpdatePixelsCache = true)
      用法举例（将图片的所有像素复制到Color数组中）：
```cs
Color pixels = new Color[image.Width * image.Height];
image.ProcessPixelRows(
    accessor => {
        Span<Color> pixelsSpan = m_pixels.AsSpan();
        for (int y = 0; y < accessor.Height; y++)
        {
            //GetRowSpan是获取该行所有像素的Span<Rgba32>，效率极高
            MemoryMarshal.Cast<Rgba32, Color>(accessor.GetRowSpan(y)).CopyTo(pixelsSpan.Slice(y * Width, Width));
        }
    }, false
);
```
* 额外支持音频格式Flac、Mp3，其中Flac必须为16位深  
   现在Content.zip中的所有Wav已替换为体积小解码快的无损Flac  
   （使用第三方音频库NAudio.Core、NAudio.Flac、NLayer.NAudioSupport，开发者可调用NAudio.Core来加载它支持的更多小众格式音频）
* Json加载库替换为.Net自带的System.Text.Json，它比之前的SimpleJson判定更严格，例如不允许多余的逗号之类的，如开发者发现相关报错，请规范自己的Json文件

> 中间更新记录丢失

## API 1.53 (2023-10-29)

* 支持Javascript脚本，可以在游戏菜单直接运行或放进mod中加载并运行。
* 世界名长度上限改为128。
* 解决mod管理器无法扫描0kb文件导致一直加载的问题。
* 修复鼠标指针问题，适配全面屏
* 在游戏社区【只看我的】可以看到自己上传的作品，可删除和发布。
* 修复告示牌报错。
* 部分翻译调整。
* 更改DrawFlatBlock接口的纹理位置获取，贴图异常请重写GetTextureSlotCount并return 1。
* 将BaseFlatBatch的顶点缓存数组从ushort改为int，理论上解决了所有拉丝。
* 解决mod不能使用其他mod中的dll的问题，优化了依赖项加载。
* 让存档导入导出时可保持原本的文件夹结构。
* 更改mod加固规则。
* 解决地形接口只有一个mod生效的问题。
* 解决合成配方界面说明的显示问题。
* 优化游戏内登录逻辑，游戏社区新增登录按钮。
* 新增方块亮度调整接口。
* 新增视图雾颜色调整接口。
* 新增社区管理员模式。

> 中间更新记录丢失

## API 1.44 (2023-09-27)

1. 解决音频卡顿问题，现在不用开音轨缓存了，不会没有声音了。
2. 完善mod管理器，现在进入管理器不会闪退了，除此之外，更新了很多功能。
3. 新增更多接口，mod能实现更多样的效果。
4. 电脑可连续打字，不用一个字一个字码了，同时修复误锁键盘的bug。
5. 优化外置材质的绘制，游玩更流畅。
6. 修复多人同屏地形不显示的bug，现在可以正常地添加或删除玩家。

> 更久以前的更新记录丢失