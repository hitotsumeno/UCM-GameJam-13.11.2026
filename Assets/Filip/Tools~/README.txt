Tools that made the pictures in Assets/Resources/Art. Unity ignores this folder (name ends with ~).
Run from PowerShell on Windows:

  gen.ps1  -Dir <output folder> -Res 1024 -Final      -> nebula_far.png, nebula_near.png
           (without -Final it also writes screen_*.png: what the game screen would show)
  bake.ps1 -In <stars.fbx> -Out <output folder>       -> star1.png + star_glow1.png ... per model in the FBX

The look of the clouds (colours, density) is set by the numbers in NebulaGen.cs.

  rocks.ps1 -In <stone.fbx> -Out <output folder>      -> asteroid_l1..4, asteroid_m1..4, asteroid_s1..4 .png
           Renders the artist's 3D rocks into shaded 2D pictures. The list at the top of rocks.ps1
           says which model and which view angle becomes which picture; edit it to add variants.
           Light, rim colour and rock relief are set by the numbers in RockBake.cs.

  ship.ps1 -Out <output folder>                       -> ship.png, ship_gun.png, station.png
           Placeholder rocket, its side gun and the station, drawn with simple shapes (ShipGen.cs).
