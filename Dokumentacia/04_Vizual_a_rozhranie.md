# 4. Vizuál a rozhranie

## Rozhranie počas hry (HUD)

Rozhranie ukazuje ikony a čísla, nie vety. Nápovedy na spodku obrazovky boli zámerne
odstránené: tím chcel rozhranie, ktoré nepôsobí strojovo a neruší.

**Písmo.** Hra má vlastné pixelové písmo 5 × 7 bodov, nakreslené priamo v kóde
(`Assets/_Scripts/PixelFont.cs`). Pozná iba veľké latinské písmená, číslice
a znaky `- . : / + !`. Systémové písmo ani TextMesh Pro sa v hre nepoužívajú.
Ikony (nápis O₂, trhlina, šípka) sú nakreslené rovnakým spôsobom.

| Miesto | Čo ukazuje |
|---|---|
| Vľavo hore | nápis O₂ a ukazovateľ vzduchu z 20 dielikov (1 dielik = 5 kyslíka). Normálne je azúrový ako všetko, čo dáva vzduch, pri úniku vzduchu oranžový, pod 25 % červený a bliká (aj nápis O₂). Prázdne dieliky v prvej štvrtine sú jemne červenkasté: tam začína nebezpečenstvo. Keď loď stratí veľa vzduchu naraz (náraz, čierna diera, rýchle pristátie), stratená časť chvíľu zostane bledá a potom zmizne, takže je vidieť, koľko to stálo. Novo získaný vzduch (kryštály, fľaša) chvíľu svieti nabielo. |
| Pod ukazovateľom | červené trhliny: jedna za každú trhlinu v trupe |
| Vpravo hore | séria „X2“ až „X5“ a pod ňou tenký pásik so zostávajúcim časom série |
| Okraj obrazovky | zelená šípka smerom k cieľovej lodi a vzdialenosť k nej; zmizne, keď je loď v zábere |
| Hore v strede | na začiatku úrovne „1/3 ROCKS“ (tri sekundy, potom pomaly zmizne) |
| Stred | krátke hlásenie „TOO FAST“ pri príliš rýchlom pristátí |
| Celá obrazovka | okraje obrazovky stmavnú vo farbe búrky, keď je loď za okrajom úrovne; červený záblesk pri trhline |

**Koniec úrovne.** Obrazovka stmavne a v strede sa ukáže:

- pri úspechu: malé počítadlo „2/3“, veľké zelené „DOCKED“, nápis O₂ so zostávajúcim
  vzduchom a kláves „ENTER“ v rámčeku (ako klávesa na klávesnici);
- pri prehre: veľké červené „OUT OF AIR“ alebo „CRASHED“ a kláves „R“ v rámčeku.

Všetky slová, ktoré hra ukazuje, sú v jednom súbore `Assets/_Scripts/Texts.cs`.
Názvy úrovní sú v súboroch úrovní (riadok `# name=`).

**Pomôcky v hernom svete:**

- Okolo cieľovej lode sa pri priblížení objaví krúžok: zelený = dosť pomalé pristátie,
  červený = príliš rýchle.
- **Značky nebezpečenstva pri okraji obrazovky** ukazujú hrozby, ktoré ešte nie je vidieť:
  meteor (oranžová, presne tam, kde vletí do obrazovky) a mínu lovca, ktorá loď prenasleduje
  (červená). Značka je malá pixelová ikona hrozby s mäkkou žiarou a šípkou, ktorá mieri
  **dnu, na hráča** (zelená šípka k cieľovej lodi mieri von, preto sa nepletú). Čím je hrozba
  bližšie, tým je značka väčšia, jasnejšia a rýchlejšie bliká.
- Pole čiernej diery je vidieť ako mäkkú teplú žiaru.
- Hranicu úrovne ukazuje stena búrky (pozri nižšie).

## Vizuálny štýl

- **Svieti len to, čo je pre hru dôležité:** loď hráča (biela žiara pod ňou), strely (žltá),
  kyslíkové fľaše a kryštály (azúrová), meteory (oranžová), míny (farba lampy).
- **Kamene** sú veľké, teplé a svetlé, s hlineným povrchom. Dobre sa odlišujú od chladného pozadia.
- **Pozadie** je stlmené a nebliká, aby nesúperilo s kameňmi.
- **Každá úroveň má vlastnú farebnú tému:** farba svetla (globálne 2D svetlo farbí kamene,
  loď aj cieľ), sada oblakov, farba a hustota hviezd, farba búrky na okraji a veľký objekt v diaľke.

## Pozadie a paralaxa

Hráč letí cez oblaky, nie ponad nehybný obrázok. Pozadie má niekoľko vrstiev a každá
sa posúva inou rýchlosťou (paralaxa). Číslo udáva, akou časťou rýchlosti sveta sa vrstva
na obrazovke posúva:

