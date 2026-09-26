# Garden of Winds / 风之庭园

为 Sky City 场景创作的原创循环配乐。没有采用电影《天空之城》的主题旋律或录音。

- 80 秒、32 小节、6/8 拍、72 BPM，D 大调。
- 钢琴承担旋律，竖琴分解和弦，弦乐长音形成轻柔的空间底色。
- 四段结构：主题、主题变奏、上行展开、柔和回归。
- 微小的力度与起音变化、立体声声部位置、房间混响尾音跨循环衔接。
- MIDI：`GardenOfWinds.mid`；完整事件谱：`score-events.json`。
- 最终 Unity 音频：[GardenOfWinds.ogg](../../../Sky_City_Project/Assets/SkyCity/Content/Shared/Audio/GardenOfWinds.ogg)。

## 乐器采样

使用 [Versilian Studios 的 VSCO 2 Community Edition](https://versilian-studios.com/vsco-community/) 钢琴、竖琴、小提琴组与大提琴组录音。厂商以 **CC0-1.0** 提供；原始 [GitHub 仓库](https://github.com/sgossner/VSCO-2-CE) 与本地 `VSCO-CC0-LICENSE.txt` 保留来源与完整许可。`sample-manifest.json` 记录固定版本、35 个备选乐器采样的下载地址与 SHA-256。

`Samples/` 是可重新获取的本地缓存；原始多采样不进入 Unity。场景仅使用渲染后的混音。

## 重建

安装 Python 依赖 `numpy scipy soundfile mido`，并让 FFmpeg 可执行文件位于 PATH。按顺序运行：

```powershell
python ArtSource/SkyCity/Music/prepare_samples.py
python ArtSource/SkyCity/Music/compose_garden_of_winds.py
```

脚本导出 24-bit WAV 母带（本地缓存）、Ogg、MIDI、事件谱和 `audio-validation.json`。钢琴 / 竖琴按科学音高记名；VSCO 弦乐文件按其 C3=中央 C 的规则映射，避免跨库八度错位。

`SkyCitySoundscape` 控制三秒渐入和 M 键静音。仅创建一个 AudioSource 和一个 AudioListener，音乐不随相机远近改变音量。
