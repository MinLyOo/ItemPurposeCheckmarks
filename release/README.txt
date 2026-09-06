ItemPurposeCheckmarks
=====================
基于 AllQuestsCheckmarks（ZGFueDkx, GPL-3.0）与 MoreCheckmarks（TommySoucy, MIT）融合而成的独立任务/用途勾选 Mod。
支持 SPT 4.1+（测试环境: 4.1.3）。

目标
----
在 AllQuestsCheckmarks 的勾选/配色体系上，整合 MoreCheckmarks 的独有信息维度，让仓库内每个物品一目了然地显示它"有何用途"。

功能
----
1. 任务勾选（继承 AQC）
   - 进行中任务需要 / 未来任务需要 / 已完成已集齐（Have enough）区分
   - In Stash 库存计数、Total needed 合计需求
   - Collector 任务专用、小队（Fika）需求
2. 藏身处升级材料（来自 MCM）
   - 显示某区域下一级 / 未来等级缺多少材料、当前持有多少
3. 商人 Barter 交易（来自 MCM）
   - 显示该物品可作为"货币"换到哪些物品、哪个商人
4. 制造配方（来自 MCM）
   - 显示该物品作为制造原料能造出什么
5. 愿望单（来自 MCM）
   - 标记在我愿望单里的物品
6. 其他（来自 MCM）
   - 未来任务前置任务数（还剩几个前置未完成）
   - ON YOU —— 当前身上持有数量
   - 战局内散货 "Take" 交互按钮染色（与勾选同色）

所有维度共用一套勾选颜色仲裁（优先级: 已集齐 > 进行中 > 藏身处缺料 > 未来/Collector > 愿望单 > 交易 > 制造 > 藏身处已够 > 小队 > FIR）。

安装（手动）
------------
将本项目 release 目录下的两个子文件夹复制到你的 SPT 目录即可（保持相对路径）：

1) 客户端插件（BepInEx）
   release\BepInEx\plugins\ItemPurposeCheckmarks
       ->  {游戏目录}\BepInEx\plugins\ItemPurposeCheckmarks
   内含: ItemPurposeCheckmarks-Client.dll、ItemPurposeCheckmarksAssets、locales\{en,ch,pl,ru}.json

2) 服务端 mod（SPT Runtime）
   release\SPT_Runtime\user\mods\ItemPurposeCheckmarks
       ->  {服务器目录}\user\mods\ItemPurposeCheckmarks
   内含: ItemPurposeCheckmarks-Server.dll
   首次启动服务端会自动生成 config.json 与 quest-id-reference.txt

卸载
----
删除上面这两个文件夹即可，无残留，不影响游戏/AQC。

兼容性
------
- 与 AllQuestsCheckmarks（zgfuedkx.allquestscheckmarks）互斥，不可同时安装（服务端已声明 Incompatibilities）。
- 与 MoreCheckmarks 无冲突（GUI 钩子不同）。
- Fika: 软依赖。未装 Fika 时自动跳过小队功能，不影响其余功能。

配置
----
- 客户端: 进游戏后 F12（BepInEx 配置界面）→ ItemPurposeCheckmarks。
  分组: General / Purpose colors / Colors / Text / Debug。
- 服务端: {mod 目录}\config.json 可隐藏指定任务（excludedQuestIds）。
  任务 ID 清单见首次启动生成的 quest-id-reference.txt。

构建
----
- 客户端: Client\Client.csproj（引用 ZGCLib 与 F:\EFT v4.1 的游戏 DLL，仅编译时只读引用）
- 服务端: Server\Server.csproj（NuGet: SPTushonka.Server.Core 4.1.3）
- 输出目录: release\（不会写入游戏目录，天然隔离）
