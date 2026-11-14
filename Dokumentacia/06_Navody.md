# 6. Návody

## Spustiť hru v editore

1. Otvoriť projekt v Unity 6 (6000.0.85f2).
2. Otvoriť scénu `Assets/_Scenes/DraftScene.unity`.
3. Stlačiť **Play**. Hra začína prvou úrovňou (dá sa zmeniť na objekte DraftGame,
   pole Start Level).

## Zmeniť čísla balansu

1. Menu **Filip > Select Tuning Asset** (alebo kliknúť na `Assets/_Settings/Tuning.asset`).
2. V Inspectore zmeniť hodnotu. Čísla sú rozdelené do skupín (Shooting, Player, Oxygen,
   Asteroids, Meteors, Mines, Black holes, Crystals, Combo …) a pri väčšine je vysvetlivka.
3. Zmena platí hneď, aj počas hry. Pozor: zmena urobená počas Play zostane uložená,
   pretože `Tuning.asset` je súbor projektu.

Najčastejšie úpravy:

| Chcem | Pole |
|---|---|
| silnejší / slabší spätný ráz | `RecoilImpulse` (3) |
| rýchlejšia / pomalšia streľba | `FireRate` (4 za sekundu) |
| pomalší úbytok vzduchu | `OxygenDrainPerSecond` (2), `OxygenPerShot` (1) |
| viac vzduchu z kryštálov | `CrystalOxygen` (2,8) |
| pomalšie meteory | `MeteorSpeedMin` (13), `MeteorSpeedMax` (17) |
| menej agresívni lovci | `MineSpeed` (5,5), `MineAccel` (12), `MineWakeRadius` (16) |
| slabšie čierne diery | `HoleBigStrength` (520), `HoleSmallStrength` (200) |
| ľahšie pristátie | `DockMaxSpeed` (5) |
| menej trestajúce nárazy | `BreachImpactSpeed` (7), `BreachDrainPerSecond` (1) |

Niektoré čísla sa použijú iba pri výrobe prefabov (veľkosti objektov, počty hviezd, žiara).
Po ich zmene treba spustiť **Filip > Rebuild Prefabs And Scene**.

## Upraviť úroveň

**Spôsob A: priamo v Unity.** Otvoriť prefab `Assets/Prefabs/Levels/LevelN`
a posúvať, pridávať alebo mazať objekty (kamene, míny, fľaše, diery). Hranice úrovne
sú na koreni prefabu v komponente `Level` (pole Bounds) a v editore ich vidno ako červený
obdĺžnik. Pozor: menu Rebuild Prefabs And Scene tieto ručné úpravy prepíše.

**Spôsob B: v textovom súbore.** Upraviť `Assets/_Resources/Levels/levelN.txt`
(formát je v kapitole 3) a spustiť **Filip > Rebuild Prefabs And Scene**.

**Pridať novú úroveň:** vytvoriť `level4.txt` (čísla za sebou bez medzery)
a spustiť Rebuild Prefabs And Scene. Nová úroveň sa automaticky pridá na koniec zoznamu
v objekte DraftGame.

## Vymeniť obrázky

Hra hľadá obrázky v `Assets/_Resources/Art` podľa mena. Ak obrázok chýba, nakreslí
sa zástupný tvar. Veľkosť v hre určuje kód (dlhšia strana obrázka), nie obrázok.
1 jednotka = 100 pixelov, PNG s priehľadným pozadím. Nastavenia importu sa doplnia samy.

| Súbor | Veľkosť v hre | Poznámka |
|---|---|---|
| `astronaut.png` | 1 jednotka | loď hráča, nakreslená **smerom doprava** |
| `ship.png` | 6 jednotiek | cieľová loď, nosom **doprava** |
| `asteroid_s1.png` … | 2 | malé kamene; varianty `asteroid_s2`, `asteroid_s3` … |
| `asteroid_m1.png` … | 4 | stredné kamene |
| `asteroid_l1.png` … | 6 | veľké kamene |
| `oxygen.png` | 0,8 | kyslíková fľaša |
| `crystal.png` | 0,35 | kryštál kyslíka |
| `mine.png` | 1,1 | mína, okrúhla; farebnú lampu v strede dokreslí hra |
| `blackhole.png` | štvorec | tmavý disk má zaberať 32 % šírky obrázka, v strede |
| `planet1.png` | 12 | guľa má zaberať stredných 88 % obrázka (zvyšok je žiara) |
| `background.png` | 48 × 48 | štvorec 4096 × 4096 px; ak existuje, nahradí oblaky |
| `star1.png` … | 0,1 – 0,5 | tvary hviezd, každá hviezda si vyberie náhodný variant |

Po pridaní alebo výmene obrázka treba spustiť **Filip > Rebuild Prefabs And Scene**:
prefaby si obrázky berú iba vtedy.

## Nové kamene alebo hviezdy z 3D modelu

1. Nahradiť `Assets/Art/Source/stone.fbx` alebo `stars.fbx` novou verziou (rovnaké meno).
2. Menu **Filip > Bake Rocks And Stars From FBX**: prepíše obrázky `asteroid_*`,
   `star*` a `star_glow*`. Ktorý model a z akého uhla sa kreslí, určuje zoznam na začiatku
   `Assets/Editor/ArtBake.cs`.
3. Menu **Filip > Rebuild Prefabs And Scene**.

## Zmeniť texty

Všetky slová hry sú v `Assets/_Scripts/Texts.cs`, názvy úrovní v súboroch úrovní
(`# name=`). Pixelové písmo pozná iba latinské písmená (malé sa ukážu ako veľké),
číslice a znaky `- . : / + !`. Slovenské písmená s diakritikou ani iné znaky sa neukážu.

## Skúšobný build pre Windows

1. Zastaviť hru (Play vypnutý).
2. Menu **Filip > Test Build (Windows)**.
3. Hotový `DraftGame.exe` sa uloží do `D:\ProjectGameJam\DraftBuild` (mimo repozitára)
   a sám sa spustí. Výsledok sa vypíše do konzoly Unity: „DRAFT TEST BUILD OK“ alebo „FAILED“.

Build dostane scénu priamo, takže sa nemenia nastavenia projektu (Build Settings).
Build trvá niekoľko minút.

## Z príkazového riadku (keď je Unity zatvorená)

```
"C:\Program Files\Unity\Hub\Editor\6000.0.85f2\Editor\Unity.exe" -batchmode -quit -projectPath <priečinok projektu> -executeMethod DraftSetup.RebuildAll
```

Podobne `-executeMethod ArtBake.BakeAll` znova zapečie kamene a hviezdy.
Chyby kompilácie sú v logu editora: `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
