# ItemPurposeCheckmarks — 构建与配置手册

独立任务/用途勾选 Mod。继承 AllQuestsCheckmarks（AQC, GPL-3.0）的全部能力，并整合 MoreCheckmarks（MCM, GPLv3）的信息维度。

功能亮点：
- 任务勾选（继承 AQC 全部能力）
- 藏身处升级材料按设施显示（来自 MCM，`Show hideout upgrade materials` 选项控制）
- 商人名对齐显示（服务端路由 `/item-purpose-checkmarks/trader-names` 提供）

- 客户端：BepInEx 插件（`netstandard2.1`）
- 服务端：SPT 编译型 mod（`net10.0`，SPTushonka 4.1.3）
- 版本：1.0.0（SPT ≥ 4.1，测试 4.1.3）

---

## 1. 目录结构

```
ItemPurposeCheckmarks/
├─ BUILD_AND_CONFIG.md         本文档
├─ ItemPurposeCheckmarks-manual-install.zip   手动安装包
├─ release/                    构建产物（隔离输出，不写入游戏目录）
│  ├─ README.txt
│  ├─ BepInEx/plugins/ItemPurposeCheckmarks/   客户端（复制到游戏目录）
│  └─ SPT_Runtime/user/mods/ItemPurposeCheckmarks/   服务端（复制到服务器目录）
├─ Client/                     客户端工程
│  ├─ Client.csproj
│  ├─ Plugin.cs                BepInEx 入口
│  ├─ Helpers/                 StashHelper/SquadQuests/FikaBridge/Settings/QuestsData/
│  │                           QuestJson/Assets/Locales/Utils/QuestsHelper/
│  │                           HideoutHelper/BarterHelper/CraftHelper
│  ├─ Patches/                 QuestItemViewPanelPatch/QuestClassPatch/
│  │                           ProfileSelectionPatch/LocalGameStartPatch/
│  │                           ItemSpecificationPanelPatch/UpdateApplicationLanguagePatch/
│  │                           TakeActionPatch
│  ├─ Assets/Bundle            勾选图标 Bundle
│  └─ locales/                 en/ch/pl/ru.json
├─ Server/                     服务端工程
│  ├─ Server.csproj
│  ├─ ItemPurposeCheckmarksMod.cs       核心路由逻辑
│  ├─ ItemPurposeCheckmarksRouter.cs    静态路由注册
│  ├─ ItemPurposeCheckmarksMetadata.cs  服务端元数据（兼容性声明）
│  ├─ QuestJson.cs             裁剪任务模型
│  └─ ServerConfig.cs          config.json 读取/默认生成
└─ ZGCLib/                     公共库源码（AQC 作者 zgFueDkx 同名库）
```

---

## 2. 构建环境

| 组件 | 版本 | 用途 |
|---|---|---|
| .NET SDK | 10.0（含以上） | 编译客户端与服务端 |
| 游戏目录 | `F:\EFT v4.1` | 客户端编译**只读引用**其 DLL |
| NuGet 包 | SPTushonka.\* 4.1.3 | 服务端依赖（自动还原） |

> 构建对游戏零侵入：`Client.csproj` 里的 `F:\EFT v4.1\...\*.dll` 全部是编译期引用（`HintPath`），运行中游戏开着也不锁文件。产物永远落在 `release/`。

---

## 3. 构建步骤

### 3.1 客户端

```text
# 若游戏目录不是 F:\EFT v4.1，先改 Client.csproj 里一行：
#   <SptDir>F:\EFT v4.1</SptDir>
```

```bash
dotnet build Client/Client.csproj -c Release
```

输出 → `release/BepInEx/plugins/ItemPurposeCheckmarks/`：
`ItemPurposeCheckmarks-Client.dll` + `ItemPurposeCheckmarksAssets`（勾选图标）+ `locales/*.json`。

### 3.2 服务端

```bash
dotnet build Server/Server.csproj -c Release
```

输出 → `release/SPT_Runtime/user/mods/ItemPurposeCheckmarks/ItemPurposeCheckmarks-Server.dll`
（自动还原 SPTushonka 4.1.3，联网或本地已缓存均可）。

### 3.3 一键打包

```bash
Compress-Archive -Path .\release\* -DestinationPath .\ItemPurposeCheckmarks-manual-install.zip -Force
```

