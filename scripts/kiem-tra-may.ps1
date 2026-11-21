# Kiểm tra máy trước khi chạy kiểm thử tay. Chỉ ĐỌC: không tạo, sửa hay xóa dữ liệu, không đăng nhập.
# Chạy ở thư mục gốc của kho mã:  powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-may.ps1
# Thêm -BoQuaBuild để bỏ bước biên dịch (nhanh hơn).
param([switch]$BoQuaBuild)
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$script:loi = 0; $script:canhBao = 0
function Ket($trangThai, $ten, $chiTiet) {
    $nhan = @{ OK = 'ĐẠT   '; LOI = 'LỖI   '; CB = 'CHÚ Ý '}[$trangThai]
    $mau = @{ OK = 'Green'; LOI = 'Red'; CB = 'Yellow' }[$trangThai]
    Write-Host ("[{0}] {1}" -f $nhan, $ten) -ForegroundColor $mau -NoNewline
    if ($chiTiet) { Write-Host ("  - {0}" -f $chiTiet) } else { Write-Host '' }
    if ($trangThai -eq 'LOI') { $script:loi++ } elseif ($trangThai -eq 'CB') { $script:canhBao++ }
}

$goc = (Get-Location).Path
if (-not (Test-Path (Join-Path $goc 'CafeManager.sln'))) { Ket 'LOI' 'Thư mục hiện tại' 'không phải thư mục gốc (không thấy CafeManager.sln); hãy cd vào thư mục doan_CSharp'; exit 1 }
Ket 'OK' 'Thư mục gốc kho mã' $goc

# 1. .NET SDK đúng phiên bản khóa trong global.json
try {
    $yeuCau = (Get-Content (Join-Path $goc 'global.json') -Raw | ConvertFrom-Json).sdk.version
    $dangCo = (& dotnet --version) 2>$null
    if ($dangCo -eq $yeuCau) { Ket 'OK' '.NET SDK' "$dangCo (khớp global.json)" }
    elseif ($dangCo) { Ket 'LOI' '.NET SDK' "đang là $dangCo, global.json yêu cầu $yeuCau; cài đúng bản hoặc báo A" }
    else { Ket 'LOI' '.NET SDK' 'không chạy được lệnh dotnet' }
} catch { Ket 'LOI' '.NET SDK' $_.Exception.Message }

# 2. LocalDB
try {
    $thongTin = (& sqllocaldb info MSSQLLocalDB) 2>&1 | Out-String
    if ($thongTin -match 'Version') { Ket 'OK' 'SQL Server LocalDB (MSSQLLocalDB)' (($thongTin -split "`r?`n" | Where-Object { $_ -match 'Version|State' }) -join '; ') }
    else { Ket 'LOI' 'SQL Server LocalDB' 'không thấy instance MSSQLLocalDB; cài SQL Server Express LocalDB' }
} catch { Ket 'LOI' 'SQL Server LocalDB' 'không chạy được sqllocaldb; cài SQL Server Express LocalDB' }

# 3. sqlcmd (cần cho các câu truy vấn trong hướng dẫn; có thể thay bằng SSMS)
if (Get-Command sqlcmd -ErrorAction SilentlyContinue) { Ket 'OK' 'sqlcmd' 'có (luôn dùng cờ -I, xem docs/huong-dan-demo.md mục 1)' }
else { Ket 'CB' 'sqlcmd' 'không có; dùng SSMS cho các câu truy vấn SQL' }

# 4. Cổng 5055 (Web) phải rảnh trước khi chạy máy chủ; nếu đang có máy chủ của chính mình thì chỉ báo
$cong = Get-NetTCPConnection -LocalPort 5055 -State Listen -ErrorAction SilentlyContinue
if ($cong) {
    $tienTrinh = (Get-Process -Id $cong[0].OwningProcess -ErrorAction SilentlyContinue).ProcessName
    Ket 'CB' 'Cổng 5055' "đang được dùng bởi '$tienTrinh' (nếu là máy chủ Web của dự án thì dùng luôn, nếu không hãy tắt nó)"
} else { Ket 'OK' 'Cổng 5055' 'rảnh' }

# 5. Phiên bản mã (ghi vào biên bản)
if (Get-Command git -ErrorAction SilentlyContinue) {
    $nhanh = (& git rev-parse --abbrev-ref HEAD) 2>$null; $ma = (& git rev-parse --short HEAD) 2>$null
    $doDang = (& git status --porcelain) 2>$null
    if ($ma) {
        Ket 'OK' 'Phiên bản mã (ghi vào biên bản)' "$nhanh @ $ma"
        if ($doDang) { Ket 'CB' 'Thay đổi chưa commit' 'có; kết quả kiểm thử chỉ đúng cho mã đang có trên máy bạn' }
    }
} else { Ket 'CB' 'git' 'không có lệnh git' }

