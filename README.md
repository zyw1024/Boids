# Boids

**用 AI Vibe Coding 制作一个梦幻海洋鱼群项目。**

目标是让 Boids 群体行为成为可交互、可欣赏的游戏体验：在海洋中点击投放食物，鱼群靠近、进食，再自然散开。课程将从视觉效果出发，逐步解释分离、对齐、聚合及目标吸引的实现。

目前已完成 **Moonveil / 月光鱼** 的建模、骨骼与四段动画，以及 E「彩色梦境」的首版 Unity 风格样片：固定镜头、三维花瓣环境、绘画 Shader 和 70 条静态鱼。**Boids 算法与投喂交互尚未实现。**

![Redon dream sea — Unity camera render](Boids_Proj/Captures/Redon_Style.png)

[观看四段动画预览（MP4）](ArtSource/Moonveil/Preview/Moonveil_Motions.mp4) · [查看 Unity 实机画面](Boids_Proj/Captures/Moonveil_Unity.png) · [美术资源说明](ArtSource/Moonveil/README.md)

**当前版本范围**：[E「彩色梦境」单场景、完全固定镜头，只验证美术风格](ArtSource/Redon/README.md)。上图为实际 Unity 渲染；[E / F 概念图](ArtSource/Concepts/ArtHistoryStudies/README.md)和[第一轮 Shader 概念图](ArtSource/Concepts/ShaderStudies/README.md)保留为参考。当前是风格初稿，后续还需细化植物造型、边缘和笔触层次。

## 快速开始

以下步骤查看当前 E 风格样片。

1. 克隆仓库：

   ```sh
   git clone https://github.com/zyw1024/Boids.git
   ```

2. 在 Unity Hub 中添加仓库内的 **`Boids_Proj`** 文件夹。
3. 使用项目记录的 **Unity 2022.3.62f2c1** 打开，等待包恢复及资源导入。项目使用 **URP 14.0.12**；其他 Unity 版本尚未验证。
4. 打开 `Assets/Boids/Scenes/RedonDream.unity`，切换到 **Game** 视图或按 **Play**。相机与鱼群保持静止，非 3:2 窗口会留边以保留构图。

如需单独检查鱼的四段动画，打开 `Assets/Boids/Scenes/MoonveilPreview.unity` 并按 Play。以下操作仅用于该动画预览：

| 操作 | 功能 |
| --- | --- |
| 按键 1 / 2 / 3 / 4，或底部按钮 | 悬停 / 巡游 / 加速 / 进食 |
| 鼠标左键拖动 | 旋转观察 |
| 鼠标滚轮 | 缩放 |

运行预览不需要启动 AI 客户端、Blender 或 MCP 服务。Unity MCP 编辑器包已固定在 `v10.2.0`，首次恢复该 Git 包需要网络及 Git。

## 目录

```text
Boids/
├── Boids_Proj/              # Unity project: Assets, Packages, ProjectSettings
│   ├── Assets/Boids/        # Fish, animations, painterly shaders and scenes
│   └── Captures/           # Reviewed screenshot and validation results
├── ArtSource/Moonveil/     # Blender source, procedural authoring scripts and GLB
│   └── Preview/            # Stills and animation reel
├── ArtSource/Redon/        # Style implementation notes and AI texture prompts
├── ArtSource/Concepts/     # Archived visual directions
└── Setup/                  # Optional Blender / Unity MCP utilities
```

## 月光鱼资源

- 青蓝色鱼身、珠光腹部、金色眼睛、发光侧线与半透明鱼鳍。
- 13 根骨骼、两个蒙皮网格、两个材质槽，11,596 个三角形。
- **Hover**（3 秒）、**Swim**（1.6 秒）、**Dart**（0.8 秒）循环动画，以及 **Feed**（2 秒）单次动作。
- Unity 预制体以 **+Z 为前方**，关闭根运动，便于后续由 Boids 控制位置和朝向。
- Blender 源文件包含贴图、骨骼、动画和展示灯光；GLB 包含贴图与四段动画，供后续 HTML / Three.js 版本使用。

```csharp
// Animation interface; movement will be supplied by the Boids agent.
fish.SetSwimSpeed(0.45f); // 0 = hover, 0.45 = cruise, 1 = dart
fish.Feed();             // One feeding motion, then resume swimming
```

制作环境为 Blender **5.2.2 LTS**。模型、贴图和动画由仓库内的 Python 脚本生成；无需外部美术素材。重建方式见 [美术说明](ArtSource/Moonveil/README.md) 和 [MCP 工具说明](Setup/README.md)。

## 验证与版本控制

已在 Blender 和 Unity 中检查蒙皮变形、三个循环的首尾衔接、预制体朝向，以及进食后恢复游泳的 Animator 状态切换。检查结果保存在 `ArtSource/Moonveil/Preview/blender-animation-validation.json` 与 `Boids_Proj/Captures/`。

E 样片的场景完整性、Shader、资源持久化与运行检查另见 [Redon 说明](ArtSource/Redon/README.md)。版本按功能分支提交，通过 Pull Request 评审；美术样片与概念图分别保存。

Unity 使用可见 `.meta` 文件与文本序列化。仓库保留 `Assets`、`Packages`、`ProjectSettings` 和美术源文件；排除 `Library`、`Temp`、编辑器日志、用户设置、逐帧渲染缓存及本机 MCP 会话记录。当前资源体积较小，使用普通 Git 保存，不依赖 Git LFS。

## 后续计划

- [x] 三维鱼模型、材质与配套动作
- [x] Unity 动画预览与资源验证
- [x] E 单场景固定镜头美术初稿、绘画 Shader 与 AI 颜料纹理
- [ ] 三维 Boids 鱼群与自然转向
- [ ] 鼠标投喂、食物消耗与鱼群散开
- [ ] 根据实机评审细化环境造型、笔触和动态效果
- [ ] 鱼群数量测试与 LOD / 渲染优化
- [ ] HTML / Three.js 实现