---

## 4. 手动安装

把 `release/` 下两个子文件夹复制到目标目录（**保持相对路径**）：

| 来源 | 目标 |
|---|---|
| `release/BepInEx/plugins/ItemPurposeCheckmarks` | `{游戏目录}/BepInEx/plugins/ItemPurposeCheckmarks` |
| `release/SPT_Runtime/user/mods/ItemPurposeCheckmarks` | `{服务器目录}/user/mods/ItemPurposeCheckmarks` |

卸载 = 删除上述两个文件夹即可，无残留。

兼容性要点：
- 与 **AQC**（`zgfuedkx.allquestscheckmarks`）互斥，不可共存（GUI 钩子相同；服务端 `Incompatibilities` 已声明）。
- 与 **MCM** 无冲突。
- **Fika**：软依赖。未装 Fika 时自动跳过小队功能，其余功能照常。

---

## 5. 客户端配置（F12 → ItemPurposeCheckmarks）

进入游戏后按 F12（BepInEx 配置界面）即可修改；也可直接编辑
`{游戏目录}/BepInEx/config/com.kee.itempurposecheckmarks.cfg`。

> F12 界面已汉化，且每个信息维度（藏身处/交易/制造/愿望单/前置数/身上持有/Take 染色）都有独立的“显示”开关。
> 升级版本后若想清理旧配置，可删除上面这个 `.cfg` 文件，游戏启动时会用默认值重新生成中文配置。

五个分组：

### General（通用）
| 选项 | 默认 | 说明 |
|---|---|---|
| Include Collector quest (Fence) | true | 是否计入 Collector（Fence）所需物品 |
| Include non-FiR quests | true | 是否计入无需 FiR 的（可正常上交的）任务 |
| Include loyalty regain quests | false | 是否计入"挽回声望"类任务（Fence 补偿、Lightkeeper Make Amends、Chemical 尾声） |
| Include unreachable quests | false | 是否计入不可达任务（活动任务、其他账号类型任务） |
| Hide checkmark if have enough (in raid) | false | 战局内外出装已集齐时隐藏勾选（与"Include PMC inventory"组合需谨慎） |
| Show only active quests | false | 只显示进行中任务，隐藏未来任务 |
| Include items in PMC inventory (in raid) | false | 战局内是否把 PMC 背包计入 "In Stash" 计数 |
| Mark squad members quests *（仅装 Fika） | true | 标记小队成员当前所需物品 |

### Purpose colors（融合维度开关与配色）
| 选项 | 默认 | 说明 |
|---|---|---|
| Show hideout upgrade materials | true | 标记藏身处升级材料 |
| Show barter trade items | true | 标记可作为商人交易货币的物品 |
| Show crafting ingredients | true | 标记制造配方原料 |
| Show wishlist items | true | 标记愿望单物品 |
| Check all future hideout levels | false | off=只看下一级；on=看所有可达等级 |
| Hideout checkmark only on FiR items | false | 藏身处勾选仅显示在 FiR 物品上 |
| Show quest prerequisite count | true | 未来任务行显示剩余前置任务数 |
| Show 'On You' count | true | 显示当前身上持有数量 |
| Colorize the Take action on loot | true | 战局散货 "Take" 按钮按勾选同色染字 |
| Hideout need color | `#ff8c1a` | 藏身处缺料勾选色 |
| Hideout ready color | `#33cc66` | 藏身处已够料勾选色 |
| Wishlist color | `#ffd400` | 愿望单勾选色 |
| Barter color | `#39c0ed` | 交易勾选色 |
| Craft color | `#b388ff` | 制造勾选色 |

### Colors（沿用 AQC 配色体系）
| 选项 | 默认 | 说明 |
|---|---|---|
| Checkmark color | `#bf00ff` | 未来任务勾选色 |
| Checkmark color (non-FIR) | `#73264d` | 未来任务（非 FiR）勾选色 |
| Collector color | `#bf00ff` | Collector 任务勾选色 |
| Use different color if have enough | false | 已集齐时用单独颜色 |
| Have enough color | `#00ff00` | 已集齐（Have enough）勾选色 |
| Use custom quest checkmark color | false | 进行中任务用自定义色 |
| Custom quest color | `#ffeb6d` | 进行中任务自定义色 |
| Checkmark color (squad members) *（仅装 Fika） | `#ff3333` | 小队需求勾选色 |

