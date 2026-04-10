param(
    [string]$unityEditorPath = $env:UNITY_PATH,
    [string]$projectPath = (Get-Location).Path,
    [string]$resultsDir = "$((Get-Location).Path)\TestResults"
)

if (-not $unityEditorPath) {
    Write-Error "Không tìm thấy đường dẫn Unity Editor. Vui lòng truyền tham số -unityEditorPath hoặc đặt biến môi trường UNITY_PATH."
    exit 1
}

if (-not (Test-Path $unityEditorPath)) {
    Write-Error "Unity Editor không tồn tại: $unityEditorPath"
    exit 1
}

if (-not (Test-Path $projectPath)) {
    Write-Error "Không tìm thấy thư mục project: $projectPath"
    exit 1
}

New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

$targets = @{
    StandaloneWindows64 = "playmode_pc.xml"
    Android = "playmode_mobile_android.xml"
}

$processes = @()

foreach ($target in $targets.GetEnumerator()) {
    $outFile = Join-Path $resultsDir $target.Value
    $logFile = Join-Path $resultsDir "$($target.Key.ToLower())_unity.log"

    $args = @(
        "-projectPath", $projectPath,
        "-batchmode",
        "-nographics",
        "-silent-crashes",
        "-quit",
        "-runTests",
        "-testPlatform", "PlayMode",
        "-buildTarget", $target.Key,
        "-testResults", $outFile,
        "-logFile", $logFile
    )

    Write-Host "Đang chạy kiểm thử PlayMode trên target: $($target.Key)"
    $processes += Start-Process -FilePath $unityEditorPath -ArgumentList $args -PassThru -NoNewWindow
}

Write-Host "Chờ tất cả tiến trình kiểm thử hoàn thành..."
Wait-Process -InputObject $processes

$failed = $false
foreach ($p in $processes) {
    if ($p.ExitCode -ne 0) {
        Write-Error "Unity process cho target $($p.StartInfo.Arguments) trả về exit code $($p.ExitCode)."
        $failed = $true
    }
}

if ($failed) {
    Write-Error "Một hoặc nhiều quá trình kiểm thử song song đã thất bại. Xem file log trong $resultsDir."
    exit 1
}

Write-Host "Kiểm thử song song hoàn thành thành công. Kết quả được lưu trong: $resultsDir"
exit 0
