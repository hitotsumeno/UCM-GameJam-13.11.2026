param([string]$In, [string]$Out)
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition (Get-Content -Raw "$PSScriptRoot\FbxDump.cs")
[FbxDump]::BakeSprites($In, $Out, 256)
