# Game Algorithms Implementation · Sky City

**天空之城：用 AI Vibe Coding 将游戏算法做成可以探索、交互和调参的三维场景。**

在云海中的悬空花园与城市之间自由飞行，呼唤燕群，观察递归树木生长，播种并改变花园的元胞演化规则。远处的街区由 WFC 选择兼容建筑模块，并随旅行加载、卸载。

**当前完整工程位于 [`feat/redon-style-scene`](https://github.com/zyw1024/Game-Algorithms-Implementation/tree/feat/redon-style-scene) 分支。** GitHub 默认首页所在的 `main` 保留早期代码；请使用下方克隆命令获取当前项目。本页的图片、课件与源码链接均指向当前开发分支。

![天空之城当前实机画面：悬空花园、石灰岩体、瀑布与远处街区](https://raw.githubusercontent.com/zyw1024/Game-Algorithms-Implementation/feat/redon-style-scene/Sky_City_Project/Captures/RockDiscussion/World_Final.png)

[观看 / 下载 72 秒实机演示](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Captures/SkyCityWorld/LivingCityJourney.mp4?raw=true) · [下载课堂 PPT](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Teaching/Game_Algorithms_Implementation.pptx?raw=true) · [中文讲稿](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Teaching/Teaching_Notes_ZH.md) · [更新记录](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/CHANGELOG.md)

## 当前可以体验什么

- **第一人称天空探索**：在主岛、悬桥和远端街区间飞行，靠近观察建筑、岩壁、植物及水面；支持碰撞约束与一键返回主岛。
- **Boids 鸟群**：燕子自主巡游、转向、振翅和滑翔；右键召集后在前方盘旋，再自然散开。鸟群数量与行为权重可在菜单中调整。
- **WFC 城市生成**：128 个建筑模块、8 种街区构图，配合确定性种子、连接规则和生成权重。按相机位置管理驻留街区，加载范围可设为 9 / 25 / 49 个街区的上限。
- **递归树与元胞花园**：查看树木分层生长，向花坛播种，观察休眠、萌芽、盛开、恢复四种状态之间的传播。主岛与兼容的远端建筑模块均可承载花园。
- **完整场景美术**：拱廊宫殿、铜穹顶、悬桥、体积云、反射水面、瀑布、风动植物及原创配乐共同构成游览体验。
- **新版岩体**：9 种分别设计的岩体构型、错落断面与下垂尖峰，结合浅色石灰岩材质、贴岩藤蔓和瀑布凹槽；主岛和远端街区共用这套资产。

![新版岩体近景](https://raw.githubusercontent.com/zyw1024/Game-Algorithms-Implementation/feat/redon-style-scene/Sky_City_Project/Captures/RockDiscussion/Cliff_Final.png)

## 快速运行

已验证的开发环境为 **Windows、Unity 2022.3.62f2c1、URP 14.0.12**。通过 Unity Hub 安装对应编辑器；Git 用于克隆工程和恢复 Git 包依赖。

1. 克隆当前开发分支：

   ```sh
   git clone --depth 1 --single-branch --branch feat/redon-style-scene https://github.com/zyw1024/Game-Algorithms-Implementation.git
   ```

2. 在 Unity Hub 中添加仓库内的 **`Sky_City_Project`** 文件夹，等待资源导入与包恢复。
3. 打开 **`Assets/Boids/Scenes/SkyCityWorld.unity`**，按 **Play**。
4. 在编辑器中点击 **Game** 画面进入探索；按 **Esc** 打开场景设置。初次进入时等待附近街区加载。

仓库已包含运行所需的模型、材质、场景和预生成数据。**运行场景无需启动 Blender、AI 客户端或 MCP 服务**；这些工具用于制作和修改项目。Blender 源文件与生成脚本也保留在仓库中，方便进一步开发。

## 操作与设置

以下操作适用于当前入口 `SkyCityWorld.unity`。

| 操作 | 功能 |
| --- | --- |
| W / A / S / D + 鼠标 | 沿视线飞行、转向 |
| Q / E | 下降 / 上升 |
| Shift | 加速飞行 |
| 滚轮 | 调整移动速度 |
| 鼠标右键 | 呼唤鸟群 |
| G | 向瞄准的花坛播种，最远 45 米 |
| Esc | 打开 / 关闭场景设置并释放鼠标 |
| F | 返回主岛 |
| M | 静音 / 恢复配乐 |

场景设置包含三个页面：

| 页面 | 可调整内容 |
| --- | --- |
| 鸟群 | 数量（16–128）、飞行速度、转向、感知半径、间距、分离 / 对齐 / 聚合权重及召集行为 |
| 世界生成 · WFC | 种子、桥梁连接概率、岛缘完整度、花园与高塔权重、风格统一度、求解次数、加载范围及每帧生成预算 |
| 空中花园 | 递归层数、风动、树木生长播放、演化速度、萌芽阈值、盛开与恢复时间、风播种及规则着色 |

鸟群与花园参数即时生效。WFC 种子和生成权重需要点击「应用并更新街区」；加载范围与调度预算即时生效。打开菜单会暂停相机操作，场景中的鸟群、云、水与音乐继续运行。

远端花园随街区卸载时保存本次运行中的元胞状态，返回时恢复。**这些状态仅在同一次运行中保留**，退出世界后清除。完整行为与实现说明见[第一人称世界文档](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Tools/SkyCityWorld/README.md)。

## 算法如何对应画面

| 算法 / 技术 | 在项目中的用途 | 主要实现 |
| --- | --- | --- |
| Boids | 分离、对齐、聚合，叠加目标吸引和预测避障 | [SkyCityFlock.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Scripts/SkyCityFlock.cs) |
| Wave Function Collapse | 按权重选择兼容建筑模块，传播邻接和连接约束 | [SkyCityWfc.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Scripts/SkyCityWfc.cs) |
| 参数化递归分枝 | 庭院树木的多层分枝和缓存几何 | [SkyCityBotanyGeometry.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Editor/SkyCityBotanyGeometry.cs) |
| 四状态元胞自动机 | 基于八邻域、同步更新的萌芽、盛开与恢复 | [SkyCityGardenAutomaton.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Scripts/SkyCityGardenAutomaton.cs) |
| 多尺度噪声与光线步进 | 动态体积云和云内明暗 | [Shader 目录](https://github.com/zyw1024/Game-Algorithms-Implementation/tree/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Shaders) |
| 平面反射与 Fresnel 混合 | 水面的建筑倒影与视角相关反射 | [SkyCityWaterReflection.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Scripts/SkyCityWaterReflection.cs) |
| 流式加载、LOD、浮动原点 | 控制常驻对象数量并支持远距离探索 | [SkyCityInfiniteWorld.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Scripts/SkyCityInfiniteWorld.cs) |
| 花园状态存取 | 街区加载时创建植物、卸载时保存状态、返回时恢复 | [SkyCityDistrictGardens.cs](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Assets/Boids/Scripts/SkyCityDistrictGardens.cs) |

WFC 负责模块选择与约束传播，宏观构图和资产造型另外设计。树木使用有限层级的参数化递归分枝；风播种是元胞系统的可关闭外部输入。课堂讲解从可见效果出发，再对应具体规则与实现。

## 课件与实机演示

| 材料 | 内容 |
| --- | --- |
| [英文 PPT](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Teaching/Game_Algorithms_Implementation.pptx?raw=true) | 15 页，中文演讲备注，包含工具、算法用途、提示词与视觉迭代；第 2 页嵌入实机视频，第 12 页展示岩体修改前后的同机位对照 |
| [中文讲稿](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Teaching/Teaching_Notes_ZH.md) | 逐页讲解、课堂落点和源码引用 |
| [工具与资源链接](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Teaching/Tools_Official_Links.md) | Unity、Blender、AI / MCP 工具及制作资源的官方入口 |
| [独立 MP4](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Captures/SkyCityWorld/LivingCityJourney.mp4?raw=true) | 约 72 秒，1920 × 1080、30 fps，含原项目配乐；适合课堂备用播放 |

当前课件与视频包含 **2026-09-26 的岩体重做**，演示源码版本为 [`04570b0`](https://github.com/zyw1024/Game-Algorithms-Implementation/commit/04570b0916b2cb66c981293f2be42347a12d759f)，课件发布于 [`953ce92`](https://github.com/zyw1024/Game-Algorithms-Implementation/commit/953ce92e6766bfac9d25654bee43c16b843e3ce4)。视频由 Unity Recorder 录制实际运行场景，使用编排的游览路线。下载 PPTX 后播放嵌入视频；也可直接使用独立 MP4。

## 美术资产与制作

主岛建筑、悬崖和模块资产通过 Blender 脚本建模、导出后接入 Unity；新增递归树与元胞花园使用 C# 工具生成缓存网格。运行时由 Unity 处理渲染、鸟群、花园状态与街区加载。概念图用于确定美术方向，本页效果图均来自 Unity。

- [主岛模型与场景制作](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Tools/HeroIsland/README.md)：Hanging Gardens 可编辑 Blender 文件与生成脚本。
- [岩体资产说明](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/SkyCity/Rocks/README.md)：9 种构型、源网格、贴岩植物和石灰岩材质。
- [街区与建筑模块](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/SkyCity/Infinite/README.md)：128 个模块和 8 种街区构图的建模来源。
- [原创配乐与采样来源](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/SkyCity/Music/README.md)：Garden of Winds、MIDI、渲染脚本与资源说明。
- [AI 图像制作记录](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/SkyCity/provenance.md)与 [MCP 制作工具](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Setup/README.md)。

## 工程目录

以下为当前开发分支的主要目录：

```text
Game-Algorithms-Implementation/
├── Sky_City_Project/             # 在 Unity Hub 中打开这个目录
│   ├── Assets/Boids/
│   │   ├── Scenes/               # 当前入口 SkyCityWorld.unity
│   │   ├── Scripts/              # 鸟群、WFC、花园、控制与流式加载
│   │   ├── Shaders/              # 岩体、植被、水体与体积云
│   │   ├── Art/                  # 模型、材质、预制体及灯光资源
│   │   └── Editor/               # 生成、构建、录制与验证工具
│   ├── Packages/                # Unity 包版本
│   ├── ProjectSettings/         # 工程配置
│   ├── Tools/                   # 主岛建模脚本与实现文档
│   └── Captures/                # 实机截图、视频和验证报告
├── ArtSource/SkyCity/           # Blender 源文件、岩体、街区、配乐
├── Teaching/                    # 当前 PPT、中文讲稿和资源链接
├── Setup/                       # 可选的 Blender / Unity MCP 工具
└── CHANGELOG.md                 # 迭代记录
```

`Sky_City_Project` 是当前 Unity 工程名。`Assets/Boids` 路径与已有命名空间继续保留，以维持资产引用。仓库保存源资产和导出结果，排除 `Library`、`Temp`、编辑器日志和本机配置；当前使用普通 Git，不依赖 Git LFS。

## 验证记录

新版岩体的检查结果与实机截图一同保留，便于区分资产完整性、运行行为和美术效果。

| 检查 | 最新记录 |
| --- | --- |
| 岩体源网格 | [9 种构型 × 4 档细节，共 36 个网格通过检查](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Captures/RockDiscussion/CliffValidation.json) |
| 瀑布与岩壁 | [12 个主岛采样点未被岩体遮挡](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Captures/RockDiscussion/SpillwayValidation.json) |
| 模块资源 | [128 个 Prefab、384 个 LOD 网格、8 种构图；Shader 错误为 0](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Captures/SkyCityInfinite_AssetValidation.json) |
| 本轮编辑器运行 | [24 个远端街区、64 只鸟，待处理任务、失败任务和兜底次数均为 0](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Sky_City_Project/Captures/RockDiscussion/RuntimeValidation.json) |
| 当前课件 | [15 页、原生算法表格、21 个链接与嵌入视频核对](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/Teaching/Assets/Cliff_Deck_Validation.json) |

这些检查各自覆盖指定资产和运行样本。实机演示视频不作为帧率基准；历史独立程序测试的条件与版本记录在[更新日志](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/CHANGELOG.md)中。岩体的碎裂层次、建筑与地形过渡等视觉细节仍在持续迭代。

<details>
<summary>早期场景与项目历史</summary>

项目从 Moonveil 鱼模型、海底 Boids 与绘画风格实验发展而来。这些资源和场景继续保留，供教学比较及追溯制作过程：

- [Moonveil 鱼模型与动画](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/Moonveil/README.md)
- [Redon 海底 Boids 样片](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/Redon/README.md)
- [早期天空之城场景](https://github.com/zyw1024/Game-Algorithms-Implementation/blob/feat/redon-style-scene/ArtSource/SkyCity/README.md)

旧场景使用各自文档中的控制方式。首次体验当前项目，请打开 `SkyCityWorld.unity`。

</details>
