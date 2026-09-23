# 月光鱼 / Moonveil — 第一版鱼类美术资源

用于梦幻海洋 Boids 课程的第一条原创鱼。青蓝色背部、珠光腹部、细鳞片、金色眼睛、发光侧线与淡紫色透明鱼鳍。造型采用真实三维网格；眼睛、鳃线、尾鳍与胸鳍均有几何结构。

## 交付文件

- `Moonveil.blend`：Blender 5.2 源文件，包含模型、13 根骨骼、四段 NLA 动画、打包贴图与展示灯光。展示场景是 `Moonveil_Studio`。
- `Moonveil.glb`：供后续 HTML / Three.js 项目使用，内含材质、贴图和四段动画。
- `../../Boids_Proj/Assets/Boids/Art/Moonveil/Moonveil.fbx`：Unity 导入模型。
- `../../Boids_Proj/Assets/Boids/Art/Moonveil/Moonveil.prefab`：已经配置 URP 材质与 Animator 的可用预制体。
- `../../Boids_Proj/Assets/Boids/Scenes/MoonveilPreview.unity`：Unity 动作预览场景。
- `Preview/Moonveil_Hero.png`、`Moonveil_Side.png`、`Moonveil_Front.png`：Blender 展示与检查图。
- `Preview/Moonveil_Motions.mp4`：四段动画连续预览，画面来源于 Blender。
- `../../Boids_Proj/Captures/Moonveil_Unity.png`：Unity 中的实际材质效果。

## Unity 使用

打开 `MoonveilPreview` 场景，按 Play。点击底部按钮或按 **1–4** 切换悬停、巡游、加速、进食；按住鼠标左键拖动旋转视角，滚轮缩放。进食在这个展示场景里每 3.2 秒触发一次，便于观察。

将 `Moonveil.prefab` 拖入后续海洋场景即可使用。预制体 **+Z 为前方、+Y 为上方**，默认全长约 0.69 米；可整体缩放。视觉子物体的方向与尺度已经换算，Boids 只需要改变预制体根物体的位置和朝向。

`MoonveilMotion.SetSwimSpeed(0..1)` 控制运动强度；`MoonveilMotion.Feed()` 播放一次低头啄食，之后恢复当前游速。移动轨迹与速度由后续 Boids 行为负责，动画关闭根运动。

| 动画 | 时长 | 用途 | 循环 |
| --- | --- | --- | --- |
| Hover | 3.0 秒 | 轻摆尾、胸鳍划水 | 是 |
| Swim | 1.6 秒 | 身体到尾部的侧向波动 | 是 |
| Dart | 0.8 秒 | 更快、更大幅度的摆尾 | 是 |
| Feed | 2.0 秒 | 低头啄食后回正 | 否 |

Animator 参数：`Speed`（float）在 0 / 0.45 / 1 分别对应 Hover / Swim / Dart；`Feed`（trigger）触发进食。动画之间有短暂过渡。

## 资源与验证

- 6,132 个 Blender 顶点，11,596 个三角形，13 根骨骼。
- 两个蒙皮网格、两个材质槽：不透明鱼身和透明鳍膜。眼睛及鳃线颜色合入鱼身贴图，避免每条鱼携带大量材质。
- 鱼身颜色图及法线图：1024×576；鳍膜 RGBA：512×512。透明鳍采用双面渲染。
- `Preview/blender-animation-validation.json`：蒙皮权重和动画起止位置检查。
- `../../Boids_Proj/Captures/Moonveil_Validation.json`：Unity 导入后再次烘焙蒙皮顶点，检查动画确有运动、循环接缝和朝向。

这是第一条风格基准鱼，当前预览用于检查造型与动作。成群时的数量预算、LOD、海水雾、光束及食物粒子将在 Boids 场景里实际测量与调校。

## 可复现制作

`build_moonveil.py` 从参数生成网格、绘制贴图、绑定骨骼、制作四段动画并输出 FBX / GLB / Blend。几何与贴图均由此项目生成，没有外部模型或纹理依赖。脚本根据自身位置定位仓库根目录，也可使用环境变量 `BOIDS_ROOT` 指定位置。

本项目的 `Setup/art_mcp.py` 使用 MCP 协议连接已配置的 Blender / Unity。通过 Blender MCP 执行建模脚本后，在 Unity 菜单选择 **Boids > Moonveil > Build Art Preview** 重建展示资源；**Verify Imported Animation** 验证导入；**Capture Preview** 保存实机画面。重建展示场景前请先保存其他场景工作。

`render_preview.py` 和 `render_motion.py` 使用 Blender 后台渲染，均不修改源文件。
