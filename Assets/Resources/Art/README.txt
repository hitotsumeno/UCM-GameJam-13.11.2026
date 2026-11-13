Art for the draft game (Assets/Filip). Put PNG files here.

The game finds pictures by file name. If a file is missing, a placeholder is drawn.
Size in the game is set by code (the larger side of the picture), not by the picture:

  astronaut.png      1 unit    drawn facing RIGHT
  ship.png           6 units   drawn nose to the RIGHT
  asteroid_s1.png    1 unit    variants: asteroid_s2.png, asteroid_s3.png ...
  asteroid_m1.png    2 units   variants: asteroid_m2.png, asteroid_m3.png ...
  asteroid_l1.png    4 units   variants: asteroid_l2.png, asteroid_l3.png ...
  oxygen.png         0.6 unit
  planet1.png        8 units
  background.png     square 4096x4096 px, shown as 48x48 units

1 unit = 100 pixels. Transparent background (PNG with alpha), except background.png.
Import settings (Sprite, 100 pixels per unit) are applied automatically.
