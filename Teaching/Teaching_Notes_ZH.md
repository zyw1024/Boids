# Game Algorithms Implementation

15 页英文 PPT，中文讲稿。第 2 页嵌入新版岩体的 72 秒实机视频，第 12 页展示同机位岩体对照。

演示源码版本：04570b0916b2cb66c981293f2be42347a12d759f

## 1. Game Algorithms Implementation
建议 0.5 分钟

这是当前 Sky_City_Project 的真实运行截图。项目已经更名并推送到 GitHub；课堂版本位于 feat/redon-style-scene 分支，当前提交为 04570b0。按标题、实况、工具、算法用途、迭代过程、Recap 六部分展开。链接直接指向演示分支。项目目录是 Projects/Sky_City_Project/Sky_City_Project，本地保留独立课件副本，GitHub 仓库的 Teaching 目录提供当前 PPT、中文讲稿和工具链接。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/LivingCity_Hero.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/CHANGELOG.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Teaching/Assets/HKUST_Guangzhou_Logo.png

## 2. Live demonstration
建议 2 分钟

先播放 72 秒实况，再提问学生看到哪些规则在起作用。0:00 主岛与鸟群，0:08 分形树生长，0:26 花园元胞演化，0:34 花园控制，0:40 前往远方，0:45 远端街区花园，1:04 群岛全景。录像由 Unity Recorder 录制运行中的场景，镜头是编排的自动巡游，不能称为人工现场操作。云、水、鸟群、树、花园与加载系统均在运行。配乐为项目原创 Garden of Winds。视频已嵌入第二页，也提供独立 MP4 供课堂备用。 本版演示重新录制于新版岩体导入之后，包含九种岩体构型、石灰岩材质、贴岩藤蔓和瀑布通道；保留树木净空及水池修正。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/LivingCityJourney.mp4
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/LivingCityJourney.json
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Editor/SkyCityLivingWorldRecorder.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/Music/README.md

## 3. Tools for creating the world
建议 2 分钟

逐项说明输入和产出：Codex 协助实现与迭代；图像生成用于概念与纹理参考；Blender 产出可编辑模型、建筑模块与 LOD；Python 负责批量建模和音乐脚本；Unity 与 URP 运行场景和渲染。主岛建筑是 Blender 制作，树与花园的生长逻辑则在运行时生成。图像参考并非作为假背景代替三维场景。

来源
https://openai.com/codex/
https://developers.openai.com/api/docs/guides/image-generation
https://www.blender.org/
https://www.python.org/
https://unity.com/
https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/index.html
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Setup/README.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/Music/README.md

## 4. Tools for connecting and sharing
建议 2 分钟

MCP 是工具连接协议。Unity MCP 在本次工作中固定指向 Sky_City_Project 的编辑器实例，不能根据列表顺序误操作其他项目。Blender MCP 支持编辑器连接，批量资产构建也使用 Blender Python。Recorder 获取实际运行画面，FFmpeg 编码并核验音视频，Git 保存版本，GitHub 共享工程。链接指向项目官方站点或维护者仓库。

来源
https://github.com/CoplayDev/unity-mcp
https://github.com/ahujasid/mcp-for-blender
https://docs.unity3d.com/Packages/com.unity.recorder@4.0/manual/index.html
https://ffmpeg.org/
https://git-scm.com/
https://github.com/
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Setup/README.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/Music/README.md

## 5. Supporting libraries and assets
建议 1 分钟

这些是项目脚本实际使用的支撑库及资源。NumPy、SciPy、SoundFile 和 Mido 用于音符、采样与音频处理；VSCO 2 Community Edition 提供乐器采样，配乐是原创作品，不是电影原声。FontTools 将 Noto Sans SC 子集化以支持游戏中文菜单。资源与软件分工不同，讲解时不把字体、采样说成生成算法。

来源
https://numpy.org/
https://scipy.org/
https://python-soundfile.readthedocs.io/
https://mido.readthedocs.io/
https://fonttools.readthedocs.io/
https://versilian-studios.com/vsco-community/
https://fonts.google.com/noto/specimen/Noto+Sans+SC
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Setup/README.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/Music/README.md

## 6. Algorithms and applications
建议 4 分钟

