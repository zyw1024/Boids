# 天空之城 · 风之庭园

一个独立的晨光空中花园场景：象牙白的拱廊宫殿、氧化铜穹顶、花园钟塔、悬桥、风化悬崖、垂藤与瀑布，以及穿过天空的燕群。

![Unity 实际相机画面](../../Sky_City_Project/Captures/SkyCity_Style.png)

## 运行

在 Unity 2022.3.62f2c1 打开 `Sky_City_Project/Assets/Boids/Scenes/SkyCity.unity`，切换 Game 后按 Play。相机以 3:2 构图开场，非 3:2 窗口自动留边；现在可以带缓动地探索场景。

- **右键拖动**：围绕城市环视。
- **滚轮**：拉近、拉远。
- **中键拖动**：平移观察中心。
- **F**：平滑回到初始构图。
- **M**：渐变静音 / 恢复 BGM。
- **左键**：继续用于引导鸟群。

- 64 只燕子分成四个小群体，自主选择飞行目标，叠加 Boids 分离、对齐、聚合、预测避障和画面边界约束。初始曲线只决定开场构图，运行中不沿固定轨道。
- 每只鸟有独立的转弯侧倾、爬升、俯冲加速、振翅和滑翔节奏；翼根旋转与翼尖弯折共同表现飞行动作。
- 左键点击画面中右侧开放天空可引导鸟群盘旋；约 7 秒后恢复自主飞行。
- 云海使用三维密度光线步进与自阴影。近景漂移更快、远景更慢，云体持续翻卷；整体分布在有限范围内起伏，避免运行后漂出场景。
- 三面丝绸旗帜的旗根固定，褶皱沿风向传播到自由旗尾，同时有上下起伏和阵风；正面、阴影和深度使用相同变形。
- 原创配乐 **Garden of Winds / 风之庭园**：80 秒、6/8 拍、72 BPM，柔和钢琴、竖琴与弦乐；循环衔接，启动时 3 秒渐入。
- 新增悬空镜水庭园：实时平面倒影、菲涅耳反射、深度吸收、折射、双溢流口方向水流、漂移泡沫与连续瀑布。
- 原来的海底场景继续保留，可直接从 Scenes 文件夹打开。

[实际 Play 模式飞行视频](../../Sky_City_Project/Captures/SkyCity_Flight.mp4)由 Unity 相机连续输出帧编码而成。18 秒、1536 × 1024、30 fps；这是固定时间步长的离线录制，不能作为实时帧率测试。

[水面流动与倒影近景](../../Sky_City_Project/Captures/SkyCity_WaterFlow.mp4)截取上述录像第 2–10 秒的镜水庭园区域，并放大至 900 × 676，方便观察流向与反射扰动。

## 资产与制作边界

- `SkyCity.blend`：可编辑 Blender 源文件。建筑、拱券、栏杆、阶梯、穹顶、岩壁、叶片、花丛、垂藤、瀑布网格以及燕子的身体、双翼和分叉尾羽均为三维几何。
- `build_sky_city.py`：确定性建模脚本，使用 Unity 的 Y-up 设计坐标，转换后从 Blender 导出两个 FBX。源文件中的云网格用于构图预览，在 Unity 中关闭。
- `SkyCity_Concept.png`：AI 生成的美术方向参考，不作为城市或鸟的渲染贴图。
- `DawnCloudscape.png`：早期天空绘景试作，保留制作记录；当前场景不再使用。天空底色由程序渐变和日光晕构成，所有可见云团来自三维密度场。
- `WeatheredLimestone.png`：单独生成的岩石表面颜色纹理，三向投影到真实岩壁网格；表面细节由 shader 补充。
- `SkyCityAtmosphereFeature.cs` 在 URP 不透明物体之后、透明水之前渲染整片体积云。`PerlinWorleyVolume.asset` 保存计算着色器生成的 128³ 周期 FBM/Worley 噪声；224 次视线采样和 7 次光照采样计算消光、散射与自阴影。云的构图分布缓慢起伏，内部密度与细节沿风向移动。旧 `CloudDensity_v4.asset` 与旧云盒生成器已不用于场景。
- `SkyCityLit.shader` 处理顶点颜料、几何遮蔽、岩壁纹理与叶片透光；铜穹顶和石材使用不同的金属度、粗糙度。`SkyCityPool.shader`、`SkyCityWater.shader` 和 `SkyCitySpray.shader` 分别处理池水、瀑布与柔和水雾；旗帜另有风动。
- `SkyCityWaterReflection.cs` 使用镜像相机与斜截投影，每次主相机渲染时更新建筑、鸟与云的真实倒影，排除水本身以避免递归。主画面云为全分辨率，倒影云按其目标的 60% 分辨率计算；每个相机复用自己的渲染目标。

