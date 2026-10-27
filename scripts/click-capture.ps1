param(
    [int]$ClickX = -1,
    [int]$ClickY = -1,
    [string]$Out = "D:\111\artifacts\click.png",
    [int]$WaitSeconds = 7
)

$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class StarryInput {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@

$exe = "D:\111\src\StarDream.App\bin\x64\Debug\net8.0-windows10.0.26100.0\StarDream.App.exe"
$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds $WaitSeconds
$p = Get-Process -Id $proc.Id
$p.Refresh()

[StarryInput]::SetWindowPos($p.MainWindowHandle, [IntPtr](-1), 0, 0, 0, 0, 0x0003) | Out-Null
Start-Sleep -Seconds 2

$rect = New-Object StarryInput+RECT
[StarryInput]::GetWindowRect($p.MainWindowHandle, [ref]$rect) | Out-Null

if ($ClickX -ge 0 -and $ClickY -ge 0) {
    [StarryInput]::SetCursorPos(($rect.L + $ClickX), ($rect.T + $ClickY)) | Out-Null
    Start-Sleep -Milliseconds 400
    [StarryInput]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 120
    [StarryInput]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Seconds 1
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$full = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
$graphics = [System.Drawing.Graphics]::FromImage($full)
$graphics.CopyFromScreen($screen.Location, [System.Drawing.Point]::Empty, $screen.Size)
$graphics.Dispose()

$width = $rect.R - $rect.L
$height = $rect.B - $rect.T
$crop = New-Object System.Drawing.Bitmap $width, $height
$cropGraphics = [System.Drawing.Graphics]::FromImage($crop)
$cropGraphics.DrawImage(
    $full,
    (New-Object System.Drawing.Rectangle 0, 0, $width, $height),
    (New-Object System.Drawing.Rectangle $rect.L, $rect.T, $width, $height),
    [System.Drawing.GraphicsUnit]::Pixel)
$cropGraphics.Dispose()
$crop.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
$full.Dispose()

$p.Kill()
Write-Host "saved $Out"
