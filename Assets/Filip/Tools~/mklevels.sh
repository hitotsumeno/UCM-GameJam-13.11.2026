#!/bin/bash
# Builds the three level files. Items are "col row char"; rows count from the top.
# Run from the project folder: bash Assets/Filip/Tools~/mklevels.sh
L=Assets/Resources/Levels
mkmap() { awk -v C="$1" -v R="$2" '{ g[$2 "," $1] = $3 } END { for (r = 0; r < R; r++) { line = ""; for (c = 0; c < C; c++) { k = r "," c; line = line ((k in g) ? g[k] : "."); } print line } }'; }   # reads "col row char" lines
row() { local r=$1 ch=$2; shift 2; for c in "$@"; do echo "$c $r $ch"; done; }   # same char at several columns of one row
col() { local c=$1 ch=$2; shift 2; for r in "$@"; do echo "$c $r $ch"; done; }   # same char at several rows of one column

# ---------------- LEVEL 1: DEBRIS. All the air is inside the rocks. ----------------
{
cat <<'EOF'
# name=DEBRIS
# radio=Fuel is gone and the tanks are nearly dry. The only air out here is frozen inside the rocks.
# nebula=calm
# backdrop=wreck
# clouds=0.9
# starcolor=0.8,0.9,1.0
# oxygen=55
# meteors=9-13
# movers=0.35
# stars=45,20
# hint1=LEFT MOUSE BUTTON: SHOOT. THE RECOIL MOVES YOU
# hint2=THERE ARE NO SPARE TANKS. BREAK ROCKS AND GRAB THE CRYSTALS: THEY ARE YOUR AIR
# hint3=BREAK ROCKS ONE AFTER ANOTHER: A COMBO GIVES MORE AIR
# hint4=BIG ROCKS SPLIT. DRILL A TUNNEL THROUGH THE WALL
# hint5=SHOOT TOWARD THE SHIP TO BRAKE. DOCK SLOWER THAN SPEED 5
EOF
{
  echo "10 87 P"; echo "10 85 1"; echo "10 4 S"
  # Part A: first rocks right on the way, in lines for combos
  echo "10 80 2"; col 10 a 78 76 74; row 71 a 6 8 12 14; echo "10 69 3"
  row 66 a 7 9 11 13; row 64 A 5 15; col 10 a 63 61; echo "10 59 C"
  # Part B: a real field, rocks of every size; lanes of small ones for combos
  row 56 A 4 10 16; row 53 a 2 7 13 18; echo "6 50 B"; echo "14 50 B"; row 47 a 3 10 17
  row 44 A 2 8 12 18; col 5 a 42 40 38; col 15 a 42 40 38; echo "10 40 B"; row 36 a 1 8 12 19
  echo "10 33 C"; echo "10 31 4"
  # Part C: the wall, three staggered rows across the whole level, then the ship
  row 29 A 0 5 10 15 19; row 27 a 1 10 15; row 25 B 3 8 13 18; row 23 a 5 10 15; row 21 A 1 6 11 16
  echo "4 17 o"; row 15 a 8 12; echo "10 12 5"
} | mkmap 20 90
} > $L/level1.txt

