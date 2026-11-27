# Chay demo: khoi dong Web (http://localhost:5055) va Desktop tren CSDL demo rieng do tao-demo.ps1 tao.
# Dong cua so Web va cua so Desktop khi xong, hoac chay dung-demo.ps1.
$ErrorActionPreference = 'Stop'
$demo     = $PSScriptRoot
$repo     = (Resolve-Path (Join-Path $demo '..\..')).Path
$data     = Join-Path $demo 'du-lieu'
$instance = 'CafeDemoNhom6'
$cs       = "Server=(localdb)\$instance;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true"
$url      = 'http://localhost:5055'
$file     = Join-Path $data 'tai-khoan-demo.txt'

if (-not (Test-Path $file)) { throw 'Chua co CSDL demo. Chay tao-demo.ps1 truoc.' }
$c = Get-NetTCPConnection -LocalPort 5055 -State Listen -ErrorAction SilentlyContinue
if ($c) { throw 'Cong 5055 dang duoc dung. Chay dung-demo.ps1 roi thu lai.' }

sqllocaldb start $instance | Out-Null
Set-Location $repo
Write-Host 'Build...'
dotnet build CafeManager.sln -v q | Select-Object -Last 3
if ($LASTEXITCODE -ne 0) { throw 'Build loi.' }

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__CafeDatabase = $cs
Write-Host 'Khoi dong Web...'
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$repo'; dotnet run --project src/CafeManager.Web --no-launch-profile --no-build --urls $url"

$ok = $false
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 1
    try { if ((Invoke-WebRequest "$url/api/menu" -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { $ok = $true; break } } catch { }
}
if (-not $ok) { throw 'Web khong len trong 60 giay. Xem cua so Web de biet loi.' }
Write-Host "Web da chay: $url" -ForegroundColor Green

$env:CAFE_API_BASE_URL = $url
Write-Host 'Khoi dong Desktop...'
Start-Process dotnet -ArgumentList 'run', '--project', 'src/CafeManager.Desktop', '--no-build' -WorkingDirectory $repo
Start-Process $url
Write-Host ''
Write-Host "Tai khoan mau (mat khau xem trong file): $file" -ForegroundColor Cyan
Get-Content $file | ForEach-Object { '  ' + (($_ -split ' ')[1]) } | Write-Host
