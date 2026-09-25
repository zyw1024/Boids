# Hanging Gardens / 空中花园主岛样板

打开 `Assets/Boids/Scenes/SkyCityHangingGardens.unity`，进入 Play。
右键环绕，中键平移，滚轮接近，F 返回开场；左键可在鸟群活动区域引导飞行。
场景继续使用 Unity 2022.3.62f2c1 / URP 14.0.12。

这是一座完整建模、可从侧面和近处观察的质量参考岛。依据原有天空之城概念图，重点重做了阶梯式建筑轮廓、真实窗洞与拱廊、崖壁、花园和连接伴岛的桥。它没有把概念图用作背景或投影贴图，也没有把高细节资产批量塞进无限世界。原来的 WFC 场景仍是独立的流式世界；后续模块重制可采用本样板的结构与材质标准。

## 资产与光照

- `HangingGardens.blend` 是可编辑的 Blender 源文件；`build_hero.py` 与 `geometry.py` 可确定性重建它及三级 FBX。
- 模型坐标按 Unity 的 Y 轴向上设计，单位为米。拱廊的柱脚、起拱线和楼板相互连接；桥梁拱圈与桥面之间有实心拱肩。前景水池有池底、围边、溢流槽、承托平台的石质牛腿。
- 近景植物由树干、枝条、叶簇和垂藤组成，没有使用实体球替代树冠。风动由顶点着色实现。
- 大面积墙体和岩壁使用独立 UV2 与 Unity Progressive GPU 烘焙间接光；栏杆、瓦片等细节使用光照探针，避免小图块的光照渗漏。几何遮蔽另存于顶点 alpha。
- 材质采用已有的 HonedIvoryLimestone 与 WeatheredLimestone 纹理，以及石灰岩、铜绿、陶瓦、玻璃、木材、叶片等独立表面。石材凹凸经过近景检查，避免高频闪烁。
- 云是世界空间体积密度的光线步进，具有三维噪声、太阳光遮蔽与风场。水面使用实时 URP 平面反射、折射、深度吸收与流动；瀑布和喷雾独立运动。
- 保留原有原创钢琴、弦乐、竖琴配乐与 64 只关节化飞鸟，为新岛配置专用避障体积。

## 重建

在新的 Blender 后台进程运行，不能在正在编辑的 Blender 场景中直接执行：

```powershell
& '你的 Blender 路径/blender.exe' --background --factory-startup --python 'Tools/HeroIsland/build_hero.py'
```

Unity 编译完成后，菜单 `Boids > Hanging Gardens > Build authored reference` 创建场景与可复用岛屿 Prefab；然后执行 `Bake terrace indirect light`。建场景前要保存当前场景。Bake 会重新展开大面积表面的光照 UV 并开始异步烘焙；等烘焙完成后再运行验证、录制或构建。

## 验证与预览

- `SkyCityHeroReview.Validate()` 写入 `Captures/HangingGardens/Validation.json`，检查材质编译、丢失脚本、三级 LOD、体积云、光照贴图、光照探针及水面反射。
- `SkyCityHeroBuilder.BuildPlayer()` 构建单独的 Windows 展示程序。
- 用 `-gardens-benchmark -gardens-output "绝对路径.json"` 启动展示程序，测量 40 秒实际渲染：总览、近景拱廊、水池、侧面。先预热 6 秒；测量期间不截图、不写文件。记录帧耗时、GPU 耗时、真实渲染覆盖率、反射更新、鸟群运动及运行错误。
- Play 模式下运行 `SkyCityHeroReview.Record()`，用 Unity Recorder 录制 20 秒、1800×1200、30 fps、有声音的实况。临时输出为系统临时目录下的 `HangingGardens/GardensLive.mp4`。录屏本身不作为性能测试。
- 样板运行性能与无限世界流式性能是两个独立结果，不能互相替代。

技术验证通过不代表已经达到概念图的全部艺术质量。需要同时查看总览、建筑近景、水面和实况视频，继续检验构图、植被细腻程度、云的自然程度及材质表现。

## 本次实测（2026-09-25）

Windows 独立程序，RTX 3090，1536×1024，60 fps 上限：40.006 秒内实际渲染 2400 帧，P95 16.670 ms、P99 16.720 ms，最大 17.473 ms；GPU P95 14.289 ms。反射更新 971 次，鸟群运动检查通过，运行错误 0。构建错误与警告均为 0。该结果只适用于此样板与上述硬件、分辨率。

三级建筑 LOD 分别为 1,765,953、674,436、240,437 三角形；另有少量独立的水面、瀑布和旗帜。场景包含 2 张光照图及 325 个光照探针。环绕、平移、缩放及 F 返回初始位置已在 Play 模式验证。