# ---------------- LEVEL 2: MINEFIELD. Every shot throws you back, and mines are behind you too. ----------------
{
cat <<'EOF'
# name=MINEFIELD
# radio=Old defense mines. They are all around us. Every shot we fire kicks us back, so look behind.
# nebula=alert
# clouds=0.8
# light=1.0,0.82,0.80
# starcolor=1.0,0.8,0.75
# stardensity=0.8
# oxygen=70
# meteors=off
# movers=0.2
# stars=40,15
# hint1=RED MINES HUNT YOU. A BLAST NEXT TO YOU BREACHES THE HULL: SHOOT THEM FROM AFAR
# hint2=EVERY SHOT KICKS YOU BACK. CHECK WHAT IS BEHIND YOU
# hint3=YELLOW MINES PATROL. WAIT FOR THE GAP, OR BLOW ONE UP AND SLIP THROUGH
# hint4=A RING GUARDS EACH TANK. A TANK SEALS ONE LEAK
# hint5=ONE SHOT INTO A WALL OF MINES SETS THE WHOLE WALL OFF. KEEP YOUR DISTANCE
EOF
{
  echo "12 87 P"; echo "12 85 1"; echo "12 4 S"
  # Part A: hunters ahead AND behind the first stretch, then patrol lines
  echo "12 80 m"; row 77 m 6 18; echo "12 75 2"; row 84 m 3 21; row 72 m 4 12 20
  echo "12 69 3"; row 67 h 3 9 15 21; row 64 h 6 12 18; row 62 a 2 22; echo "12 60 C"
  # Part B: rings with tanks inside, hunters between them, vertical patrols in the lanes
  echo "12 57 4"; echo "6 54 R"; echo "18 49 R"; echo "9 43 R"
  col 12 v 53 47; col 2 v 50 44; col 22 v 55 43; row 51 m 14 22; row 46 m 3 14; row 40 m 6 12 18
  echo "12 37 B"; echo "12 34 C"
  # Part C: a sleeping nest, a chain wall behind it, crossing patrols before the ship
  echo "12 32 5"; row 26 m 9 11 13 15; row 25 m 10 12 14; row 24 m 9 11 13 15
  row 20 s 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22
  row 19 s 2 4 6 8 10 12 14 16 18 20 22
  row 15 h 4 10 16 22; row 12 h 2 8 14 20; col 5 v 10 7; col 19 v 10 7; row 9 m 8 16
  echo "20 30 R"
} | mkmap 24 90
} > $L/level2.txt

# ---------------- LEVEL 3: BLACK HOLES. Nothing flies straight. ----------------
{
cat <<'EOF'
# name=EVENT HORIZON
# radio=Black holes, everywhere. Nothing flies straight in here: not us, not our shots.
# tint=0.6,0.5,1.0
# clouds=0.5
# light=0.9,0.88,1.0
# starcolor=0.85,0.8,1.0
# stardensity=1.3
# oxygen=80
# meteors=off
# movers=0.2
# stars=35,15
# hint1=BLACK HOLES PULL YOU AND YOUR SHOTS. THE DARK MIDDLE IS DEATH
# hint2=YOUR BULLETS CURVE NEAR A HOLE. AIM TO THE SIDE OF THE ROCK
# hint3=FLY PAST A HOLE AND IT TURNS YOU FOR FREE. GRAB CRYSTALS FAST: HOLES EAT THEM
# hint4=SMALL HOLES DRIFT. WATCH WHERE THEY ARE GOING
# hint5=THE LAST GATE: STRAIGHT BETWEEN THE TWO HOLES, THEN BRAKE
EOF
{
  echo "14 97 P"; echo "14 95 1"; echo "14 4 S"
  # Part A: one small hole to feel the pull, rocks behind it to learn curved shots, a gate of two
  echo "8 90 c"; row 88 a 11 17; echo "14 87 2"; row 85 a 4 6 22 24; echo "20 82 c"; row 80 A 9 14
  echo "7 76 c"; echo "21 76 c"; row 76 a 14; row 73 a 12 16; echo "14 71 C"
  # Part B: a big hole in the middle with circling rocks, drifting holes across the way, a tank close to it
  echo "14 69 3"; echo "14 60 O"; echo "14 65 o"; row 66 a 4 24; row 55 A 3 25; echo "14 51 4"
  row 49 x 7; row 45 x 21; row 47 a 12 16; col 3 a 50 46 42; col 25 a 50 46 42; echo "14 41 A"; echo "17 39 C"
  # Part C: a slalom between big holes, drifting holes up and down, the last gate, the ship
  echo "7 33 O"; echo "21 26 O"; echo "14 30 a"; row 29 a 12 16; echo "3 24 y"; echo "25 34 y"
  row 22 A 6 12; echo "23 20 o"; echo "14 18 5"; echo "8 12 O"; echo "20 12 O"; row 15 a 13 15; row 9 a 14
} | mkmap 28 100
} > $L/level3.txt
rm -f $L/level4.txt $L/level4.txt.meta
for f in $L/level[123].txt; do echo "$f: widths=$(grep -v '^#' $f | awk '{ print length($0) }' | sort -u | tr '\n' ' ') rows=$(grep -vc '^#' $f)"; done
