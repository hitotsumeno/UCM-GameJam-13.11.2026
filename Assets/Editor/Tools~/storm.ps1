# Makes storm_body.png, storm_glow.png and storm_bolt1..3.png (the storm at the level edge).
#   storm.ps1 -Out <project>\Assets\_Resources\Art
param([string]$Out)
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\StormGen.cs"
[StormGen]::Make($Out)
