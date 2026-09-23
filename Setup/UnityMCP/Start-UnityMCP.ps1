$ErrorActionPreference = 'Stop'
$serverExe = Join-Path $env:USERPROFILE '.local\bin\mcp-for-unity.exe'
$logRoot = Join-Path $PSScriptRoot 'verification'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$existing = Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
if ($existing) {
    Write-Output 'A service already listens on port 8080; leaving it running.'
    return
}
$env:UNITY_MCP_TELEMETRY_ENABLED = 'false'
$env:UNITY_MCP_DISABLE_TELEMETRY = '1'
$serverProcess = Start-Process -FilePath $serverExe -ArgumentList @('--transport','http','--http-url','http://127.0.0.1:8080') -WindowStyle Hidden -WorkingDirectory $PSScriptRoot -RedirectStandardOutput (Join-Path $logRoot 'server.stdout.log') -RedirectStandardError (Join-Path $logRoot 'server.stderr.log') -PassThru
$serverProcess.Id | Set-Content -LiteralPath (Join-Path $logRoot 'launched-process-id.txt')
Write-Output ('Started Unity MCP server process {0}.' -f $serverProcess.Id)
