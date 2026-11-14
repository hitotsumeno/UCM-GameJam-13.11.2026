param([string]$Out)
# The wreck of a ring station for the background of level 2: backdrop_wreck.png
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\FbxDump.cs", "$PSScriptRoot\RockBake.cs", "$PSScriptRoot\StationBake.cs"
[StationBake]::Bake("$Out\backdrop_wreck.png", 1024)
