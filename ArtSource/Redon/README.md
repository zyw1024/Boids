# E · 彩色梦境：Unity 交互样片

这是 E 概念方向的实时美术与交互试验：一个三维海洋花园、完全固定的 3:2 镜头，以及 96 条会巡游和进食的月光鱼。此次重做左侧舒展叶片、中央杏粉花冠、下方花床和远景植物，强化紫蓝水域与杏金色光区。

![Unity camera render](../../Sky_City_Project/Captures/Redon_Style.png)

上图由 Unity 场景相机直接渲染，1536 × 1024，没有后期图像修改。[概念图 E](../Concepts/ArtHistoryStudies/E-symbolist-dream-sea.png)只用于构图与色彩参考，没有作为背景板贴入场景。

[实际 Unity 运行视频](../../Sky_City_Project/Captures/Redon_Interaction.mp4) · [投喂截图](../../Sky_City_Project/Captures/Redon_Feeding.png) · [上一版对照](../../Sky_City_Project/Captures/Redon_Style_v1.png)

## 打开与复现

1. 用 Unity 2022.3.62f2c1 打开仓库内的 `Sky_City_Project`，URP 14.0.12。
2. 打开 `Assets/Boids/Scenes/RedonDream.unity`，切到 Game 视图并按 Play。**鼠标左键点击中央水域**投放金色食物；附近鱼逐渐靠近、进食，吃完后短暂散开并恢复巡游。
3. 不同窗口比例会留边以保留 3:2 构图。镜头位置、方向和正交尺寸不响应鼠标或键盘。
4. `Boids > Redon > Capture Style Study` 将相机画面保存到 `Captures/Redon_Style.png`；`Validate Style Scene` 输出场景检查报告。
   `Check Reload and Play Mode` 会检查重载一致性，然后在真实 Play 模式下通过屏幕射线投放食物，检查靠近、消耗、散开、动画、相机和三处食物上限，完成后自动退出 Play。检查由 Unity 编辑器独立执行，不依赖 MCP 全程保持连接。
5. `Build Fixed Camera Style Scene` 根据 C# 作者脚本和已提交纹理重新生成网格、材质与场景。重建会覆盖该样片的手工修改，调整前请提交 Git。普通查看无需重建，也无需 AI / MCP 服务。
6. `Record Interaction Preview Frames` 导出 420 张 960 × 640 实际相机帧，模拟两次通过屏幕坐标投喂，完成后自动退出。以 30 fps 编码为 14 秒 MP4；逐帧缓存不提交 Git。这是固定时间步长录制，不代表实测帧率。

## 绘画感如何实现

| 层次 | 实现 | 作用 |
| --- | --- | --- |
| 三维形体 | Bezier 卷曲叶片、大小错落的花冠、向下渐细的植物柱、花床和枝脉 | 产生真实遮挡与空间层次 |
| 颜料明暗 | Painted Surface Shader，以颜料纹理扰动色阶、露底和少量法线 | 让明暗有色块与不均匀笔触 |
| 叠色 | AI 生成的紫蓝、粉紫和杏金色 underpainting，以及沿叶片 UV 铺开的自然金色叶脉 | 给表面提供交错的颜色层次与植物细节 |
| 干刷 | 独立白色笔触遮罩图集，贴在植物表面和部分轮廓的三维面片上 | 补充刷痕、破碎轮廓和金色细点 |
| 水体 | 按空间深度混色的雾与远处绘画色场 | 让远处形体逐步融入冷暖水色 |
| 整体处理 | 四邻域方差加权的轻量绘画柔化、Bloom、调色和暗角 | 降低几何硬边，保留材质笔触；柔化不承担主要造型 |

四张纹理由内置 imagegen 生成，[完整提示词](PROMPTS.md)已归档。场景基础由 `RedonSceneBuilder.cs` 管理，植物构图由 `RedonGardenComposition.cs` 生成。鱼复用 Moonveil 模型并覆盖为绘画材质；Animator 和 MoonveilMotion 均启用，各条鱼错开动画相位。

`Art Direction - Redon` 对象集中控制颜料强度、法线扰动、雾、冷暖色与亮区位置。每种植物、岩体、鱼色的材质可独立调整。笔触固定在物体或世界空间，Shader 不使用时间驱动的纹理抖动。

场景相机使用独立的 Redon Renderer；原 URP 默认 Renderer 仍为索引 0。Painterly Shader 使用自定义的明暗方向参数，场景中的 Directional Light 不负责其阴影计算。

## 鱼群与投喂

`DreamSchoolController.cs` 使用便于教学阅读的 CPU Boids：分离、对齐、聚合，叠加缓慢变化的巡游目标、软边界、近景植物避让和有限加速度。位置先统一采样，再更新全部速度，避免依赖鱼的更新顺序。朝向平滑转动，动画速度跟随游动速度。

点击通过相机射线映射到固定水下平面，画幅留边和外侧装饰区域不响应投喂。每处食物默认 18 份，最多三处；靠近后逐份消耗并触发进食动作，吃完后附近鱼向外散开约 3.5 秒，再回到巡游。食物以金色颜料颗粒和短暂扩散笔触呈现，35 秒未吃完会消失。

当前更接近 E 的布局和色彩，但花冠边缘、鱼的细节与笔触的自然程度仍有差距。F 场景和海域切换留在后续。此版使用完整鱼模型和较密的花园网格，尚未完成 LOD、GPU 批处理或低配置性能优化；O(N²) 邻域查询适合当前小规模样片，不是千鱼规模方案。

## 验证记录

- [场景检查](../../Sky_City_Project/Captures/Redon_Validation.json)：缺失脚本、四张纹理、材质与 Shader、固定机位、96 条鱼、控制器和 Volume 子资产持久化。
- [运行与重载检查](../../Sky_City_Project/Captures/Redon_RuntimeValidation.json)：实际 Play 模式下的鱼移动、骨骼动画、屏幕坐标投喂、靠近、18 份消耗、散开、画幅外点击拒绝与食物数量上限。
- 保存并重新打开场景后比较相机输出，保存 SHA-256 与逐像素差异；允许最多 0.01% 的像素出现不超过 2/255 的通道差异。动态运行画面预期会变化，不要求与静帧一致；镜头位置、旋转、尺寸和画幅仍要求完全一致。屏幕射线测试覆盖游戏输入处理路径，但不是操作系统鼠标事件回放。

Shader 结构基于项目所用的 [URP 14 官方文档](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/writing-shaders-urp-basic-unlit-structure.html)。目前只验证 Windows / D3D11 / RTX 3090 的编辑器渲染，尚未做独立 Player 构建或跨平台验证。
