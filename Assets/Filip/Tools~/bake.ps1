param([string]$In, [string]$Out, [string]$PreviewBg, [string]$PreviewOut)
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition (Get-Content -Raw "$PSScriptRoot\FbxDump.cs")
[FbxDump]::BakeSprites($In, $Out, 256)
if ($PreviewBg) { [FbxDump]::Preview($Out, $PreviewBg, $PreviewOut, 40) }
