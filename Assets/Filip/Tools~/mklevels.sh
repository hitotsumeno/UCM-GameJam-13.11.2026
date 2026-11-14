#!/bin/bash
# Builds the three level files. Items are "col row char"; rows count from the top.
# Run from the project folder: bash Assets/Filip/Tools~/mklevels.sh
# After that the files can still be edited by hand in a text editor.
L=Assets/Resources/Levels
mkmap() { awk -v C="$1" -v R="$2" '{ g[$2 "," $1] = $3 } END { for (r = 0; r < R; r++) { line = ""; for (c = 0; c < C; c++) { k = r "," c; line = line ((k in g) ? g[k] : "."); } print line } }'; }   # reads "col row char" lines
row() { local r=$1 ch=$2; shift 2; for c in "$@"; do echo "$c $r $ch"; done; }   # same char at several columns of one row
col() { local c=$1 ch=$2; shift 2; for r in "$@"; do echo "$c $r $ch"; done; }   # same char at several rows of one column

# ---------------- LEVEL 1: DEBRIS. All the air is inside the rocks; meteors hunt you. ----------------
{
cat <<'EOF'
# name=DEBRIS
# radio=Fuel is gone and the tanks are nearly dry. The only air out here is frozen inside the rocks.
# nebula=calm
# backdrop=wreck
# clouds=0.9
# starcolor=0.8,0.9,1.0
# oxygen=55
# meteors=5-8
# movers=0.35
# stars=45,20
# hint1=LEFT MOUSE BUTTON: SHOOT. THE RECOIL MOVES YOU
# hint2=THERE ARE NO SPARE TANKS. BREAK ROCKS AND GRAB THE CRYSTALS: THEY ARE YOUR AIR
# hint3=BREAK ROCKS ONE AFTER ANOTHER: A COMBO GIVES MORE AIR
# hint4=A RED ARROW AT THE TOP MEANS A METEOR IS COMING AT YOU. DODGE IT OR SHOOT IT
# hint5=BIG ROCKS SPLIT. DRILL A TUNNEL THROUGH THE WALL
# hint6=SHOOT TOWARD THE SHIP TO BRAKE. DOCK SLOWER THAN SPEED 5
EOF
{
  echo "10 87 P"; echo "10 85 1"; echo "10 4 S"
  # Part A: first rocks right on the way, in lines for combos
  echo "10 80 2"; col 10 a 78 76 74; row 71 a 6 8 12 14; echo "10 69 3"
  row 66 a 7 9 11 13; row 64 A 5 15; col 10 a 63 61; echo "10 59 4"
  # Part B: a real field, rocks of every size; lanes of small ones for combos
  row 56 A 4 10 16; row 53 a 2 7 13 18; echo "6 50 B"; echo "14 50 B"; row 47 a 3 10 17
  row 44 A 2 8 12 18; col 5 a 42 40 38; col 15 a 42 40 38; echo "10 40 B"; row 36 a 1 8 12 19
  echo "10 33 5"
  # Part C: the wall, staggered rows across the whole level, then the ship
  row 29 A 0 5 10 15 19; row 27 a 1 10 15; row 25 B 3 8 13 18; row 23 a 5 10 15; row 21 A 1 6 11 16
  echo "4 17 o"; row 15 a 8 12; echo "10 12 6"
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
# hint1=RED MINES HUNT YOU, AND THEY ARE FAST. HIDE BEHIND A ROCK OR SHOOT THEM FROM AFAR
# hint2=EVERY SHOT KICKS YOU BACK. CHECK WHAT IS BEHIND YOU
# hint3=YELLOW MINES PATROL. WAIT FOR THE GAP, OR BLOW ONE UP AND SLIP THROUGH
# hint4=A RING GUARDS EACH TANK. BREAK THE RING ONE MINE AT A TIME. A TANK SEALS ONE LEAK
# hint5=THE BIG ORANGE MINE TAKES THREE HITS AND WIPES OUT EVERYTHING AROUND IT. SNIPE IT FROM HERE
# hint6=ONE SHOT INTO A WALL OF MINES SETS THE WHOLE WALL OFF. KEEP YOUR DISTANCE
EOF
{
  echo "12 81 P"; echo "12 79 1"; echo "12 4 S"
  # Part A: fast hunters, big rocks to hide behind, then hunters behind your back
  echo "7 76 B"; echo "17 76 B"; echo "12 73 m"; row 70 m 5 19; echo "12 68 2"; row 80 m 2 22
  # Part B: patrol gates, each row moving, then vertical patrols
  echo "12 65 3"; row 63 h 3 9 15 21; row 60 h 6 12 18; row 57 v 4 12 20; row 55 a 1 23
  # Part C: tanks inside turning rings, hunters between them, a rock for cover
  echo "12 51 4"; echo "6 47 R"; echo "18 41 R"; echo "14 48 m"; echo "10 40 m"; echo "12 44 A"
  # Part D: a nest of hunters asleep around a heavy mine: snipe it before they wake
  echo "12 37 5"; echo "12 29 M"; row 31 m 9 15; row 27 m 9 15; echo "12 32 m"; echo "12 26 m"; row 29 m 6 18
  # Part E: a chain wall, crossing patrols, the last hunters by the ship
  echo "12 23 6"; row 20 s 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22
  row 19 s 2 4 6 8 10 12 14 16 18 20 22
  row 16 h 4 10 16 22; row 13 h 2 8 14 20; col 5 v 11 8; col 19 v 11 8; row 9 m 8 16
} | mkmap 24 84
} > $L/level2.txt

# ---------------- LEVEL 3: EVENT HORIZON. Nothing flies straight, and the only air circles the holes. ----------------
{
cat <<'EOF'
# name=EVENT HORIZON
# radio=Black holes, everywhere. Nothing flies straight in here: not us, not our shots.
# tint=0.6,0.5,1.0
# clouds=0.5
# light=0.9,0.88,1.0
# starcolor=0.85,0.8,1.0
# stardensity=1.3
# oxygen=70
# meteors=off
# movers=0
# stars=35,15
# hint1=BLACK HOLES PULL YOU AND YOUR SHOTS. THE DARK MIDDLE IS DEATH
# hint2=THE ONLY AIR HERE IS IN THE ROCKS CIRCLING THE HOLES. YOUR SHOTS CURVE: AIM TO THE SIDE
# hint3=SMALL HOLES DRIFT. WATCH WHERE THEY ARE GOING
# hint4=FLY PAST A HOLE AND IT TURNS YOU FOR FREE. GRAB CRYSTALS FAST: HOLES EAT THEM. MINES TOO
# hint5=THE LAST GATE: STRAIGHT BETWEEN THE TWO HOLES, THEN BRAKE
EOF
{
  echo "11 77 P"; echo "11 75 1"; echo "11 4 S"
  # Room 1: one small hole right on the way, two rocks circling it
  echo "11 71 2"; echo "11 67 c"
  # Room 2: a gate of two small holes with a third one drifting across it
  echo "11 61 3"; echo "5 57 c"; echo "17 57 c"; echo "11 53 x"
  # Room 3: a big hole in the middle, its pull covers the whole width; a tank close to it; two hunters to lure in
  echo "11 50 4"; echo "11 42 O"; echo "11 47 o"; echo "3 40 m"; echo "19 44 m"
  # Room 4: a slalom between two big holes, a small one drifting up and down between them
  echo "5 29 O"; echo "17 21 O"; echo "11 25 y"
  # Room 5: the last gate and the ship behind it
  echo "11 16 5"; echo "11 15 o"; echo "4 11 O"; echo "18 11 O"
} | mkmap 22 80
} > $L/level3.txt

for f in $L/level[123].txt; do echo "$f: widths=$(grep -v '^#' $f | awk '{ print length($0) }' | sort -u | tr '\n' ' ') rows=$(grep -vc '^#' $f) objects=$(grep -v '^#' $f | tr -d '.\n' | wc -c)"; done
