param(
    [string]$Exe = "D:\111\src\StarDream.App\bin\x64\Debug\net8.0-windows10.0.26100.0\StarDream.App.exe",
    [string]$Out = "D:\111\artifacts\capture.png",
    [int]$WaitSeconds = 7
)

$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinCap {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@

$p = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds $WaitSeconds
$proc = Get-Process -Id $p.Id
$proc.Refresh()
$handle = $proc.MainWindowHandle
if ($handle -eq [IntPtr]::Zero) { $proc.Kill(); throw "no main window" }

[WinCap]::ShowWindow($handle, 9) | Out-Null
[WinCap]::SetForegroundWindow($handle) | Out-Null
# Foreground changes are often refused for background processes; topmost works regardless.
$hwndTopmost = [IntPtr](-1)
$swpNoMoveNoSize = 0x0001 -bor 0x0002
[WinCap]::SetWindowPos($handle, $hwndTopmost, 0, 0, 0, 0, $swpNoMoveNoSize) | Out-Null
Start-Sleep -Seconds 2
$foreground = [WinCap]::GetForegroundWindow() -eq $handle

$rect = New-Object WinCap+RECT
[WinCap]::GetWindowRect($handle, [ref]$rect) | Out-Null
$width = $rect.R - $rect.L
$height = $rect.B - $rect.T
if ($width -le 0 -or $height -le 0) { $proc.Kill(); throw "bad window rect" }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.CopyFromScreen($screen.Location, [System.Drawing.Point]::Empty, $screen.Size)
$graphics.Dispose()

$crop = New-Object System.Drawing.Bitmap $width, $height
$cropGraphics = [System.Drawing.Graphics]::FromImage($crop)
$cropGraphics.DrawImage($bmp, (New-Object System.Drawing.Rectangle 0, 0, $width, $height), (New-Object System.Drawing.Rectangle $rect.L, $rect.T, $width, $height), [System.Drawing.GraphicsUnit]::Pixel)
$cropGraphics.Dispose()
$crop.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
$bmp.Dispose()

$proc.Kill()
"foreground=$foreground  size=${width}x${height}  saved=$Out"
