# 动画配置 JSON 参考

本文档是 glTF 生物动画配置文件的完整格式参考。动画配置文件定义了动画如何播放、切换和混合。

配置文件位于 `Assets/Animations/` 目录，在数据库模板中通过 `AnimationConfigPath` 参数引用（不含 `.json` 扩展名）。

## 目录

1. [顶层结构](#1-顶层结构)
2. [模板系统](#2-模板系统)
3. [层级（Layers）](#3-层级layers)
4. [动画定义（Animations）](#4-动画定义animations)
5. [状态规则（States）](#5-状态规则states)
6. [驱动器（Drivers）](#6-驱动器drivers)
7. [动画事件](#7-动画事件)
8. [Root Motion 配置](#8-root-motion-配置)
9. [表达式简介](#9-表达式简介)
10. [JSON 继承](#10-json-继承extends)
11. [完整示例](#11-完整示例)

---

## 1. 顶层结构

```json
{
  "template": "FourLegged",
  "rootBoneRotation": 180,
  "modelScale": 0.01,
  "layers": { ... },
  "animations": { ... },
  "states": { ... },
  "parameters": { ... }
}
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `template` | string | `"Simple"` | 动画模板名称或自定义模板路径 |
| `rootBoneRotation` | float 或 Vector3 | `0` | 根骨骼旋转修正（度）。单数字=绕 Y；`[x,y,z]` 或 `{"X":..,"Y":..,"Z":..}`=绕 X/Y/Z。详见 [根骨骼变换覆盖与混合](#根骨骼变换覆盖与混合) |
| `rootBoneTranslation` | Vector3 | `[0,0,0]` | 根骨骼平移修正（米）。`[x,y,z]` 或 `{"X":..,"Y":..,"Z":..}`。沿实体本地坐标轴（Y 恒上；X/Z 随实体朝向），不受 rootBoneRotation 旋转影响 |
| `modelScale` | float | `1` | 模型缩放。厘米单位模型用 `0.01`，米单位模型用 `1.0` |
| `layers` | object | `{}` | 层级配置，覆盖或补充模板中的层级 |
| `animations` | object | `{}` | 动画别名到 AnimationReference 的映射 |
| `states` | object | `{}` | 状态名到状态层配置（layer + rules）的映射 |
| `parameters` | object | `{}` | 参数初始值 |

---

## 2. 模板系统

模板预定义了层级和驱动器配置。选择合适的模板可大幅简化配置。

### 内置模板

#### Simple

适用于简单实体，只有基本步态控制。

```json
{
  "name": "Simple",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" }
  }
}
```

#### FourLegged

适用于四足动物（狼、狐狸、猫等）。

```json
{
  "name": "FourLegged",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "Head": { "index": 1, "blendMode": "Override", "boneMask": ["Head", "Neck"] },
    "Death": { "index": 2, "blendMode": "Override" }
  }
}
```

#### Human

适用于人形生物。

```json
{
  "name": "Human",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "Activity": { "index": 1, "blendMode": "Additive", "boneMask": ["Hand1", "Hand2"] },
    "Ride": { "index": 2, "blendMode": "Override" },
    "Death": { "index": 3, "blendMode": "Override" }
  }
}
```

#### Bird

适用于鸟类。

```json
{
  "name": "Bird",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "Head": { "index": 1, "blendMode": "Override", "boneMask": ["Head", "Neck"] },
    "Death": { "index": 2, "blendMode": "Override" }
  }
}
```

#### FlightlessBird

适用于不会飞的鸟（鸡、企鹅等）。

```json
{
  "name": "FlightlessBird",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "Head": { "index": 1, "blendMode": "Override", "boneMask": ["Head", "Neck"] },
    "Death": { "index": 2, "blendMode": "Override" }
  }
}
```

#### Fish

适用于鱼类。

```json
{
  "name": "Fish",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "Head": { "index": 1, "blendMode": "Override", "boneMask": ["Jaw"] },
    "Death": { "index": 2, "blendMode": "Override" }
  }
}
```

### 自定义模板

当内置模板不满足需求时，可以在 `Assets/AnimationTemplates/` 目录下创建自定义模板文件。

自定义模板文件格式：

```json
{
  "name": "CustomCreature",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "UpperBodyAction": { "index": 1, "blendMode": "Override" },
    "Death": { "index": 2, "blendMode": "Override" }
  }
}
```

在动画配置中通过文件路径引用自定义模板（不含 `.template.json` 扩展名）：

```json
{
  "template": "AnimationTemplates/CustomCreature",
  ...
}
```

### 模板层配置字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `index` | int | 层级索引，决定混合顺序（从小到大） |
| `blendMode` | string | `"Override"`（覆盖）或 `"Additive"`（叠加） |
| `boneMask` | string[] | 骨骼过滤列表。列出的骨 + 其全部后代 |
| `boneMaskExclude` | string[] | 排除骨骼（同子树语义，从结果集扣除） |
| `weight` | float? | 层权重（0-1）。null/<=0 回退默认 1。Override 控制覆盖强度，Additive 控制叠加强度 |

---

## 3. 层级（Layers）

层级用于实现动画的分层混合。例如基础移动在 Base 层，头部看向在 Head 层，它们独立运行并自动混合。

在配置文件中，`layers` 用于覆盖模板中的层级默认设置，或为层级配置驱动器：

```json
"layers": {
  "Head": {
    "bones": ["b_Head_05"],
    "driver": {
      "type": "LookAt",
      "properties": {
        "TargetBoneName": "b_Head_05",
        "MaxAngleX": 40,
        "MaxAngleY": 15
      }
    }
  },
  "Death": {
    "driver": {
      "type": "Death",
      "properties": {
        "BodyDrop": -0.4
      }
    }
  }
}
```

### 层配置字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `bones` | string[] | 骨骼过滤列表，按子树展开。**省略则保留模板 `boneMask`**；设为非空数组则覆盖 |
| `bonesExclude` | string[] | 排除骨骼（同子树语义）。**省略则保留模板 `boneMaskExclude`**；设为 `[]`（空数组）清除 |
| `weight` | float? | 层权重（0-1）。**省略则保留模板 `weight`**；<=0 视为 1 |
| `driver` | object | 驱动器配置（详见[驱动器章节](#6-驱动器drivers)） |
| `blendMode` | string | 混合模式：`"override"` 或 `"additive"` |
| `blendCurve` | string | 混合曲线：`"linear"` 或 `"smoothstep"` |

### 混合模式

- **Override**：该层动画完全覆盖指定骨骼的变换。多层时，后面的层覆盖前面的层
- **Additive**：该层动画叠加到已有变换上。适合局部动作叠加（如手臂动作叠加到全身）

### 混合规则

层级按 `index` 从小到大处理。每一层的结果与前一层的输出按混合模式和权重混合。最终的骨骼变换用于渲染。

### 骨骼遮罩（子树展开）

`boneMask`/`bones` 与 `boneMaskExclude`/`bonesExclude` 按**子树**展开：列出一个骨 = 该骨 + 它的全部后代。最终影响范围 = include 子树并集 − exclude 子树并集。

- 两者皆空 = 影响全部骨骼。
- include 空 + exclude 非空 = 除 exclude 子树外的全部骨骼。
- 未知骨名忽略（无匹配则 no-op）。

> **⚠ 行为变更（相对旧版本）**：`boneMask`/`bones` 此前为**精确名匹配**（仅列出的骨骼受影响，不含后代），现为**子树匹配**。若模板列出的骨骼带有需独立动画的后代（例如 SC Human 模板配 `Hand1/Hand2` 且装备带手指骨骼），这些后代会一并纳入该层。内置模板的蒙版骨骼多为叶节点，不受影响；第三方模板如出现非预期骨骼被纳入，请复核此点。

示例（人形上下半身分层）：

````json
{
  "Activity": { "boneMask": [ "spine_01" ] },
  "Ride": { "boneMask": [ "pelvis" ], "boneMaskExclude": [ "spine_01" ] }
}
````

- `spine_01` 子树 = 整个上半身（躯干/颈/头/双臂/手指）。
- `pelvis` 子树 − `spine_01` 子树 = pelvis + 双腿（pelvis 是 spine_01 的父，扣掉 spine_01 即移除上半身，剩下下半身）。

---

## 4. 动画定义（Animations）

`animations` 定义动画别名到具体动画片段的映射。状态规则通过别名引用动画。

### 基本格式

```json
"animations": {
  "idle": {
    "source": "Survey",
    "speed": 1.0,
    "loop": true,
    "blendDuration": 0.3
  },
  "walk": {
    "source": "Walk",
    "speed": "[SpeedAbs] * 0.65",
    "loop": true,
    "blendDuration": 0.2
  }
}
```

### AnimationReference 字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `source` | string | 必填 | 动画来源（见下方 source 格式） |
| `speed` | float 或 string | `1.0` | 播放速度。支持表达式字符串 |
| `loop` | bool 或 string | `true` | 是否循环。支持表达式字符串 |
| `blendDuration` | float 或 string | `0.3` | 切换到此动画时的混合过渡时间（秒） |
| `startPhase` | float 或 string | `0.0` | 播放起始相位（0-1） |
| `endPhase` | float 或 string | `1.0` | 播放结束相位（0-1） |
| `preservePose` | bool | `false` | 非循环动画结束后是否保持最终姿态 |
| `events` | array | `[]` | 动画事件列表 |
| `onComplete` | object | null | 动画完成时的动作 |
| `rootMotion` | object | null | Root Motion 配置 |
| `rootBoneRotation` | float 或 Vector3 | null | 根骨骼旋转覆盖（度）。覆盖顶层。静态值，不支持表达式 |
| `rootBoneTranslation` | Vector3 | null | 根骨骼平移覆盖（米）。覆盖顶层 |

### source 格式

`source` 支持多种格式：

| 格式 | 示例 | 说明 |
|------|------|------|
| 动画片段名称 | `"Walk"` | 直接引用模型文件中的动画片段 |
| animation:// 协议 | `"animation://Walk"` | 显式指定动画片段（与裸名等效） |
| driver: 协议 | `"driver:LookAt"` | 使用驱动器替代动画片段 |
| 外部文件 | `"file:path/to/file.glb#AnimName"` | 从外部 GLB 文件加载动画 |
| 参数插值 | `"[GaitState]"` | 从 string 参数取值作为 source（见[参数插值](#参数插值)） |

#### 参数插值

`source` 形如 `"[paramName]"`（整体用方括号包裹）时，引擎从 `Parameters` 取名为 `paramName` 的 **string** 参数值作为实际 source。纯字符串取值，**不走表达式计算**（不支持算术/函数）。

```json
{
  "condition": "[RandomIdleEvent] != ''",
  "animation": {
    "source": "[RandomIdleEvent]",
    "loop": false,
    "onComplete": { "type": "trigger", "name": "RandomIdleComplete" }
  }
}
```

C# 侧 `p.SetString("RandomIdleEvent", "Dance_Loop")` 即让该规则播放 `Dance_Loop`，改值即换动画。

行为要点：

- **参数须为 string 类型**。配置方负责保证：`parameters` 中默认值声明为字符串（如 `"RandomIdleEvent": ""`），C# 用 `SetString` 设值。非 string 参数取值行为未定义。
- **空字符串**：参数为 `""` 时解析后 source 为空 → 该规则不播新动画也**不停旧**，层维持上一帧输出（非停用）。配合 `condition` 判空（如 `[RandomIdleEvent] != ''`）控制是否进入。
- **换值触发重切**：字面量 source 规则在匹配路径不变时跳过重切（去重优化）；`[param]` source 解析后的实际动画名随参数值变化，名变即打破去重、强制层重新从起始相位播放——这是改参数值就能换动画的底层机制。
- **未注册参数**：参数名未声明时取值返回空串（不报错），效果同空字符串。

### 相位裁剪（startPhase / endPhase）

使用一个动画的不同部分作为不同动画：

```json
"jump": {
  "source": "Jump",
  "loop": false,
  "blendDuration": 0.2
},
"jumpLandRecovery": {
  "source": "Jump",
  "startPhase": 0.652,
  "endPhase": 1.0,
  "loop": false,
  "blendDuration": 0.2
}
```

`jumpLandRecovery` 复用 Jump 动画的 65.2% 到 100% 部分。

### preservePose

设为 `true` 时，非循环动画播放结束后骨骼保持在最后一帧的姿态，而不是恢复到默认姿态。适合一次性动作后的持续姿态（如死亡、潜行暂停）：

```json
"death": {
  "source": "Death",
  "loop": false,
  "preservePose": true
}
```

### 根骨骼变换覆盖与混合

`rootBoneRotation` 与 `rootBoneTranslation` 既可顶层配置（全局默认），也可在单个动画（AnimationReference）上覆盖。典型用途：复用动画（如把地面爬行旋转 90° 当作爬梯姿态）。

```json
"climb": {
  "source": "Crawl",
  "rootBoneRotation": [90, 0, 0],
  "rootBoneTranslation": [0, 0.2, 0],
  "blendDuration": 0.3
}
```

**两种写法**（rotation 单数字 = 绕 Y，向后兼容；translation 仅 Vector3）：
- 数组：`[x, y, z]`
- 对象：`{ "X": x, "Y": y, "Z": z }`

**覆盖与回退**：动画显式设置 → 用该值；未设置 → 回退顶层；顶层也未设 → 无变换（Identity）。

**平滑混合**：切入带覆盖的动画时，根骨骼变换随该动画的 `blendDuration` 平滑过渡（smoothstep 曲线）。从当前有效姿态出发，到达目标正好与姿态过渡同步；切出（回到无覆盖动画）同样平滑回正。

- **中断安全**：过渡未完成又切换动画，从当前姿态继续，不突跳。
- **旋转序**：`Yaw(Y) · Pitch(X) · Roll(Z)`（单轴 Y=90 等价旧 `rootBoneRotation: 90`）。
- **平移序**：`R*T`（先旋转后平移）——平移在旋转前的坐标架应用，故不被 `rootBoneRotation` 旋转。轴为实体本地坐标轴（Y 恒指上方；X/Z 随实体朝向），即 `[0, 0.5, 0]` 恒向实体上方抬升 0.5 米。
- **静态值**：不支持表达式（NCalc）。旋转/平移必须是数字或 Vector3 字面量。
- **根运动方向**：旋转同时变换 root motion 方向（绕 Y 旋转后前进方向跟随）；平移不参与根运动。
- **层级停用**：状态规则把某层切到 `animation: null` 时，该层根骨骼变换以 0.2s 过渡回退顶层默认。

---

## 5. 状态规则（States）

状态规则定义了在什么条件下播放什么动画。它们是状态机的主要驱动机制。

### 基本格式

```json
"states": {
  "gait": {
    "layer": "Base",
    "rules": [
      { "condition": "[SpeedAbs] > 0.7 * [WalkSpeed]", "animation": { "source": "run" } },
      { "condition": "[SpeedAbs] > 0.3", "animation": { "source": "walk" } },
      { "condition": "true", "animation": { "source": "idle" } }
    ]
  }
}
```

每个状态层包含：
- `layer`：控制哪个层级
- `rules`：有序规则列表，从上到下评估，第一个匹配的规则生效

### StateLayerConfig 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `layer` | string | 控制的层级名称 |
| `rules` | array | 状态规则列表（有序） |

### StateRuleConfig 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `condition` | string | NCalc 布尔表达式 |
| `animation` | object、string 或 null | 匹配时的 AnimationReference。`null` 表示停用该层；字符串简写等价于 `{ "source": <字符串> }`（见[动画简写](#动画简写)）。与 `rules` 互斥 |
| `rules` | array | 嵌套子规则（决策树）。非空时为分组节点：匹配 `condition` 后递归评估子规则。与 `animation` 互斥 |

### 条件表达式

条件使用 NCalc 表达式语法。规则按列表顺序评估，第一个为 `true` 的规则生效。

#### 引用参数

用 `[参数名]` 引用 AnimationParameters 中的值：

```
[SpeedAbs] > 0.3
[DeathPhase] > 0
[Gait] == 'Run'
[ButtFactor] > 0
```

#### 内置条件

| 条件 | 说明 |
|------|------|
| `IsDead` | 生物是否死亡 |
| `true` | 总是匹配，用作默认规则 |

#### 比较运算

| 运算符 | 说明 | 示例 |
|--------|------|------|
| `>`, `<`, `>=`, `<=` | 数值比较 | `[SpeedAbs] > 0.3` |
| `==`, `!=` | 等值比较 | `[Gait] == 'Run'` |
| `and`, `or` | 逻辑与/或 | `[A] > 0 and [B] < 1` |
| `not` | 逻辑非 | `not IsDead` |

#### 规则中的动画覆盖

规则中的 `animation` 可以覆盖动画的属性：

```json
{
  "condition": "[SpeedAbs] > [WalkSpeed] * 0.7",
  "animation": {
    "source": "run",
    "speed": "[SpeedAbs] * 0.6"
  }
}
```

#### 动画简写

`animation` 为字符串时等价于 `{ "source": <字符串> }`，仅需指定来源时使用：

```json
{ "condition": "[IsDead] == true", "animation": "death" }
```

等价于：

```json
{ "condition": "[IsDead] == true", "animation": { "source": "death" } }
```

需覆盖其他属性（speed、loop 等）时仍用对象形式。简写对 `animations` 别名定义同样生效。

`null` 同样适用：`"animation": null` 等价于 `{ "source": null }`，表示停用该层。

### 停用层级

将 `animation` 设为 `null` 可在特定条件下停用整个层级：

```json
{
  "condition": "[DeathPhase] > 0",
  "animation": null
}
```

### 嵌套规则（决策树）

规则可嵌套 `rules` 形成决策树。外层 `condition` 匹配后递归评估子规则，取子规则中首个匹配的叶子；外层不匹配则**短路整组**（跳过所有子规则评估）。

适用场景：一组共享前缀条件的变体（如蹲下的 idle/walk/run）。外层条件失败时避免逐条评估组内规则。

```json
"rules": [
  {
    "condition": "[CrouchFactor] > 0.0",
    "rules": [
      { "condition": "[SpeedAbs] > 0.2", "animation": { "source": "crouch_walk", "speed": "[Speed] * 1.25" } },
      { "condition": "true", "animation": { "source": "crouch_idle" } }
    ]
  },
  { "condition": "[SpeedAbs] > 0.2", "animation": { "source": "walk", "speed": "[Speed]" } },
  { "condition": "true", "animation": { "source": "idle" } }
]
```

- 站立走：`[CrouchFactor] > 0.0` 失败 → 短路整组 → 匹配 `walk`（仅评估 2 条）
- 蹲下走：外层匹配 → 子规则 `[SpeedAbs] > 0.2` → `crouch_walk`
- 蹲下静止：外层匹配 → 子规则 `true` → `crouch_idle`

#### 嵌套规则要点

- `rules` 与 `animation` 互斥：分组节点（有 `rules`）不能同时设 `animation`，否则加载报错
- `rules` 不可为空数组：`"rules": []` 会加载报错（应省略字段或添加子规则）
- 子规则末尾建议放 `"condition": "true"` 兜底。组内全不匹配时会回溯：跳过整组，继续同层下一规则
- 嵌套深度无限制（实际 2 层够用）
- 子规则的 `animation` 属性覆盖与扁平规则一致（可覆盖 speed/loop 等）
- 状态切换（组切换或组内切换）的过渡与扁平规则相同，走层过渡逻辑

### 多状态层示例

```json
"states": {
  "gait": {
    "layer": "Base",
    "rules": [
      { "condition": "IsDead", "animation": null },
      { "condition": "[SpeedAbs] > 0.7 * [WalkSpeed]", "animation": { "source": "run" } },
      { "condition": "[SpeedAbs] > 0.3", "animation": { "source": "walk" } },
      { "condition": "true", "animation": { "source": "idle" } }
    ]
  },
  "activity": {
    "layer": "UpperBodyAction",
    "rules": [
      { "condition": "[ButtFactor] > 0", "animation": { "source": "bite", "speed": "[ButtFactor]" } },
      { "condition": "[Activity] == 'Feed'", "animation": { "source": "fetch" } },
      { "condition": "true", "animation": null }
    ]
  },
  "death": {
    "layer": "Death",
    "rules": [
      { "condition": "[Death]", "animation": { "source": "death" } }
    ]
  }
}
```

---

## 6. 驱动器（Drivers）

驱动器是替代动画片段的骨骼变换来源。它们通过算法而非关键帧数据来计算骨骼姿态。

> **重要提示**：驱动器主要是为了兼容游戏本体中 `.dae` 模型的 C# 硬编码动画而设计的。对于 glTF 模型，**建议直接使用模型文件中的关键帧动画**，而非驱动器。驱动器仅在以下情况推荐使用：
> - LookAt 驱动器：需要头部/骨骼实时跟踪目标方向
> - Death 驱动器：需要统一的死亡物理效果

### 在层级中配置驱动器

在 `layers` 中为层级指定驱动器：

```json
"layers": {
  "Head": {
    "bones": ["b_Head_05"],
    "driver": {
      "type": "LookAt",
      "properties": {
        "TargetBoneName": "b_Head_05",
        "MaxAngleX": 40,
        "MaxAngleY": 15,
        "PitchAxis": "Z",
        "YawAxis": "Y"
      }
    }
  }
}
```

### 在状态规则中使用驱动器

通过 `driver:` 前缀在状态规则中引用驱动器：

```json
{
  "condition": "true",
  "animation": { "source": "driver:LookAt" }
}
```

### 内置引擎驱动器

#### LookAt 驱动器

让指定骨骼朝向目标方向。常用于头部追踪。

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `TargetBoneName` | `"Head"` | 目标骨骼名称 |
| `LookAngleXParam` | `"LookAngleX"` | 俯仰角参数名 |
| `LookAngleYParam` | `"LookAngleY"` | 偏航角参数名 |
| `MaxAngleX` | `65` | 最大俯仰角（度） |
| `MaxAngleY` | `55` | 最大偏航角（度） |
| `PitchAxis` | `"X"` | 俯仰旋转轴 |
| `YawAxis` | `"Z"` | 偏航旋转轴 |
| `InvertPitch` | `false` | 是否反转俯仰 |
| `InvertYaw` | `false` | 是否反转偏航 |

需要在 `parameters` 中提供 `LookAngleX` 和 `LookAngleY` 参数，或通过 C# 代码设置。

#### Death 驱动器

基于死亡阶段参数计算骨骼的倒下动画。

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `RootBoneName` | null（使用模型根骨骼） | 根骨骼名称 |
| `DeathPhaseParam` | `"DeathPhase"` | 死亡阶段参数名 |
| `BodyHeightParam` | `"BodyHeight"` | 身体高度参数名 |
| `BodyRightParam` | `"BodyRight"` | 身体右侧方向参数名 |
| `DeathCauseOffsetParam` | `"DeathCauseOffset"` | 死因偏移参数名 |
| `RollAngle` | `90` | 倒下时的翻滚角度 |
| `PitchAngle` | `0` | 倒下时的俯仰角度 |
| `BodyDrop` | `0` | 相对身体高度的下降比例 |
| `AutoRollDirection` | `true` | 是否自动朝伤害来源方向翻滚 |

#### Expression 驱动器

通过表达式驱动骨骼变换。

```json
{
  "type": "Expression",
  "properties": {
    "boneConfigs": [
      {
        "boneName": "Head",
        "rotationX": "[LookAngleX]",
        "rotationY": "[LookAngleY]"
      }
    ]
  }
}
```

每个骨骼配置可设置：
- `boneName` — 骨骼名称
- `positionX/Y/Z` — 位置表达式（默认 `"0"`）
- `rotationX/Y/Z` — 旋转表达式（度数，默认 `"0"`）
- `scaleX/Y/Z` — 缩放表达式（默认 `"1"`）

---

## 7. 动画事件

动画事件允许在动画播放到指定时间点时触发 C# 回调。

### 配置格式

在动画定义的 `events` 数组中添加事件：

```json
{
  "source": "Bite",
  "loop": true,
  "events": [
    { "time": 0.577, "name": "AttackHit" },
    { "time": 0.3, "name": "PlaySound", "data": "Audio/Creatures/Bite" }
  ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `time` | float | 触发时间点（归一化 0-1，0 为动画开始，1 为动画结束） |
| `name` | string | 事件名称，用于在 C# 中识别 |
| `data` | string | 可选的附加数据 |

### OnComplete 动作

非循环动画结束时可以自动触发事件：

```json
{
  "source": "Bark",
  "loop": false,
  "onComplete": {
    "type": "trigger",
    "name": "BarkComplete",
    "data": null
  }
}
```

| onComplete 字段 | 说明 |
|-----------------|------|
| `type` | `"trigger"` — 触发事件 |
| `name` | 事件名称 |
| `data` | 事件附加数据 |

---

## 8. Root Motion 配置

Root Motion 允许动画驱动生物的实际移动。最常见的用途是实现动画驱动的跳跃。

### 配置格式

在动画定义中添加 `rootMotion` 配置：

```json
{
  "source": "Jump",
  "loop": false,
  "rootMotion": {
    "sourceBone": "Hips",
    "translation": {
      "mode": "AddImpulse",
      "impulsePhase": 0.182,
      "impulseMethod": "Peak",
      "velocityMask": [0, 1, 0],
      "impulseScale": 2.1,
      "maxImpulse": 15
    }
  }
}
```

### RootMotionConfig 字段

| 字段 | 说明 |
|------|------|
| `sourceBone` | 提取运动的骨骼名称（null = 自动检测） |
| `translation` | 平移配置 |
| `scale` | 缩放配置（可选） |

### TranslationMode

| 模式 | 说明 |
|------|------|
| `"None"` | 不使用 root motion（默认） |
| `"Blend"` | 将动画位移混合到生物速度 |
| `"AddImpulse"` | 在指定动画相位点施加脉冲速度（适合跳跃） |
| `"Override"` | 动画位移直接覆盖生物位置 |

### TranslationConfig 字段

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `mode` | `"None"` | 平移模式 |
| `blendMethod` | `"SmoothDamp"` | Blend 模式的混合方法：`"SmoothDamp"`、`"WeightedAverage"`、`"SpringDamper"` |
| `smoothTime` | `0.3` | SmoothDamp 平滑时间 |
| `impulsePhase` | `-1` | 脉冲触发的动画相位（-1 = 自动） |
| `impulseMethod` | `"Average"` | 脉冲计算方法：`"Average"`、`"Peak"`、`"Weighted"` |
| `impulseScale` | `1.0` | 脉冲缩放因子 |
| `velocityMask` | `[1, 1, 1]` | 各轴是否受影响（0 = 不受影响，1 = 受影响） |
| `impulseOverride` | `null` | AddImpulse：直接指定脉冲向量（body-local，覆盖动画位移计算）。设此项后忽略 `impulseMethod`/`impulseScale` |
| `impulseSpeedOverride` | `null` | AddImpulse：脉冲速度向量（m/s，body-local，由物理体旋转转世界）。覆盖动画位移，可组合方向如 `[0, 2, -6]`（前 6 + 上 2，上扬轻微浮空减地面阻力） |
| `maxSpeed` | `20` | 最大速度限制 |
| `maxImpulse` | `50` | 最大脉冲限制（脉冲速度模长上限） |

详细的 Root Motion 用法参见 [AnimationAdvancedTopics.md](AnimationAdvancedTopics.md)。

---

## 9. 表达式简介

多个配置字段支持表达式字符串。表达式使用 NCalc 语法。

### 表达式触发条件

当值是包含 `[参数名]` 的字符串，或以 `expr:` 前缀开头时，会被作为表达式解析：

```json
"speed": "[SpeedAbs] * 0.65"           // 表达式
"speed": 1.0                           // 静态值
"condition": "[SpeedAbs] > 0.3"        // 条件表达式
```

### 参数引用

`[参数名]` 引用 AnimationParameters 中的值。常用参数：

| 参数 | 类型 | 来源 |
|------|------|------|
| `SpeedAbs` | float | 生物当前移动速度绝对值（需 C# 代码同步） |
| `WalkSpeed` | float | 生物行走速度设定值 |
| `DeathPhase` | float | 死亡阶段 0-1 |
| `LookAngleX` | float | 俯仰角 |
| `LookAngleY` | float | 偏航角 |

参数通过 C# 代码的 `SyncAnimationParameters()` 方法设置到 AnimationController。`base.SyncAnimationParameters()` 已同步大量内置参数（SpeedAbs、DeathPhase、IsDead、WalkSpeed、LookAngleX/Y 等），模组只需添加自定义参数。

### 自定义函数

动画系统注册的函数：

| 函数 | 说明 |
|------|------|
| `lerp(a, b, t)` | 线性插值 |
| `smoothstep(t)` | 平滑阶梯函数 |
| `clamp(v, min, max)` | 钳制值 |
| `degtorad(v)` | 度转弧度 |
| `radtodeg(v)` | 弧度转度 |
| `pi` | 圆周率（零参数函数） |

NCalc 引擎原生提供的数学函数也可直接使用：`abs`, `min`, `max`, `sin`, `cos`, `sqrt`。

更多表达式细节参见 [AnimationAdvancedTopics.md](AnimationAdvancedTopics.md)。

---

## 10. JSON 继承（extends）

动画配置支持 `extends` 属性实现配置文件的继承。这在创建同系列生物变体时很有用。

### 基本用法

```json
{
  "extends": "Animations/BaseFox",
  "animations": {
    "idle": {
      "source": "CustomIdle"
    }
  }
}
```

上述配置继承 `Animations/BaseFox.json` 的所有内容，仅覆盖 `idle` 动画。

### 合并规则

- **对象**：深度合并（递归合并子属性）
- **数组**：默认替换整个数组

### 数组操作符

在需要修改而非替换数组时，可使用特殊操作符：

| 操作符 | 说明 |
|--------|------|
| `$replace` | 替换整个数组（默认行为） |
| `$remove` | 从数组中移除匹配元素 |
| `$prepend` | 在数组前面插入元素 |
| `$append` | 在数组末尾添加元素 |

### 链式继承

配置可以多级继承，系统会自动检测循环引用。

---

## 11. 完整示例

### 最小化四足生物配置

```json
{
  "template": "FourLegged",
  "rootBoneRotation": 180,
  "modelScale": 0.01,

  "animations": {
    "idle": {
      "source": "Survey",
      "speed": 1.0,
      "loop": true,
      "blendDuration": 0.3
    },
    "walk": {
      "source": "Walk",
      "speed": "[SpeedAbs] * 0.65",
      "loop": true,
      "blendDuration": 0.2
    },
    "run": {
      "source": "Run",
      "speed": "[SpeedAbs] * 0.5",
      "loop": true,
      "blendDuration": 0.15
    }
  },

  "states": {
    "gait": {
      "layer": "Base",
      "rules": [
        { "condition": "IsDead", "animation": null },
        { "condition": "[SpeedAbs] > 0.7 * [WalkSpeed]", "animation": { "source": "run" } },
        { "condition": "[SpeedAbs] > 0.3", "animation": { "source": "walk" } },
        { "condition": "true", "animation": { "source": "idle" } }
      ]
    },
    "death": {
      "layer": "Death",
      "rules": [
        { "condition": "[DeathPhase] > 0", "animation": { "source": "driver:Death" } },
        { "condition": "true", "animation": null }
      ]
    }
  },

  "parameters": {
    "WalkSpeed": 1.0,
    "DeathSpeed": 3.0,
    "LookAngleX": 0.0,
    "LookAngleY": 0.0
  }
}
```

### 高级生物配置（含自定义模板、Root Motion、事件）

自定义模板（`Assets/AnimationTemplates/CustomFox.template.json`）：

```json
{
  "name": "CustomFox",
  "layers": {
    "Base": { "index": 0, "blendMode": "Override" },
    "UpperBodyAction": { "index": 1, "blendMode": "Override" },
    "Death": { "index": 2, "blendMode": "Override" }
  }
}
```

动画配置（`Assets/Animations/CustomFox.json`）：

```json
{
  "template": "AnimationTemplates/CustomFox",
  "rootBoneRotation": 180,
  "modelScale": 0.6,

  "animations": {
    "idle": { "source": "Idle", "loop": true, "blendDuration": 0.3 },
    "walk": { "source": "Walk", "loop": true, "blendDuration": 0.25 },
    "run": { "source": "Run", "loop": true, "blendDuration": 0.25 },
    "jump": {
      "source": "Jump",
      "loop": false,
      "blendDuration": 0.2,
      "rootMotion": {
        "sourceBone": "Hips",
        "translation": {
          "mode": "AddImpulse",
          "impulsePhase": 0.182,
          "impulseMethod": "Peak",
          "velocityMask": [0, 1, 0],
          "impulseScale": 2.1
        }
      }
    },
    "bite": {
      "source": "Bite",
      "loop": true,
      "blendDuration": 0.2,
      "events": [
        { "time": 0.577, "name": "AttackHit" }
      ]
    },
    "death": {
      "source": "Death",
      "loop": false,
      "preservePose": true,
      "blendDuration": 0.3
    }
  },

  "states": {
    "gait": {
      "layer": "Base",
      "rules": [
        { "condition": "[Gait] == 'Jump'", "animation": { "source": "jump" } },
        { "condition": "[Gait] == 'Fall'", "animation": { "source": "fall" } },
        { "condition": "[SpeedAbs] > [WalkSpeed] * 0.7", "animation": { "source": "run", "speed": "[SpeedAbs] * 0.6" } },
        { "condition": "[SpeedAbs] > 0.2", "animation": { "source": "walk", "speed": "[SpeedAbs] * 0.65" } },
        { "condition": "true", "animation": { "source": "idle" } }
      ]
    },
    "activity": {
      "layer": "UpperBodyAction",
      "rules": [
        { "condition": "[ButtFactor] > 0", "animation": { "source": "bite", "speed": "[ButtFactor]" } },
        { "condition": "true", "animation": null }
      ]
    },
    "death": {
      "layer": "Death",
      "rules": [
        { "condition": "[Death]", "animation": { "source": "death" } }
      ]
    }
  },

  "parameters": {
    "WalkSpeed": 1.0,
    "SpeedAbs": 0.0,
    "ButtFactor": 0.0
  }
}
```
