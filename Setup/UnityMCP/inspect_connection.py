import asyncio
import json
from pathlib import Path
from datetime import timedelta
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client

OUT = Path(__file__).parent / "verification"

async def main():
    OUT.mkdir(parents=True, exist_ok=True)
    async with streamablehttp_client("http://127.0.0.1:8080/mcp", timeout=15, sse_read_timeout=45) as (read, write, _):
        async with ClientSession(read, write, read_timeout_seconds=timedelta(seconds=45)) as session:
            info = await session.initialize()
            tools = await session.list_tools()
            resources = await session.list_resources()
            report = {"server": info.serverInfo.model_dump(), "tools": [t.model_dump() for t in tools.tools], "resources": [r.model_dump(mode="json") for r in resources.resources]}
            for resource in resources.resources:
                if "instances" in str(resource.uri):
                    result = await session.read_resource(resource.uri)
                    report["instances"] = result.model_dump(mode="json")
            (OUT / "discovery.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
            print(json.dumps({"server": report["server"], "tool_names": [t.name for t in tools.tools], "resources": [str(r.uri) for r in resources.resources], "instances": report.get("instances")}, ensure_ascii=False))

asyncio.run(main())
