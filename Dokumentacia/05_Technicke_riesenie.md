# 5. Technické riešenie

## Nástroje

- **Unity 6**: projekt je uložený vo verzii 6000.0.79f1, pracuje sa vo verzii 6000.0.85f2.
- **URP 2D** (Universal Render Pipeline pre 2D) s globálnym 2D svetlom (Global Light 2D).
  Sprity majú predvolený materiál.
- **C#**, vstup cez `UnityEngine.Input` (myš a klávesnica).
- **Fyzika 2D** (`Rigidbody2D`, API Unity 6: `linearVelocity`). Globálna gravitácia
  v nastaveniach zostala, ale všetky telesá hry ju majú vypnutú (`gravityScale = 0`).
  Ťah planéty a čiernych dier počíta kód.
- **Rozhranie:** uGUI, text cez vlastné pixelové písmo (`RawImage`).
- **Git a GitHub** na spoluprácu tímu.

## Hlavná myšlienka architektúry

- **Hra sa nestavia kódom pri spustení.** Všetko je pripravené vopred v scéne a v prefaboch,
  odkazy medzi objektmi sú nastavené v Inspectore (polia `[SerializeField]`).
  Túto štruktúru navrhol programátor vo vetve `main`.
- **Prefaby vyrába editorový skript** `Assets/Editor/DraftSetup.cs`
  (menu **Filip > Rebuild Prefabs And Scene**). Vyrobí všetky prefaby z kódu a zo súborov
  úrovní a pripraví scénu. Používa pevné náhodné semienko (1311), takže pri každom
  spustení vznikne rovnaké rozloženie. Potom sa dá všetko upravovať ručne ako v bežnom
  projekte Unity, no ďalšie spustenie menu ručné úpravy prepíše.
- **Počas hry sa len vytvárajú kópie prefabov** (`Instantiate`): úroveň, strely, kryštály,
  úlomky kameňov, meteory, výbuchy, trhliny a obláčiky vzduchu.
- **Reštart a zmena úrovne bez načítania scény:** aktuálna úroveň sa zničí a vytvorí sa
  nová kópia prefabu.
- **Statické polia sa pri štarte nulujú**, takže hra funguje aj s vypnutým reloadom domény.
- **Všetky čísla balansu sú v jednom assete** `Assets/_Settings/Tuning.asset`
  (trieda `Tuning`, ScriptableObject). Každý komponent má pole `tuning`, ktoré naň ukazuje.
- **Všetky texty hry sú v jednom súbore** `Assets/_Scripts/Texts.cs`.
- **Nepoužívajú sa nové tagy, vrstvy ani sorting layers** (sú v ProjectSettings,
  ktoré sa necommitujú). Poradie kreslenia určuje `sortingOrder`.

## Scéna `Assets/_Scenes/DraftScene.unity`

| Objekt | Úloha |
|---|---|
| Main Camera + `CameraRig` | kamera, ktorá plynulo sleduje loď a trasie sa |
| Global Light 2D | svetlo; každá úroveň mu dá vlastnú farbu |
| HUD (Canvas + `Hud`) | celé rozhranie, časti priradené v Inspectore |
| DraftGame (`DraftGame`) | riadi hru: zoznam prefabov úrovní, štartovacia úroveň, prefab strely a výbuchu |

## Priebeh hry (`DraftGame`)

- Stavy hry: **Playing** (hrá sa), **Won** (pristál), **Lost** (prehral).
- Pri štarte úrovne: vytvorí kópiu prefabu úrovne, nastaví farbu svetla, kyslík, rozhranie
  a kameru.
- Každý snímok: odpočíta kyslík (čas, mimo hraníc, trhliny), aktualizuje šípku k lodi
  a sériu.
- Udalosti od objektov: zničenie (`RegisterKill`), trhlina v trupe (`BreachHull`),
  zaplátanie (`RepairBreach`), prechod čiernou dierou (`ThrownThroughWormhole`),
  dotyk cieľovej lode (`PlayerReachedShip`), kryštál (`CollectCrystal`), výhra a prehra.
- Klávesy: R reštart, Enter ďalšia úroveň po výhre, Esc koniec hry.

## Priečinky projektu

