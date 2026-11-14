param([string]$Out)
# Far-away objects for level backgrounds: backdrop_sun.png, backdrop_wreck.png, backdrop_planet.png
Add-Type -ReferencedAssemblies System.Drawing -Path "$PSScriptRoot\NebulaGen.cs", "$PSScriptRoot\BackdropGen.cs"
[BackdropGen]::Sun("$Out\backdrop_sun.png")
[BackdropGen]::Wreck("$Out\backdrop_wreck.png")
[BackdropGen]::Planet("$Out\backdrop_planet.png")
"backdrops done"
