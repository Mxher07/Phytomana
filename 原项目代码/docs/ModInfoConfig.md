# Survivalcraft 模组信息文件 (Mod Info) 配置指南

本文档介绍了生存战争插件版模组配置文件（通常为 JSON 格式）的各项参数含义及填写规范。

## 参数一览

| 字段名 | 类型 | 中文名 | 必须 |
| :---: | :---: | :----- | :--: |
| **Name** | 字符串 | 模组名称 | 是 |
| **Version** | 字符串 | 模组版本。建议符合 [Semver 语义化版本](https://semver.org/lang/zh-CN/) 标准 | 是 |
| **ApiVersion** | 字符串 | 模组适配的 API 版本，低于 1.8 的模组将警告可能无法使用，但还是会尝试加载 | 是 |
| **Description** | 字符串 | 模组描述 | 否 |
| **ScVersion** | 字符串 | 模组适配的生存战争游戏版本，没有实际用处 | 是 |
| **LoadOrder** | 整数 | 模组加载顺序，值越小越先加载，默认为 0<br/>建议：主题模组 -100000~-10000，辅助模组 10000~100000<br/>注意：玩家能在游戏中手动修改顺序 | 否 |
| **NonPersistentMod** | 布尔值 | 非持久性模组，默认为 false<br/>若为 true，存档中不会记录该模组，从而在移除该模组后，进入存档时，不会警告未安装该模组；适用于不在存档中存储数据、添加方块、添加实体的模组 | 否 |
| **GameplayImpactLevel** | 字符串 | 玩法影响等级，用于标识模组对游戏平衡性的影响程度，会保存到存档中，默认为 `Cosmetic`，详情另见下方 | 否 |
| **Link** | 字符串 | 模组链接，玩家能从模组管理界面打开；建议设置为带有详细介绍的链接，或者能反馈 Bug 的链接 | 否 |
| **Author** | 字符串 | 模组作者 | 否 |
| **PackageName** | 字符串 | 模组包名，用于区分不同模组，一旦有若干个模组有相同包名，它们将全部不会加载 | 是 |
| **Dependencies** | 对象 | 该模组依赖的其他模组，填写规则另见下方 | 否 |
| **Settings** | 数组 | 模组设置，填写规则另见下方 | 否 |

### GameplayImpactLevel 玩法影响等级

| 可选值 | 中文名 | 示例 |
| :----: | :----: | :--- |
| `Cosmetic` | 纯装饰品 | 材质包、字体包、光影 |
| `Assist` | 轻度辅助 | 小地图（不透视）、箱子整理、显示生物血量、合适成本提升玩家能力 |
| `Turbo` | 强力辅助 | 一键撸树、自动化、矿物雷达、低成本提升玩家能力、合适成本的规则破坏 |
| `Break` | 规则破坏 | 无/低成本地大幅提升玩家能力、飞行、传送门、掉落物/产量倍增 |
| `Godmode` | 上帝模式 | 无敌、瞬移、无限资源 |

## Dependencies 依赖关系

在 1.8.2 之前的插件版中，Dependencies 每项只支持固定的一个版本号，从 1.8.2 开始，Dependencies 将支持范围版本，写法上支持 Nuget 风格和少量 SemVer 风格，规则如下表：

### Nuget 风格

| 示例 | 逻辑范围 | 说明 |
| :--: | :------: | :--- |
| `1.0` | $x \ge 1.0$ | 直接一个版本号，表示最低版本要求 |
| `[1.0,)` | $x \ge 1.0$ | `[` 表示大于等于 |
| `(1.0,)` | $x > 1.0$ | `(` 表示大于 |
| `(,1.0]` | $x \le 1.0$ | `]` 表示小于等于 |
| `(,1.0)` | $x < 1.0$ | `]` 表示小于 |
| `[1.0, 2.0]` | $1.0 \le x \le 2.0$ | 指定闭区间 |
| `[1.0, 2.0)` | $1.0 \le x < 2.0$ | 左闭右开 |
| `(1.0)` | $/$ | 无效 |

### SemVer 风格

| 示例 | 逻辑范围 | 说明 |
| --- | --- | --- |
| `=1.0.0` | $x = 1.0.0$ | 精确匹配 |
| `>1.0.0` | $x > 1.0.0$ | 大于 |
| `>=1.0.0` | $x \ge 1.0.0$ | 大于等于 |
| `<2.0.0` | $x < 2.0.0$ | 小于 |
| `<=2.0.0` | $x \le 2.0.0$ | 小于等于 |
| `^1.0.0` | $1.0.0 \le x < 2.0.0$ | 主版本号相同 |
| `~1.0.0` | $1.0.0 \le x < 1.1.0$ | 次版本号相同 |

> **注意**：Nuget 风格与 SemVer 风格**不可混合使用**  
> ApiVersion 也支持上面写法，但和以前一样，没有实际作用

版本号的写法是 `Major.Minor.Patch.Revision[-Suffix]`，即：主要版本号、次要版本号、补丁版本号、修订版本号、可选后缀，四个版本号均为数字，使用小数点分隔，最后的后缀前面要有`-`，例如：`1.2.3.4-beta`。  
如果转换失败，加载依赖时将只对比字符串是否相同，而不判断范围（像以前的插件版那样）

同时插件版 1.8.2 支持了一种新的写法，先看旧数组写法：

```json
{
    "Dependencies": [
        "PackageNameOfModA:1.2",
        "PackageNameOfModB:3.4",
        "PackageNameOfModC:5.6"
    ]
}
```

现在支持新写法：

```json
{
    "Dependencies": {
        "PackageNameOfModA": "1.2",
        "PackageNameOfModB": "[3.4,)",
        "PackageNameOfModC": ">=5.6"
    }
}
```

两种写法效果相同，但新写法更直观清晰

## Settings 模组设置

从 API 1.9.3 开始，模组可以通过 `Settings` 字段来方便地添加模组设置项，添加后，玩家能在游戏 `设置`-`模组设置` 中调整这些设置

先看示例：

```json
{
    "Settings": [
        {
            "Id": "TemplateModSettingsGroup1", // 用于获取设置值，必须有
            "Name": "Settings Group 1", // 入口按钮的显示名称（支持国际化）
            "Title": "Adjust Template Mod Settings Group 1", // 点开按钮后，在左侧边栏显示的标题（支持国际化）
            "Items": [
                {
                    "Id": "TemplateModSettingsItem1",
                    "Name": "[TemplateMod/Settings/Group1:1]", // 等价于 LanguageControl.Get("TemplateMod", "Settings", "Group1", "1")
                    "Description": "[TemplateMod/Settings/Group1:2]",
                    "Type": "bool", // 基本类型、Game 命名空间之外的类型，需要写完整类名
                    "Default": false,
                    "Widget": "BoolButtonSettingWidget" // 必须为实现了 IModSettingItemWidget 接口且继承自 Widget 类的类名，内置组件详见 ModSettingItemWidget.cs。
                },
                {
                    "Id": "TemplateModSettingsItem2",
                    // 不写 Name 时，会自动尝试从 LanguageControl.Get("ModSettings", PackageName, Id 链, "Name") 获取，Description、Title 同理
                    // 获取失败后，Name、Title 默认为 Id，Description 默认为空字符串
                    "Type": "int",
                    "Default": 0,
                    "Widget": "NumberSliderSettingWidget",
                    // Widget 可从 Descriptor.WidgetProperties 读取下面属性
                    "WidgetProperties": {
                        "MinValue": -10,
                        "MaxValue": 10,
                        "Granularity": 1,
                        "DecimalPlaces": 0
                    }
                },
                // 小标题有两种写法，第一种如下，支持 "[TemplateMod/Settings/Label1]" 本地化
                { "Text": "我是小标题" },
                // 小标题第二种写法，会自动尝试从 LanguageControl.Get("ModSettings", PackageName, Id 链, "Name") 获取
                { "Id": "TemplateModSettingsLabel2" },
                // 分隔线 Separator：
                { "Separator": true },
                {
                    "Id": "TemplateModSettingsSubgroup1",
                    "Name": "More Settings",
                    "Title": "Adjust Template Mod More Settings",
                    "Items": [
                        // 省略，支持嵌套
                    ]
                }
            ]
        },
        // 支持添加多个入口按钮，但因为 Items 为空，不会显示
        {
            "Id": "TemplateModSettingsGroup2",
            // 省略
        }
    ]
}
```
> 也可以另起一个文件 `modsettings.json`，直接写 `[{ "Id": "TemplateModSettingsGroup1", ...}, ...]`，注意，这样做会覆盖 `modinfo.json` 中配置的设置项

有两种方式获取设置值：

1. 从 ModSettingsManager 主动获取

```csharp
bool value = ModSettingsManager.Get<bool>(packageName, "TemplateModSettingsGroup1", "TemplateModSettingsItem1");
// 或者用 TryGet
if (ModSettingsManager.TryGet<bool>(out bool value, packageName, "TemplateModSettingsGroup1", "TemplateModSettingsItem1")) {
    // do something
}
```

2. 从 ModLoader.OnModSettingChanged 订阅更新

```csharp
public class TemplateModLoader : ModLoader {
    // idPath 举例：["TemplateModSettingsGroup1", "TemplateModSettingsItem1"]，不含 packageName
    public overrider void OnModSettingChanged(string[] idPath, object value) {
        // do something
    }
}
```

> 注意：Settings 在语言字符串初始化完成后才会生效