| Priečinok | Obsah |
|---|---|
| `Assets/_Scenes` | `DraftScene` (táto hra), `s_StartMenu` (úvodné menu programátora), `LV1` a `SampleScene` (scény programátora) |
| `Assets/_Scripts` | skripty hry |
| `Assets/Editor` | editorové skripty (generovanie prefabov, pečenie grafiky, build) |
| `Assets/Editor/Tools~` | nástroje mimo Unity (Unity priečinky s `~` ignoruje) |
| `Assets/Prefabs` | prefaby objektov |
| `Assets/Prefabs/Levels` | prefaby úrovní `Level1` – `Level3` |
| `Assets/_Resources/Art` | obrázky hry |
| `Assets/_Resources/Levels` | textové súbory úrovní (zdroj pre prefaby) |
| `Assets/_Settings` | `Tuning.asset`, všetky čísla balansu |
| `Assets/Art/Shapes` | jednoduché tvary a ikony rozhrania |
| `Assets/Art/Source` | 3D modely výtvarníčky (`stone.fbx`, `stars.fbx`), do hry nejdú |
| `Assets/Physics` | fyzikálne materiály (`Bouncy` pre kamene a loď, `Mine` pre míny) |
| `Dokumentacia` | táto dokumentácia (mimo `Assets`, Unity ju nenačítava) |

## Skripty hry (`Assets/_Scripts`)

| Skript | Čo robí |
|---|---|
| `DraftGame.cs` | riadi hru: úrovne, kyslík, séria, trhliny, výhra a prehra |
| `Level.cs` | koreň prefabu úrovne: názov, kyslík na štarte, podiel letcov, prahy hviezdičiek, farba svetla, hranice, odkaz na hráča a cieľ |
| `PlayerController.cs` | loď hráča: mierenie myšou, streľba, spätný ráz, najvyššia rýchlosť, trhliny pri tvrdom náraze |
| `Bullet.cs` | strela: rýchlosť, životnosť, zásah kameňa alebo míny |
| `Asteroid.cs` | kameň: veľkosť, zásahy, rozpad veľkého kameňa, kryštály, pohyb a otáčanie vnútri hraníc |
| `Meteor.cs` | rýchly kameň zhora: ohnivý chvost a výstražný trojuholník |
| `MeteorSpawner.cs` | posiela meteory a roje s predstihom (zapína sa na koreni úrovne) |
| `OxygenCrystal.cs` | kryštál kyslíka: rozlet, priťahovanie k lodi, blikanie, ťah čiernych dier |
| `OxygenTank.cs` | kyslíková fľaša: +30 kyslíka a zaplátanie trhliny |
| `Mine.cs` | míny: lovec, hliadka, stena, prstenec, ťažká; výbuch a reťaz |
| `BlackHole.cs` | čierna diera: ťah, pohlcovanie, plávanie, prechod červou dierou |
| `Orbiter.cs` | drží kameň na kruhovej dráhe okolo čiernej diery |
| `Planet.cs` | planéta: ťah lode a striel, dotyk = prehra |
| `Leak.cs` | trhlina v trupe, ktorá vyfukuje vzduch |
| `LeakPuff.cs` | jeden obláčik unikajúceho vzduchu |
| `Blast.cs` | záblesk výbuchu |
| `Ship.cs` | cieľová loď: dotyk a krúžok pri pristávaní |
| `CameraRig.cs` | sledovanie lode a trasenie kamery |
| `Hud.cs` | rozhranie: vzduch, značky trhlín, séria, šípka, nápisy, koniec úrovne |
| `PixelFont.cs` | pixelové písmo 5 × 7 a pixelové ikony |
| `PixelLabel.cs` | jeden riadok textu pixelovým písmom (funguje aj v editore) |
| `ParallaxBackground.cs` | jedna vrstva pozadia s paralaxou (opakujúca sa, obmedzená alebo voľná) |
| `StarLayer.cs` | vrstva hviezd, ktoré sa opakujú okolo kamery |
| `Texts.cs` | všetky slová hry |
| `Tuning.cs` | trieda so všetkými číslami balansu (hodnoty sú v `Tuning.asset`) |

Skripty programátora `GunScript.cs` a `PlayerMovementScript.cs` patria k jeho scénam
a táto hra ich nepoužíva.

## Editorové skripty (`Assets/Editor`)

