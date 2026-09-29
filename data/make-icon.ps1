# Erzeugt data\app.ico (16-256 px, PNG-komprimiert). Aufruf: powershell -File data\make-icon.ps1
Add-Type -AssemblyName System.Drawing

function New-JoyCon([double]$x, [double]$y, [double]$w, [double]$h, [double]$ro, [double]$ri, [bool]$isLeft) {
    # Links: aussen (links) gross gerundet, innen (rechts) fast eckig. Rechts gespiegelt.
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    if ($isLeft) { $tl = $ro; $tr = $ri; $br = $ri; $bl = $ro } else { $tl = $ri; $tr = $ro; $br = $ro; $bl = $ri }
    $p.AddArc([single]$x, [single]$y, [single](2 * $tl), [single](2 * $tl), 180, 90)
    $p.AddArc([single]($x + $w - 2 * $tr), [single]$y, [single](2 * $tr), [single](2 * $tr), 270, 90)
    $p.AddArc([single]($x + $w - 2 * $br), [single]($y + $h - 2 * $br), [single](2 * $br), [single](2 * $br), 0, 90)
    $p.AddArc([single]$x, [single]($y + $h - 2 * $bl), [single](2 * $bl), [single](2 * $bl), 90, 90)
    $p.CloseFigure()
    return $p
}

function New-IconPng([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Zwei weisse Joy-Cons, ohne Hintergrund. Aussenseite stark gerundet, Innenseite gerade.
    $k = $s / 32.0
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 247, 250))
    $edge = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 150, 158, 168)), ([single][Math]::Max(1, 0.55 * $k))
    $dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 36, 41, 47))

    # Masse aus Geometrie berechnen, damit Sticks/Knoepfe mittig sitzen
    $w = 10.4 * $k; $h = 26 * $k; $top = 3 * $k; $ro = 5.2 * $k; $ri = 1.3 * $k
    $gap = 1.6 * $k
    $lx = (32 * $k - 2 * $w - $gap) / 2      # linke Joy-Con-Kante, Paar zentriert
    $rx = $lx + $w + $gap
    $lc = $lx + $w / 2; $rc = $rx + $w / 2   # Joy-Con-Mitten (x)
    $yUp = $top + $h * 0.30; $yLo = $top + $h * 0.70

    $left = New-JoyCon $lx $top $w $h $ro $ri $true
    $right = New-JoyCon $rx $top $w $h $ro $ri $false
    foreach ($c in @($left, $right)) { $g.FillPath($white, $c); $g.DrawPath($edge, $c) }

    # Sticks: links oben, rechts unten (bei jeder Groesse sichtbar)
    $st = [single](4.6 * $k)
    $g.FillEllipse($dark, [single]($lc - $st / 2), [single]($yUp - $st / 2), $st, $st)
    $g.FillEllipse($dark, [single]($rc - $st / 2), [single]($yLo - $st / 2), $st, $st)

    if ($s -ge 32) {
        # Je vier runde Knoepfe im Kreuz: links unten (Steuerkreuz), rechts oben (ABXY)
        $b = [single](2.1 * $k); $o = 2.4 * $k
        foreach ($ctr in @(@($lc, $yLo), @($rc, $yUp))) {
            foreach ($dd in @(@(0, -1), @(0, 1), @(-1, 0), @(1, 0))) {
                $g.FillEllipse($dark, [single]($ctr[0] + $dd[0] * $o - $b / 2), [single]($ctr[1] + $dd[1] * $o - $b / 2), $b, $b)
            }
        }
    }

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    return ,$ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = foreach ($s in $sizes) { ,(New-IconPng $s) }

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $images[$i].Length
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$len); $bw.Write([uint32]$offset)
    $offset += $len
}
foreach ($img in $images) { $bw.Write($img) }
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'app.ico'), $out.ToArray())
Write-Output "app.ico erstellt"
