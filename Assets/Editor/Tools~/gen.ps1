param([string]$Dir, [string]$Theme = "debris", [int]$Res = 1024, [switch]$Final)
# Cloud pictures for one level theme: debris (level 2), calm (level 1), alert (level 3), planet (level 4).
#   gen.ps1 -Dir <Assets\Resources\Art> -Theme calm -Final   -> nebula_calm_far.png, nebula_calm_near.png
# The "debris" theme writes nebula_far.png and nebula_near.png (the default set).
# Without -Final it writes test frames screen_<theme>_*.png instead: what the game screen would show.
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\NebulaGen.cs"
[NebulaGen]::SetTheme($Theme)
$far = [NebulaGen]::Far($Res, [NebulaGen]::Seed)
$near = [NebulaGen]::Near($Res, [NebulaGen]::Seed)
$name = if ($Theme -eq "debris") { "nebula" } else { "nebula_$Theme" }
if ($Final) {
    [NebulaGen]::Save($far, $Res, "$Dir\${name}_far.png")
    [NebulaGen]::Save($near, $Res, "$Dir\${name}_near.png")
} else {
    # As in the game: camera size 10, far clouds darkened to 0.6, near clouds 0.4 opaque.
    [NebulaGen]::MockPpu = 31.5; [NebulaGen]::MockFarMul = 0.6; [NebulaGen]::MockNearMul = 0.4
    [NebulaGen]::Screen($far, $Res, 64, $near, $Res, 80, 0, 0, 0.25, 0.40, "$Dir\screen_${Theme}_a.png")
    [NebulaGen]::Screen($far, $Res, 64, $near, $Res, 80, 30, 60, 0.25, 0.40, "$Dir\screen_${Theme}_b.png")
}
"$Theme done"
