# 天空之城主岛资产

主岛集成在唯一场景 `Assets/SkyCity/Scenes/SkyCityWorld.unity` 中。运行和操作见 [World 文档](../World/README.md)。

`HangingGardens.blend` 是可编辑模型源；`build_island.py` 与 `geometry.py` 生成主岛、两座伴岛及三级 FBX。导出目录为 `Assets/SkyCity/Content/Island`。建筑包含实体拱廊、窗洞、楼板、桥梁和水池；岩体复用 [Rocks 制作源](../../../ArtSource/SkyCity/Rocks/README.md)。

## 重建模型

在 Unity 工程根目录运行独立 Blender 后台进程：

```powershell
& '你的 Blender 路径/blender.exe' --background --factory-startup --python Tools/Island/build_island.py
```

Unity 导入完成后打开 `SkyCityWorld.unity`，选择 **Sky City → Assets → Reimport island meshes**。工具更新已有的渲染网格、光照网格和碰撞壳，保留资产 GUID。建筑几何变化后应重新检查 UV 和烘焙光照；岩体使用环境光与实时光照，避免单个岛体中心的探针将整个岩壁染黑。

## 云海与材质

`SkyCityCloudBuilder` 将造型与三维噪声烘焙到 `SculptedCloudField.asset`，当前世界云海和主岛材质共享该缓存。太阳方向或云造型改变后，在 Edit 模式选择 **Sky City → Assets → Sculpt cloud sea**。`SkyCityCloudNoiseBuilder` 只负责共享噪声资源。

材质位于 `Content/Island`，共享纹理和燕子模型位于 `Content/Shared`，世界云材质和渲染器位于 `Content/World`。唯一场景的光照贴图与反射缓存保存在 `Scenes/SkyCityWorld`。

`SkyCityStyleReview.RefreshAuthoredMeshes(true)` 与 `SkyCityStyleReview.Validate()` 分别更新网格和检查表面一致性；构建与录制使用统一的 World 工具。