### Text（文本配色）
| 选项 | 默认 | 说明 |
|---|---|---|
| Use bullet points | true | 任务清单用 "·" 前缀 |
| Use custom text colors | false | 任务文字用自定义色 |
| Custom text color - active quests | `#dd831a` | 进行中任务文字色 |
| Custom text color - future quests | `#d24dff` | 未来任务文字色 |
| Custom text color - squad quests *（仅装 Fika） | `#ffc299` | 小队任务文字色 |

### Debug
| 选项 | 默认 | 说明 |
|---|---|---|
| Debug logs | false | 在 Player.log 输出调试日志 |
| Reload quests data | 按钮 | 手动从服务端重载任务数据 |

### 勾选颜色仲裁优先级（单一根因）
```
已集齐(Have enough) > 进行中任务 > 藏身处缺料 > 未来/Collector
> 愿望单 > 交易 > 制造 > 藏身处已够 > 小队 > FiR
```
同一物品只会显示**一种**勾选色，来自上面的最高优先级维度。

---

## 6. 服务端配置

### 6.1 config.json（任务排除）
首次启动服务端自动在 `{mod}/user/mods/ItemPurposeCheckmarks/config.json` 生成，可手动编辑：

```json
{
  "hideInactiveEventQuests": true,
  "excludedQuestIds": []
}
```

| 字段 | 默认 | 说明 |
|---|---|---|
| hideInactiveEventQuests | true | 是否隐藏未激活的活动/事件任务 |
| excludedQuestIds | [] | 要完全隐藏的任务 ID 列表（从 quest-id-reference.txt 抄） |

### 6.2 quest-id-reference.txt（任务 ID 参考）
首次启动自动生成 `{mod}/user/mods/ItemPurposeCheckmarks/quest-id-reference.txt`，格式：

```text
# ItemPurposeCheckmarks quest reference - auto-generated on server start.
# Format: Quest Name [Trader] = questId
# Paste a questId into excludedQuestIds in config.json to hide it.
```

如：
```text
First Blood [Prapor] = 59f9d81586f7742a6d4b1f4c ...
```
把行末 `questId` 填入 `excludedQuestIds` 即可隐藏对应任务。

### 6.3 服务端路由（客户端自动消费，无需手动调用）
| 路由 | 说明 |
|---|---|
| `/item-purpose-checkmarks/quests` | 任务数据（含前置/完成条件与排除过滤） |
| `/item-purpose-checkmarks/active-quests` | Fika 队友进行中任务 |
| `/item-purpose-checkmarks/assorts` | 全商人 barter 货物（Fence 走 FenceService） |
| `/item-purpose-checkmarks/trader-names` | 按商人枚举顺序对齐的名称 |
| `/item-purpose-checkmarks/productions` | 藏身处制造配方 |

---

## 7. 开发环境延伸

- **4.1 API 适配示例**：服务端 `DatabaseServer` 已移除 → 用注入的 `HideoutTable` 读 `Production`；商人走 `TraderHelper`；Fence 走 `Services.Commerce.FenceService`。客户端藏身处走 `Singleton<HideoutRepresentation>.Instance`（`HideoutRepresentation` 替换 4.0 的 `HideoutClass`）。
- **物品显示名**：`Utils.GetItemName` 用 `"{模板ID} Name"` 本地化键（EFT 标准，无需运行时 ItemFactory 实例）。
- **隔离开发**：`Client.csproj`/`Server.csproj` 的 PostBuild 只写 `release/`，可放心在游戏开着时 `dotnet build`。真正进游戏测试时才复制 `release/` 内容。

---

## 8. 常见问题

| 现象 | 排查 |
|---|---|
| 勾选不显示 | F12 里对应维度开关是否开启；`Include non-FiR quests` / 任务状态过滤；`Debug logs` 打开看 Player.log |
| “Take” 未染色 | `ColorizeTakeAction` 是否开启；仅对**战局散货**有效（仓库不适用） |
| 服务端没生成 config.json | mod 是否已放入 `user/mods/`；重启服务端 |
| 与 AQC 同时报冲突 | 二者互斥，卸载 AQC |
| 无 Fika 但日志报小队相关 | 忽略，属于软依赖自动跳过路径 |