# 6. CSDL Dev: tồn tại, đủ migration, có tài khoản mẫu (chỉ SELECT)
$mayChu = '(localdb)\MSSQLLocalDB'; $csdl = 'CafeManager_Group6_Dev'
if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
    $coCsdl = (& sqlcmd -S $mayChu -E -C -I -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$csdl') IS NULL THEN 0 ELSE 1 END") 2>$null | Select-Object -First 1
    if ($coCsdl -eq '1') {
        $soMigration = (& sqlcmd -S $mayChu -E -C -I -b -d $csdl -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM __EFMigrationsHistory") 2>$null | Select-Object -First 1
        $caCanCo = @(Get-ChildItem (Join-Path $goc 'src\CafeManager.Infrastructure\Migrations') -Filter '*.cs' -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d{14}_.*(?<!\.Designer)\.cs$' }).Count
        if ($soMigration -and $caCanCo -and [int]$soMigration -ge $caCanCo) { Ket 'OK' "CSDL $csdl" "có, $soMigration migration đã áp dụng" }
        else { Ket 'CB' "CSDL $csdl" "có nhưng mới áp dụng $soMigration/$caCanCo migration; chạy lại lệnh --migrate ở docs/huong-dan-demo.md mục 1" }
        $taiKhoan = (& sqlcmd -S $mayChu -E -C -I -b -d $csdl -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM AspNetUsers WHERE UserName IN (N'dev.manager', N'dev.cashier', N'dev.customer')") 2>$null | Select-Object -First 1
        if ($taiKhoan -eq '3') { Ket 'OK' 'Tài khoản mẫu' 'đủ 3 tài khoản (mật khẩu: tự tra ghi chú lúc tạo, không lấy lại được)' }
        else { Ket 'CB' 'Tài khoản mẫu' "mới có $taiKhoan/3; chạy lệnh có --seed --seed-accounts (mật khẩu hiện MỘT lần)" }
        $mon = (& sqlcmd -S $mayChu -E -C -I -b -d $csdl -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM ProductVariant WHERE IsActive = 1 AND VariantType = 'Drink'") 2>$null | Select-Object -First 1
        if ([int]$mon -ge 1) { Ket 'OK' 'Món đang bán' "$mon biến thể đồ uống" } else { Ket 'CB' 'Món đang bán' 'chưa có món nào; chạy --seed hoặc tạo món bằng D06 (KT04)' }
    } else { Ket 'CB' "CSDL $csdl" 'chưa có; chạy lệnh --migrate --seed --seed-accounts ở docs/huong-dan-demo.md mục 1' }
} else { Ket 'CB' 'CSDL Dev' 'bỏ qua vì không có sqlcmd' }

# 7. Máy chủ Web đang chạy chưa (không bắt buộc)
try {
    $tl = Invoke-WebRequest -Uri 'http://localhost:5055/api/menu' -UseBasicParsing -TimeoutSec 4
    Ket 'OK' 'Máy chủ Web http://localhost:5055' "đang chạy, /api/menu trả mã $($tl.StatusCode)"
} catch { Ket 'CB' 'Máy chủ Web http://localhost:5055' 'chưa chạy (bình thường nếu bạn chưa khởi động; lệnh ở docs/huong-dan-demo.md mục 1)' }

# 8. Biên dịch toàn solution
if (-not $BoQuaBuild) {
    $kq = & dotnet build CafeManager.sln --nologo -v q 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0) { Ket 'OK' 'Biên dịch CafeManager.sln' 'thành công' }
    else { Ket 'LOI' 'Biên dịch CafeManager.sln' ('thất bại; nếu thấy "Application Control policy" là Smart App Control chặn, xem docs/huong-dan-demo.md mục 1. ' + (($kq -split "`r?`n" | Select-String 'error' | Select-Object -First 2) -join ' | ')) }
} else { Ket 'CB' 'Biên dịch' 'bỏ qua theo yêu cầu (-BoQuaBuild)' }

Write-Host ''
if ($script:loi -gt 0) { Write-Host "KẾT QUẢ: $($script:loi) lỗi, $($script:canhBao) chú ý. Sửa các dòng LỖI trước khi kiểm thử." -ForegroundColor Red; exit 1 }
elseif ($script:canhBao -gt 0) { Write-Host "KẾT QUẢ: không có lỗi, $($script:canhBao) chú ý (đọc từng dòng CHÚ Ý)." -ForegroundColor Yellow; exit 0 }
else { Write-Host 'KẾT QUẢ: máy sẵn sàng.' -ForegroundColor Green; exit 0 }
