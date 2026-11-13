Tools that made the pictures in Assets/Resources/Art. Unity ignores this folder (name ends with ~).
Run from PowerShell on Windows:

  gen.ps1  -Dir <output folder> -Res 1024 -Final      -> nebula_far.png, nebula_near.png
           (without -Final it also writes screen_*.png: what the game screen would show)
  bake.ps1 -In <stars.fbx> -Out <output folder>       -> star1.png + star_glow1.png ... per model in the FBX

The look of the clouds (colours, density) is set by the numbers in NebulaGen.cs.
