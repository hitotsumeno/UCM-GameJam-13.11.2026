param([string]$Dir, [int]$Res = 1024, [int]$Seed = 7, [switch]$Final)
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition (Get-Content -Raw "$PSScriptRoot\NebulaGen.cs")
$far = [NebulaGen]::Far($Res, $Seed)
$near = [NebulaGen]::Near($Res, $Seed)
[NebulaGen]::Save($far, $Res, "$Dir\nebula_far.png")
[NebulaGen]::Save($near, $Res, "$Dir\nebula_near.png")
if (-not $Final) {
    # Three moments of a flight straight up: what the screen shows at y = 0, 40, 80.
    [NebulaGen]::Screen($far, $Res, 64, $near, $Res, 80, 0, 0, 0.25, 0.55, "$Dir\screen_0.png")
    [NebulaGen]::Screen($far, $Res, 64, $near, $Res, 80, 0, 40, 0.25, 0.55, "$Dir\screen_40.png")
    [NebulaGen]::Screen($far, $Res, 64, $near, $Res, 80, 0, 80, 0.25, 0.55, "$Dir\screen_80.png")
}
Get-ChildItem $Dir -Filter *.png | Select-Object Name, Length
