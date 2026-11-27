# Dung Web demo (cong 5055), dong Desktop va dung instance LocalDB cua demo. Du lieu demo van giu nguyen.
$instance = 'CafeDemoNhom6'
$c = Get-NetTCPConnection -LocalPort 5055 -State Listen -ErrorAction SilentlyContinue
if ($c) { $c | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }; Write-Host 'Da dung Web demo.' } else { Write-Host 'Web demo khong chay.' }
Get-Process -Name CafeManager.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
sqllocaldb stop $instance | Out-Null
