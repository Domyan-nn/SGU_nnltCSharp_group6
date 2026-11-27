# Tao lai CSDL demo sach tren mot instance LocalDB rieng ("CafeDemoNhom6"), KHONG cham CSDL Dev cua may.
# Chay tu bat ky dau: powershell -ExecutionPolicy Bypass -File scripts\demo\tao-demo.ps1
# Ma nguon lay tu chinh repo chua script nay (hay git pull nhanh main truoc khi chay).
# Tep CSDL va mat khau tai khoan mau nam o scripts\demo\du-lieu\ (da nam trong .gitignore, KHONG commit).
# Chay lai script nay de quay ve du lieu sach; moi lan chay mat khau moi duoc ghi vao du-lieu\tai-khoan-demo.txt.
$ErrorActionPreference = 'Stop'
$demo     = $PSScriptRoot
$repo     = (Resolve-Path (Join-Path $demo '..\..')).Path
$data     = Join-Path $demo 'du-lieu'
$instance = 'CafeDemoNhom6'
$cs       = "Server=(localdb)\$instance;Database=CafeManager_Group6_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true"

foreach ($tool in 'sqllocaldb', 'sqlcmd', 'dotnet') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Khong tim thay lenh '$tool'. Chay scripts\kiem-tra-may.ps1 de xem may con thieu gi." }
}
$c = Get-NetTCPConnection -LocalPort 5055 -State Listen -ErrorAction SilentlyContinue
if ($c) { Write-Host 'Cong 5055 dang duoc dung (Web demo dang chay). Chay dung-demo.ps1 truoc.' -ForegroundColor Yellow; exit 1 }

Write-Host "Ma nguon: $repo"
Write-Host "Dung va xoa instance $instance cu (neu co)..."
if ((sqllocaldb info) -contains $instance) {
    sqllocaldb stop $instance | Out-Null
    sqllocaldb delete $instance | Out-Null
}
Get-ChildItem $data -ErrorAction SilentlyContinue | Remove-Item -Force
New-Item -ItemType Directory -Force $data | Out-Null

Write-Host 'Tao instance va CSDL moi...'
sqllocaldb create $instance -s | Out-Null
$mdf = Join-Path $data 'cafe_demo.mdf'
$ldf = Join-Path $data 'cafe_demo_log.ldf'
sqlcmd -S "(localdb)\$instance" -E -C -I -b -Q "CREATE DATABASE CafeManager_Group6_Dev ON (NAME=d, FILENAME='$mdf') LOG ON (NAME=l, FILENAME='$ldf')"
if ($LASTEXITCODE -ne 0) { throw 'Khong tao duoc CSDL demo.' }

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__CafeDatabase = $cs
Set-Location $repo
Write-Host 'Build...'
dotnet build CafeManager.sln -v q | Select-Object -Last 3
if ($LASTEXITCODE -ne 0) { throw 'Build loi.' }
Write-Host 'Migrate va seed...'
$out = dotnet run --project src/CafeManager.Web --no-launch-profile --no-build -- --migrate --seed --seed-accounts 2>&1 | Out-String
$acc = ($out -split "`n") | Where-Object { $_ -match '^\s+(Staff|Customer)\s+dev\.' }
if (-not $acc) { Write-Host $out; throw 'Khong tao duoc tai khoan mau' }
$file = Join-Path $data 'tai-khoan-demo.txt'
$acc | ForEach-Object { ($_ -replace '\s+', ' ').Trim() } | Set-Content $file -Encoding utf8
Write-Host "Xong. Tai khoan mau (loai, ten dang nhap, mat khau) nam o: $file" -ForegroundColor Green
Write-Host 'Mat khau chi dung cho CSDL demo tren may nay; khong dua vao git.'