每种算法用一句话对齐可见结果。当前 SkyCityFlock.cs 包括分离、对齐、凝聚三项，并叠加目标吸引、预测避障与转向约束；这与早期版本不同。WFC 用约束传播选取兼容模块，宏观构图与美术模块另有设计。分形树是参数化递归分枝，不应声称实现了字符串语法形式的 L-system。花园是八邻域同步更新的四状态元胞系统：Rest、Bud、Bloom、Recover；用户与风播种属于外部输入，不应把它说成凭空自发繁殖。这里只解释应用，不推导公式。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityFlock.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityWfc.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Editor/SkyCityBotanyGeometry.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityGardenAutomaton.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityLivingGarden.cs

## 7. Rendering and runtime support
建议 3 分钟

云使用平滑 value noise 的 fBm 与 Worley 密度，沿视线 ray marching；不能因源码命名含 Perlin 就称为严格梯度 Perlin 噪声。水面把反射、Fresnel、吸收与动态流纹组合成可感知的材质。生成和加载是两件事：世界可持续扩展，但相机周围仅驻留有限区块；当前菜单提供 9、25、49 三档预算，并配合 LOD 与浮动原点。当前工作还让树木与花园随着区块加载、卸载，并在同一会话返回时恢复状态。运行负担会随驻留范围、场景和硬件变化，演示录像不能替代性能测量。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/HangingGardens/Water.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityInfiniteWorld.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityDistrictGardens.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Shaders/SkyCityCloudNoise.compute
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Shaders/SkyCityAtmosphere.shader
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Shaders/SkyCityPool.shader
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/DistrictGardens_Runtime.json

## 8. 1. Give the project a visual direction
建议 3 分钟

实际提示词：“海底世界看起来不太行。在加一个场景吧，做天空、鸟群。主题是天空之城。一定要艺术感。开始干活。”另一句基准：“不要考虑简单、复杂，我们只考虑如何把场景做漂亮。”左图为项目保存的生成式概念图，右图为早期 Unity 实机，二者来源明确不同。概念确立象牙白石、铜绿穹顶、暖光、云海和花园，之后通过真实三维建模与材质实现。截图用于展示阶段成果，不声称一条提示词立即生成最终作品。

课堂落点：先确定体验与视觉语言，再评估实现是否接近目标。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/SkyCity_Concept.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/provenance.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCity_Style.png

## 9. 2. Expand through reusable modules
建议 3 分钟

实际提示词：“接下来利用波函数坍塌做无限的场景。做好动态加载工作，不要一口气加载所有场景这样很卡。性能优化问题一定要注意好。把建筑模块化，做100种以上的不同模块。”模块由 Blender 生成，目录包含 16 类、每类 8 个几何变体，共 128 个基础模块。WFC 选择兼容组合；流式加载决定现在保留多少区块。多模块并不自动等于好看，后续反馈暴露了重复和结构问题。

课堂落点：把世界内容、连接规则与运行预算一同提出。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityInfinite_128Modules.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityInfinite_Style.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/Infinite/module-catalog.json
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityWfc.cs

## 10. 3. Turn a scene into a living world
建议 3 分钟

原话：“云要做体积云，水要用URP做效果，鸟飞的太不灵活。你是否理解我的意图，还有太多东西可以提升了。”随后：“水要有倒影和反射的效果”“流动也是必要的”“云要有运动，旗帜要有飘动。场景是动态的。”两张图是云与水的不同研究画面，不是同机位前后对比。结合刚才视频观察云的体积层次、水的反射、瀑布和鸟的动作；静态截图不能单独证明运动。

课堂落点：把“更生动”拆成可观察的运动与光学响应。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/HangingGardens/Clouds_Before.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/HangingGardens/Water.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/README.md

## 11. 4. Make feedback concrete and testable
建议 3 分钟

实际提示词：“还有建筑有些不合理，比如说拱廊结构的拱廊柱子在桥面的下方。你需要审视建筑的合理性。”这里是存档的同机位结构修改对照，重点看桥面、上部廊道与下部承重关系。另一个具体反馈是“水体怎么比以前差了，像一个布条。”这类意见比笼统说“高级一点”更容易定位实现。结构修正是视觉和连接规则检查，不是建筑工程认证。

课堂落点：指出位置与关系，回到对应几何系统，比较同一视角。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityInfinite_StructureBefore.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityInfinite_StructureAfter.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityBridgeGeometry.cs

## 12. 5. Improve composition, light, shadow and materials
建议 3 分钟

