param([string]$Out, [string]$PreviewBg, [string]$PreviewOut)
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\ShipGen.cs"
[ShipGen]::Rocket("$Out\ship.png")
[ShipGen]::Gun("$Out\ship_gun.png")
[ShipGen]::Station("$Out\station.png")
if ($PreviewBg) { [ShipGen]::Preview($Out, $PreviewBg, $PreviewOut) }
"done"
