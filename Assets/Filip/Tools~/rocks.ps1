param([string]$In, [string]$Out, [string]$SheetBg, [string]$SheetOut)
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\FbxDump.cs", "$PSScriptRoot\RockBake.cs"
# picture name, model in the FBX, view angles (degrees), picture size in pixels
$jobs = @(
  @('asteroid_l1', 'm1',     0,   0,   0, 512), @('asteroid_l2', 'm2',    30, -70,  60, 512),
  @('asteroid_l3', 'm1',    70,  35,   0, 512), @('asteroid_l4', 'm2',   -60, 140,  20, 512),
  @('asteroid_m1', 'm3.001', 0,   0,   0, 384), @('asteroid_m2', 'm3',     0,  90,   0, 384),
  @('asteroid_m3', 'm3.001',80,  40,  30, 384), @('asteroid_m4', 'm2',    40, -60, 150, 384),
  @('asteroid_s1', 'm3.002', 0,  90,   0, 256), @('asteroid_s2', 'm3.002',60,  20,  45, 256),
  @('asteroid_s3', 'm3',    50, 200,  10, 256), @('asteroid_s4', 'm3.001',-40, 110, 70, 256)
)
foreach ($j in $jobs) { [RockBake]::Bake($In, $j[1], $j[2], $j[3], $j[4], $j[5], "$Out\$($j[0]).png") }
if ($SheetBg) {
  $files = $jobs | ForEach-Object { "$Out\$($_[0]).png" }
  $sizes = $jobs | ForEach-Object { if ($_[0] -like '*_l*') { 160 } elseif ($_[0] -like '*_m*') { 80 } else { 40 } }
  [RockBake]::Sheet($SheetBg, [string[]]$files, [int[]]$sizes, $SheetOut)
}
