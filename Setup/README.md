# 可选的 MCP 制作工具

运行 Unity 预览不需要这些工具。它们用于让 AI 客户端通过已配置的 Blender MCP / Unity MCP 制作和检查资源。

## 依赖

- Python 3.11 或更新版本，以及 `mcp` Python SDK（此项目制作时使用 1.30.0）。
- Blender MCP：`mcp-for-blender` 2.0.3；Blender 已安装并启用对应插件。
- Unity MCP：项目 `Packages/manifest.json` 已固定编辑器包 `v10.2.0`；对应服务端为 `mcpforunityserver` 10.2.0。

Blender 工具读取本机 `~/.codex/config.toml` 的 `mcp_servers.blender` 配置；该配置及凭据不进入仓库。Unity 工具默认使用 `http://127.0.0.1:8080/mcp`，并明确选中名为 `Sky_City_Project` 的编辑器实例。

## 使用

在仓库根目录运行，`python` 应指向安装了 `mcp` SDK 的环境：

```sh
python Setup/art_mcp.py blender ArtSource/Moonveil/build_moonveil.py
python Setup/art_mcp.py unity Setup/Examples/inspect_project.json
```

建模命令会重建 `Moonveil_Studio` 中由脚本生成的鱼，并覆盖该鱼的导出文件。脚本通过自身路径定位仓库，也可以通过环境变量 `BOIDS_ROOT` 指定仓库根目录。

也可以直接使用 Blender 的后台 Python 接口重建：

```sh
blender --background --python ArtSource/Moonveil/build_moonveil.py
```

Windows 上，`UnityMCP/Start-UnityMCP.ps1` 会尝试启动已安装在 `~/.local/bin/mcp-for-unity.exe` 的服务端；如果端口 8080 已被使用，则保留现有服务。`inspect_connection.py` 与 `verify_connection.py` 分别用于发现连接与验证项目身份。

工具产生的日志、截图、发现结果和验证会话记录写入本地并被 `.gitignore` 排除。