相机围绕开场观察点运动：左右各 65°，俯仰 2.5–38°，距离 32–95；平移范围有限，支持建筑细节观察与构图复位。尚未配置角色行走导航或面向移动设备的 LOD。AI 纹理生成记录见 [provenance.md](provenance.md)。

## 重建

从仓库根目录运行：

```powershell
& 'E:\ProgramFiles\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python 'ArtSource\SkyCity\build_sky_city.py'
```

该命令在独立后台 Blender 进程中重建源文件和 FBX。随后在 Unity 的 Edit 模式保存当前场景，选择 **Boids → Sky City → Build Garden of Winds**。场景生成器会导入模型、建立材质和后期、生成鸟群、保存新场景并加入 Build Settings。

## 验证

**Boids → Sky City → Validate Scene** 检查场景路径、64 只鸟和 128 个翼对象、模型、体积云渲染功能、倒影资源、丢失脚本和 shader 编译错误，报告为 `Captures/SkyCity_Validation.json`。

**Record and Validate Flight** 进入 Play 模式，记录 540 帧并测试位移、振翅范围、鸟群在画面内的数量、两次点击引导、画面外点击拒绝、相机固定性、转弯侧倾、速度范围、滑翔、障碍物净空和实时倒影刷新。结束后恢复录制前的后台运行和捕获帧率设置，并回到 Edit 模式，报告为 `Captures/SkyCity_RuntimeValidation.json`。截图与运行录像用于单独检查构图和动态观感；上述功能检查不代替美术判断。

**Validate Two Minute Autonomous Flight** 另外运行 120 秒无点击引导的模拟，检查自主决策、画面范围与避障稳定性；报告为 `Captures/SkyCity_LongFlightValidation.json`。本次 120 秒检查通过：64 只鸟始终在画面内，193 次群体决策，0 个运行错误。净空按包围主要建筑与水池的解析椭球计算，不能替代逐三角形碰撞测试。

编码命令（需要 FFmpeg）：

```powershell
ffmpeg -y -framerate 30 -i Sky_City_Project/Captures/SkyCityFrames/frame_%04d.png -frames:v 540 -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart Sky_City_Project/Captures/SkyCity_Flight.mp4
```

逐帧缓存不纳入版本控制。保留 Blender 源文件、FBX、材质、纹理、shader、场景、验证记录以及最终截图和视频。

## 实现参考

云的密度分层和细节侵蚀参考 [Guerrilla 的实时体积云技术演讲](https://advances.realtimerendering.com/s2015/index.html)。实现为本项目自行编写，使用程序 FBM/Worley 噪声；没有移植演讲中的完整生产系统。深度重建遵循 [Unity URP 官方说明](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/writing-shaders-urp-reconstruct-world-position.html)。

## 风、配乐与相机预览

[有声动态预览](../../Sky_City_Project/Captures/SkyCity_LivingWorld.mp4)展示云和旗帜的运动、环视、缩放、平移与复位。视频来自 Unity 逐帧相机渲染，声音配入场景使用的同一首原创配乐，并匹配启动渐入；不作为实时帧率证据。

**Boids → Sky City → Record Wind Music and Camera** 检查相机位移、缩放、平移、复位误差和实际 AudioSource 输出，报告为 `Captures/SkyCity_PresentationValidation.json`。

乐谱 MIDI、482 个音符事件、渲染脚本与采样来源位于 [Music](Music/README.md)。
