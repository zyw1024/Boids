"""Small reproducible MCP client for Blender authoring and Unity verification."""
import asyncio, base64, json, os, sys, tomllib
from datetime import timedelta
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from mcp.client.streamable_http import streamablehttp_client

ROOT = Path(__file__).resolve().parent

def decode(result):
    blocks = getattr(result, 'contents', None) or getattr(result, 'content', [])
    values = []
    for block in blocks:
        if getattr(block, 'text', None):
            try: values.append(json.loads(block.text))
            except json.JSONDecodeError: values.append(block.text)
        elif getattr(block, 'type', None) == 'image':
            path = ROOT / 'last-mcp-image.png'
            path.write_bytes(base64.b64decode(block.data))
            values.append({'image': str(path)})
    return values[0] if len(values) == 1 else values

async def commands(session, mode, argument):
    await session.initialize()
    if mode == 'unity':
        instances = decode(await session.read_resource('mcpforunity://instances'))
        active = [i for i in instances['instances'] if i['name'] == 'Boids_Proj']
        if len(active) != 1: raise RuntimeError(f'Expected exactly one Boids editor: {instances}')
        result = decode(await session.call_tool('set_active_instance', {'instance': active[0]['id']}))
        if result.get('success') is False: raise RuntimeError(result)
    if argument == 'schemas':
        result = await session.list_tools()
        output = {t.name:t.inputSchema for t in result.tools}
        (ROOT / f'{mode}-schemas.json').write_text(json.dumps(output,indent=2),encoding='utf-8')
        print(json.dumps({'tools':list(output)}),flush=True)
        return
    if mode == 'blender':
        script = Path(argument).resolve()
        code = '__file__ = ' + repr(str(script)) + '\n' + script.read_text(encoding='utf-8')
        result = await session.call_tool('execute_blender_code', {'code': code, 'user_prompt': 'Build the Moonveil fish model and animations for the Boids project.'})
        print(json.dumps(decode(result), ensure_ascii=False),flush=True)
        if result.isError: raise RuntimeError('Blender execution failed')
    else:
        for command in json.loads(Path(argument).read_text(encoding='utf-8-sig')):
            if 'resource' in command: result = await session.read_resource(command['resource'])
            else: result = await session.call_tool(command['tool'], command.get('args',{}))
            value = decode(result)
            print(json.dumps({'command':command, 'result':value},ensure_ascii=False),flush=True)
            if 'save' in command: Path(command['save']).write_text(json.dumps(value,indent=2,ensure_ascii=False),encoding='utf-8')
            if getattr(result,'isError',False): raise RuntimeError(value)

async def main():
    mode, argument = sys.argv[1:3]
    if mode == 'blender':
        cfg = tomllib.loads((Path.home()/'.codex/config.toml').read_text(encoding='utf-8-sig'))['mcp_servers']['blender']
        env = dict(os.environ); env.update(cfg.get('env',{}))
        params = StdioServerParameters(command=cfg['command'], args=cfg.get('args',[]), env=env)
        with (ROOT/'blender-authoring.log').open('a',encoding='utf-8') as log:
            async with stdio_client(params,errlog=log) as (read,write):
                async with ClientSession(read,write,read_timeout_seconds=timedelta(seconds=240)) as s:
                    await commands(s,mode,argument)
    else:
        async with streamablehttp_client('http://127.0.0.1:8080/mcp',timeout=30,sse_read_timeout=120) as (read,write,_):
            async with ClientSession(read,write,read_timeout_seconds=timedelta(seconds=120)) as s:
                await commands(s,mode,argument)

asyncio.run(main())