此前“没有美感”“重复度有些高啊”等反馈推动了主岛构图的迭代。本页更新为最新一轮岩体评审：用户说“目前的岩体不好看。和概念图差距很大，我们先讨论分析一下原因。”页面提示是这段原话的英文节选。

左右图来自同一主岛和相同机位，展示修改前后的实际 Unity 渲染。先判断大轮廓和体量关系，再看断裂面、材质尺度和明暗。新版在 Blender 中以脚本构建九种分别设计的岩体：主次体量、倾斜和下垂尖峰各不相同，之后才添加细节。主岛和八种远端街区构图使用同一资产族。Unity 中采用连续石灰岩纹理，修正岩体过黑的探针光照，并沿表面设置藤蔓、为瀑布雕出凹槽。

这是一轮同时涉及模型、材质与光照的视觉改进，不是隔离变量的实验，也不表示已经完全达到概念图质量。让学生指出仍可改善的碎岩层次及建筑底部过渡。

课堂落点：先修大形，再修表面；用相同机位的实机图评审变化。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/ArtSource/SkyCity/Rocks/README.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/RockDiscussion/CurrentEditor.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/RockDiscussion/MainIsland_Final.png

## 13. 6. Put the player inside the system
建议 3 分钟

实际提示词：“开始做完整的场景。鼠标右键点击可以呼唤鸟群聚集。wasd + 鼠标 + 滑轮 控制摄像机，第一人称视角。你是否理解我的意图”。展示当前鸟群与 WFC 菜单截图：交互让学生在结果和参数之间建立联系。演示时只改一个关键值，先预测再观察。例如增加凝聚强度可能使群体更紧凑，但不一定解决避障；扩大驻留范围会增加运行负担。不要把早期控制说明误用于当前第一人称版本。

课堂落点：把算法参数变成可以感受和调整的交互。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/FlockMenu.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/WfcSettings.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityFlockMenu.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityWfcMenu.cs

## 14. 7. Carry living systems across the city
建议 4 分钟

前一条原话是提问：“如果我还想把分形算法和元胞自动机算法加入这个场景，你有什么合适的建议吗”，建议后用户回复“赞同”。当前要求：“推广到无限街区”。因此页面第一句是压缩的英译节选，不应说成原始逐字命令。主岛已有递归树与元胞花园，本次将它们扩展到兼容的远端模块。不是给所有模块强行种植：空地、水道、圣所等不适合区域被排除，每街区最多四处花床与两棵树，保留通路和建筑净空。离开后释放对象，同一次会话返回恢复元胞状态；种子变化重新生成。后续反馈是“穿模了”，并明确为“树木或花园与建筑重叠”。修正将下层廊亭屋顶收回露台以下，收窄主树朝向建筑一侧的树冠，并让远端树冠朝向模块空地。这里应区分功能与空间关系：生长正常，不代表完整树冠和风动范围已经避开建筑。右图和第二页视频展示修正后的版本。

课堂落点：把局部效果提升为可加载、可卸载、可恢复的世界系统。 随后用户圈出小水池中的黑白斜纹，问“这是什么”。问题涉及两个层次：水面下面原本是过浅的实心底座，水波可能穿入底座；浅水泡沫又采用了规则条纹。修正为有深度的池盆，并将泡沫限制为边缘的细小、不规则变化。引导学生将视觉问题追溯到几何与着色两方面。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/Botany_Hero.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/LivingCity_RemoteGarden.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityDistrictBotanyLayout.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Assets/Boids/Scripts/SkyCityDistrictGardens.cs
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/CHANGELOG.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/ClippingReview/SameViewAfter.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/WaterReview/After.png

## 15. Recap
建议 3 分钟

回到四个问题：想得到什么可见体验？什么规则能支持它？运行时怎样检查？下一轮请求应具体到哪里？邀请学生选一个主题，如雨夜港口或沙漠遗迹，写一条体验目标、一种算法用途和一个验证方法。学习重点是持续的判断与迭代，不是把某句提示词当作一次生成完整作品的配方。最终工程已推送，学生可以从链接分支复现。

来源
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/Sky_City_Project/Captures/SkyCityWorld/LivingCity_Archipelago.png
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/README.md
https://github.com/zyw1024/Game-Algorithms-Implementation/blob/04570b0916b2cb66c981293f2be42347a12d759f/CHANGELOG.md
