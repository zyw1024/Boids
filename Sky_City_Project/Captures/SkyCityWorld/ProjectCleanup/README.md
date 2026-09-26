# 单场景项目整理验证

2026-09-26，Unity 2022.3.62f2c1 / Windows。唯一场景与构建入口为 `Assets/SkyCity/Scenes/SkyCityWorld.unity`。

- [Migration.json](Migration.json)：保留资源 GUID 对照、唯一场景、对已删除资源的序列化引用扫描、Python 制作脚本语法和当前文档的本地链接检查。`Runtime` 是新建的分类目录；其子资源保留原 GUID。
- [Assets.json](Assets.json)：场景缺失脚本、网格和材质检查；128 个预制体、384 个 LOD 网格、24 组透明瀑布 LOD；108 个 WFC 布局的确定性与接口约束；10 项花园算法和网格检查。
- [Build.json](Build.json)：最终 Windows 构建成功，错误 0、警告 0。整理后的主岛相机画面已与迁移前的同机位截图核对。
- [Runtime.json](Runtime.json)：最终独立程序的 20 项远端花园检查，覆盖播种、真实菜单回调、规则与树层级、状态卸载和精确恢复、换种子、9／49／25 街区范围及共享网格预算。

独立程序检查使用以下参数，报告路径必须为绝对路径：

```text
SkyCityWorld.exe -district-garden-review -garden-output "绝对路径/Runtime.json"
```

该检查明确调用 URP 相机渲染到 1600×900 HDR 目标，隐藏窗口也执行渲染。它验证公开输入处理函数和菜单回调，不代替物理键鼠测试。帧耗时只代表此处记录的 RTX 3090、本次运行和这些测试阶段。

PPT 与讲义已从仓库当前版本移除，本地课堂副本保留。旧版本仍可从 Git 历史追溯。
