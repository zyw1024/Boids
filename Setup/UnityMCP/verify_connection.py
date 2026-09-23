"""Verify MCP routing, project identity, editor readiness and scene access."""
import asyncio
import json
from datetime import timedelta
from pathlib import Path
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client

ROOT = Path(__file__).parent
EXPECTED_PROJECT = ROOT.parent.parent / "Boids_Proj"

def decode(result):
    blocks = getattr(result, "contents", None) or getattr(result, "content", [])
    for block in blocks:
        value = getattr(block, "text", None)
        if value:
            try:
                return json.loads(value)
            except json.JSONDecodeError:
                return {"text": value}
    return result.model_dump(mode="json")

async def main():
    report = {}
    path = ROOT / "verification" / "report.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    async with streamablehttp_client("http://127.0.0.1:8080/mcp", timeout=15, sse_read_timeout=45) as (read, write, _):
        async with ClientSession(read, write, read_timeout_seconds=timedelta(seconds=45)) as session:
            await session.initialize()
            report["instances"] = decode(await session.read_resource("mcpforunity://instances"))
            print(json.dumps({"instances": report["instances"]}, ensure_ascii=False), flush=True)
            if not report["instances"].get("instance_count", 0):
                path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
                raise RuntimeError("No Unity editor is connected yet")
            matches = [i for i in report["instances"].get("instances", []) if i.get("name") == EXPECTED_PROJECT.name]
            if len(matches) != 1:
                raise RuntimeError("Expected exactly one connected Boids_Proj editor")
            report["selection"] = decode(await session.call_tool("set_active_instance", {"instance": matches[0]["id"]}))
            if report["selection"].get("success") is False:
                raise RuntimeError(str(report["selection"]))
            for name, uri in [
                ("project", "mcpforunity://project/info"),
                ("editor", "mcpforunity://editor/state"),
            ]:
                report[name] = decode(await session.read_resource(uri))
                print(json.dumps({name: report[name]}, ensure_ascii=False), flush=True)
            expected = str(EXPECTED_PROJECT.resolve()).replace("\\", "/").lower()
            project_text = json.dumps(report["project"], ensure_ascii=False).replace("\\\\", "/").lower()
            if expected not in project_text:
                raise RuntimeError("Connected project does not match Boids project root")
            report["scene"] = decode(await session.call_tool("manage_scene", {"action": "get_active"}))
            report["hierarchy"] = decode(await session.call_tool("manage_scene", {"action": "get_hierarchy", "max_depth": 2, "page_size": 20}))
            report["errors"] = decode(await session.call_tool("read_console", {"action": "get", "types": ["error"], "count": 20, "format": "detailed"}))
            for name in ("selection", "scene", "hierarchy", "errors"):
                print(json.dumps({name: report[name]}, ensure_ascii=False), flush=True)
    path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

asyncio.run(main())