| Skript | Čo robí |
|---|---|
| `DraftSetup.cs` | vyrobí všetky prefaby, úrovne, rozhranie a scénu (menu Rebuild Prefabs And Scene) |
| `LevelData.cs` | číta textové súbory úrovní |
| `Art.cs` | hľadá obrázky v `Assets/_Resources/Art` podľa mena; ak chýbajú, nakreslí sa zástupný tvar |
| `SpriteFactory.cs` | vyrába jednoduché tvary a ikony ako PNG súbory |
| `ArtBake.cs` | zapečie kamene a hviezdy z 3D modelov výtvarníčky do obrázkov |
| `ArtImportSettings.cs` | automaticky nastaví import obrázkov (sprite, 100 pixelov na jednotku, kompresia, opakovanie oblakov) a 3D modelov |
| `TuningConnector.cs` | pripojí `Tuning.asset` ku všetkým komponentom v prefaboch a v scéne |
| `DraftBuild.cs` | skúšobný build pre Windows bez zmeny nastavení projektu |

## Menu **Filip** v Unity

| Položka | Čo robí |
|---|---|
| Rebuild Prefabs And Scene | vyrobí znova všetky prefaby, úrovne, rozhranie a objekty v scéne |
| Connect Tuning To Prefabs And Scene | doplní odkaz na `Tuning.asset` všade, kde chýba |
| Select Tuning Asset | označí `Tuning.asset`, aby sa čísla dali hneď meniť v Inspectore |
| Bake Rocks And Stars From FBX | znova zapečie obrázky kameňov a hviezd z 3D modelov |
| Test Build (Windows) | zostaví `.exe` do `D:\ProjectGameJam\DraftBuild` a spustí ho |

## Prefaby (`Assets/Prefabs`)

- **Hráč a strely:** `Player`, `Bullet`, `Leak` (trhlina), `AirPuff` (obláčik vzduchu).
- **Kamene a meteory:** `Asteroid_Small`, `Asteroid_Medium`, `Asteroid_Large`,
  `Meteor_Small`, `Meteor_Medium`.
- **Vzduch:** `OxygenTank`, `OxygenCrystal`.
- **Nebezpečenstvá:** `Mine_Hunter`, `Mine_Patrol`, `Mine_Wall`, `Mine_Orbit`, `Mine_Heavy`,
  `MineRing` (5 mín na kruhu), `BlackHole_Big`, `BlackHole_Small`, `BlackHole_Drift`,
  `Planet`, `Blast` (výbuch).
- **Cieľ:** `Ship`.
- **Pozadie:** `Background` (obvyklý vzhľad) a `Background_Level1` – `Background_Level3`
  (každá úroveň vo vlastných farbách).
- **Úrovne:** `Levels/Level1` – `Level3`.

## Nástroje mimo Unity (`Assets/Editor/Tools~`)

Spúšťajú sa cez PowerShell alebo bash, Unity ich nevidí.

| Nástroj | Na čo slúži |
|---|---|
| `NebulaGen.cs`, `gen.ps1` | generovanie bezšvových oblakov (hmlovín) |
| `BackdropGen.cs`, `backdrop.ps1` | slnko, planéta s prstencom, čierna diera |
| `StationBake.cs`, `station.ps1` | 3D trosky stanice vyrenderované do obrázka |
| `RockBake.cs`, `rocks.ps1`, `FbxDump.cs`, `bake.ps1` | čítanie súborov FBX a staršie pečenie kameňov a hviezd mimo Unity (dnes to robí `ArtBake.cs` v Unity) |
| `ShipGen.cs`, `ship.ps1` | starší pokus o raketu hráča (nepoužíva sa) |
| `mklevels.sh` | rozmiestnenie úrovní do textových súborov |

## Verzie v Gite

| Vetva alebo značka | Čo obsahuje |
|---|---|
| `main` | práca programátora: štruktúra projektu, úvodné menu |
| `filip/draft` | táto verzia hry |
| značka `draft-runtime-backup` (lokálna) | staršia verzia, v ktorej sa hra stavala kódom pri spustení |
| značka `ship-player-backup` (lokálna) | verzia s loďou hráča z modelu výtvarníčky |

Pravidlá tímu:

- do `main` sa nič nezlučuje bez súhlasu;
- zmeny, ktoré Unity sama spraví v `ProjectSettings/` a `Packages/`, sa necommitujú;
- súbory sa commitujú spolu so súbormi `.meta`;
- práca programátora (menu, jeho scény a skripty) sa nemení.
