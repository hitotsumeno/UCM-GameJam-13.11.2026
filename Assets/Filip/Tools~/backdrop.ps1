param([string]$Out)
# Far-away objects for level backgrounds: backdrop_sun.png, backdrop_planet.png
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\NebulaGen.cs", "$PSScriptRoot\BackdropGen.cs"
[BackdropGen]::Sun("$Out\backdrop_sun.png")
# backdrop_wreck.png is made by station.ps1 (a 3D render), not here.
[BackdropGen]::Planet("$Out\backdrop_planet.png")
# The planet the player flies around on level 4 (not scenery):
[BackdropGen]::GamePlanet("$Outplanet1.png")
"backdrops done"