| Vrstva (od najvzdialenejšej) | Rýchlosť |
|---|---|
| Vzdialené oblaky | 0,25 |
| Veľký objekt v diaľke (slnko, trosky, planéta) | 0,04 – 0,12 |
| Hviezdny prach (drobné bodky) | 0,2 |
| Vzdialené hviezdy | 0,3 |
| Blízke oblaky (priesvitné) | 0,4 |
| Blízke hviezdy | 0,45 |

- Oblaky sú bezšvové obrázky poskladané 3 × 3, takže sa opakujú donekonečna všetkými smermi.
- Hviezdy sa po okraji „prenášajú“ na druhú stranu, takže nikdy nedôjdu.
- Hviezdy majú jadro a farebnú svätožiaru vo farbách, ktoré ladia s hmlovinou
  (ľadovo modrá, azúrová, fialová, ružová, zlatá, biela). Neblikajú.

**Sady oblakov:** predvolená (fialovo-tyrkysová), `calm` (modrá, riedka),
`alert` (karmínová, hustá), `planet` (zlato-zelená).

**Veľké objekty v diaľke:** `sun` (vzdialené slnko), `wreck` (trosky kruhovej stanice,
tmavé a mierne rozmazané, aby pôsobili ďaleko v tieni, nie ako niečo, k čomu sa dá letieť),
`planet` (planéta s prstencom).

## Búrka na okraji úrovne

Namiesto tenkého rámu je okraj úrovne stena búrky. Na každej strane ležia dve vrstvy
obrázka búrky: tmavé husté telo a svetlé záhyby. Tečú pozdĺž okraja proti sebe, takže
oblaky sa zdanlivo prevaľujú. V časti búrky, ktorá je na obrazovke, občas zablýska.
Ďaleko za okrajom je už len jednoliata tmavá farba búrky. Obrázky (`storm_body`,
`storm_glow`, `storm_bolt1` – `3`) vygeneroval nástroj `Assets/Editor/Tools~/storm.ps1`
(rovnaký šum s krútením ako pri oblakoch, opakuje sa pozdĺž okraja bez švu).

## Grafika a jej príprava

Hra je 2D, ale časť grafiky vznikla z 3D modelov výtvarníčky. Modely sa nepúšťajú
do hry priamo: Unity z nich v editore „zapečie“ (vyrenderuje) 2D obrázky.

| Grafika | Ako vznikla |
|---|---|
| Kamene (12 obrázkov: 4 veľké, 4 stredné, 4 malé) | Z modelu výtvarníčky `stone.fbx` (5 tvarov kameňov) z rôznych uhlov. Svetlo prichádza hlavne od kamery, takže kameň vyzerá dobre aj pri otáčaní. Pridaný je jemný reliéf, zatienenie v záhyboch a teplý okraj. Kolízny kruh má 0,45 strany obrázka. Menu: Filip > Bake Rocks And Stars From FBX. |
| Hviezdy (5 tvarov + 5 žiar) | Z modelu výtvarníčky `stars.fbx`. Tvar zaberá stredných 42 % obrázka, zvyšok je priestor pre žiaru. Farbu dostávajú až v hre. |
| Oblaky (hmloviny) | Vygenerované programom: periodický Perlinov šum vo viacerých vrstvách (fBm), preto sú bezšvové. |
| Slnko, planéta s prstencom, čierna diera, planéta | Vygenerované kódom. |
| Trosky stanice | 3D scéna z jednoduchých telies (valce, časti prstenca, nosníky, solárne panely, úlomky) vyrenderovaná vlastným softvérovým rendererom, tým istým ako kamene. |
| Jednoduché tvary (kruh, žiara, prstenec, trojuholník, štvorec) a ikony rozhrania | Vytvorené kódom, uložené v `Assets/Art/Shapes`. |

**Zástupné obrázky** (kým nie sú hotové obrázky od výtvarníčky):

- loď hráča: biely kruh s modrým priezorom a sivou zbraňou;
- cieľová loď: modrosivý obdĺžnik s nosom a motorom;
- míny: tmavá guľa s ostňami a farebnou lampou;
- kyslíková fľaša: azúrová fľaša s bielym uzáverom;
- kryštál: malý azúrový kosoštvorec.

Keď sa do `Assets/_Resources/Art` pridá obrázok so správnym menom (napr. `mine.png`)
a spustí sa Filip > Rebuild Prefabs And Scene, zástupný obrázok sa nahradí.
Zoznam mien a veľkostí je v kapitole 6.

**Pripravené, zatiaľ nepoužité:** loď hráča z 3D modelu výtvarníčky (`spaceship.fbx`).
Bola zapečená z farebnej textúry, normálovej mapy a mapy drsnosti a vložená do hry,
na žiadosť tímu však bola dočasne vrátená späť. Celá táto verzia je uložená
pod značkou `ship-player-backup` (pozri kapitolu 7).
