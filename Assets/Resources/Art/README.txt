Art for the draft game (Assets/Filip). Put PNG files here.

The game finds pictures by file name. If a file is missing, a placeholder is drawn.
Size in the game is set by code (the larger side of the picture), not by the picture:

  ship.png           1 unit    the PLAYER, drawn nose to the RIGHT. Not drawn yet: the game shows a white circle with a gun
  station.png        6 units   the goal of a level, round
  asteroid_s1.png    2 units   variants: asteroid_s2.png, asteroid_s3.png ...
  asteroid_m1.png    4 units   variants: asteroid_m2.png, asteroid_m3.png ...
  asteroid_l1.png    6 units   variants: asteroid_l2.png, asteroid_l3.png ...
  oxygen.png         0.8 unit
  planet1.png        8 units
  background.png     square 4096x4096 px, shown as 48x48 units
  star1.png          small, about 0.1-0.5 unit; variants: star2.png, star3.png, star4.png ...
                     (each star on screen picks a random variant)

1 unit = 100 pixels. Transparent background (PNG with alpha), except background.png.
Import settings (Sprite, 100 pixels per unit) are applied automatically.

Already here (made for the draft, replace freely):
  nebula_far.png, nebula_near.png   generated seamless space clouds, two parallax layers.
                                    If background.png is added, it replaces both.
  star1.png ... star5.png           star shapes drawn from the artist's stars.fbx; the shape takes the middle 42% of the picture
                                    (the game is 2D, so the 3D models were turned into pictures).
  star_glow1.png ... star_glow5.png soft halo for each star, same canvas; tinted in the game.
  asteroid_l1..4, asteroid_m1..4,   the artist's 3D rocks from stone.fbx, rendered into pictures from
  asteroid_s1..4 .png               different sides (5 models -> 12 different-looking asteroids).
                                    The rock fills the picture so that a circle of 0.45 x picture size
                                    has the same area as the rock: that circle is used for collisions.
  station.png                       placeholder drawn by code (Assets/Filip/Tools~/ship.ps1): a round station.
