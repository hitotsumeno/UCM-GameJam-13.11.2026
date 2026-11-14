# 2. Herné mechaniky

Všetky čísla v tejto kapitole sú predvolené hodnoty z nastavení
`Assets/_Settings/Tuning.asset`. Dajú sa meniť bez programovania (pozri kapitolu 6).

## Jednotky

- 1 jednotka v hre = 100 pixelov obrázka.
- Kamera ukazuje približne 35,5 × 20 jednotiek.
- Mapa úrovne je rozdelená na bunky 2 × 2 jednotky.

## Pohyb a streľba

- Loď sa sama nehýbe a vesmír ju nebrzdí (žiadne trenie). Rýchlosť si drží,
  kým ju niečo nezmení.
- Výstrel dá lodi impulz **3** proti smeru mierenia. Najvyššia rýchlosť lode je **12** jednotiek za sekundu.
- Kadencia: **4** výstrely za sekundu pri držaní tlačidla.
- Strela letí rýchlosťou **20**, žije **2** sekundy, má priemer 0,3 a žlto svieti.
- **Každý výstrel stojí 1 kyslíka.**
- Telo lode je kruh s priemerom **1** a hmotnosťou 1.
- Strela lode nezasiahne vlastnú loď.

Brzdí sa výstrelom v smere letu: spätný ráz loď spomalí.

## Kyslík (vzduch)

| Čo | Hodnota |
|---|---|
| Maximum | 100 |
| Na začiatku úrovne | podľa úrovne: 55 / 70 / 70 |
| Úbytok za sekundu | 2 |
| Výstrel | −1 |
| Mimo hraníc úrovne | úbytok 4× rýchlejší |
| Každá trhlina v trupe | +1 úbytok za sekundu |
| Kryštál | +2,8 (násobený bonusom série) |
| Kyslíková fľaša | +30 a zaplátanie jednej trhliny |
| Príliš rýchle pristátie | −5 |
| Prechod čiernou dierou | −10 (a trhlina) |

Keď kyslík klesne na 0, hra končí nápisom „out of air“.

## Kamene (asteroidy)

- Tri veľkosti: malý **2**, stredný **4**, veľký **6** jednotiek. Každý kameň je navyše
  náhodne až o 12 % menší alebo väčší a má náhodný vzhľad (12 rôznych obrázkov).
- Počet zásahov do zničenia: malý **1**, stredný **2**, veľký **4**.
  Po každom zásahu kameň viac sčervenie, takže poškodenie je vidieť.
- Veľký kameň sa po zničení rozpadne na **dva stredné**.
- Kamene sa pomaly unášajú (0,2 – 0,6 jednotky za sekundu) a otáčajú sa.
  Časť z nich („letci“, podiel určuje úroveň) letí rýchlejšie, 1 – 3 jednotky za sekundu.
- Odrážajú sa od seba aj od lode (pružnosť 0,5). Na hranici úrovne sa otočia späť,
  takže neodletia preč.
- Hmotnosť rastie s veľkosťou (0,5 × priemer²): veľký kameň loď ľahko odhodí.

## Kryštály kyslíka („ničí, aby si dýchal“)

| Zdroj | Počet kryštálov |
|---|---|
| Malý kameň | 2 |
| Stredný kameň | 3 |
| Veľký kameň | 4 |
| Mína zničená strelou | 4 (ťažká mína 8) |
| Mína odpálená reťazou alebo kameňom | 2 |

- Jeden kryštál = **2,8** kyslíka, vynásobené bonusom série.
- Kryštály sa rozletia do strán a spomalia. Po **3,5** sekundy zmiznú,
  poslednú sekundu blikajú.
- Kryštály bližšie ako **3,5** jednotky sa samy priťahujú k lodi. Zoberú sa,
  keď sú bližšie ako 0,9.
- Čierne diery kryštály vťahujú a pohlcujú. Treba ich chytiť rýchlo.
- Všetko, čo dáva vzduch (kryštály aj fľaše), svieti azúrovou farbou.

## Séria (kombo)

- Každé zničenie do **3** sekúnd po predchádzajúcom zvýši sériu: x2, x3 … najviac **x5**.
- Každý stupeň pridá **+50 %** kyslíka z kryštálu: x2 = +50 %, x3 = +100 %, x5 = +200 %.
- Vpravo hore sa ukazuje „X3“ a tenký pásik so zostávajúcim časom.
- Tvrdý náraz (trhlina v trupe) sériu zruší.

## Trhliny v trupe (namiesto životov)

Hra nemá životy ani okamžité tresty. Tvrdý úder prederaví trup:

- náraz do kameňa alebo meteoru rýchlosťou vyššou ako **7**,
- dotyk míny alebo jej výbuch bližšie ako **2,2** jednotky (pri ťažkej míne 4,4),
- prechod čiernou dierou.

Čo sa stane:

- Na trupe sa objaví tmavá trhlina na strane, odkiaľ prišiel úder.
  Z nej strieľa prúd vzduchu (biele obláčiky), ktorý sa otáča spolu s loďou.
- Každá trhlina zrýchli úbytok kyslíka o **1** za sekundu. Najviac **5** trhlín.
- Obrazovka sa červeno zableskne, kamera sa silno zatrasie, pod ukazovateľom vzduchu
  pribudne červená kvapka a ukazovateľ zoranžovie.
- Séria sa zruší.
- Trhliny vydržia do konca úrovne, ak ich hráč nezaplátá fľašou.

## Kyslíková fľaša

- Pridá **30** kyslíka a zaplátá jednu (najnovšiu) trhlinu.
- Leží väčšinou mimo priamej cesty, niekedy ju stráži prstenec mín.

## Meteory

- Prilietajú zhora spoza obrazovky. Interval určuje úroveň (úroveň 1: každých 5 – 8 sekúnd),
  prvý meteor príde po 4 sekundách.
- Rýchlosť **13 – 17**. Mieria s predstihom **0,9** sekundy tam, kam loď práve letí.
- V **30 %** prípadov príde roj troch meteorov (prvý mieri presne, ďalšie okolo).
- 75 % meteorov je malých, 25 % stredných. Majú ohnivý chvost a oranžovú žiaru.
- Kým je meteor ešte nad obrazovkou, pri hornom okraji ukazuje červený trojuholník,
  odkiaľ priletí.
- Meteor je kameň: dá sa zostreliť a zanechá kryštály. Náraz doň prederaví trup.
  Rýchly meteor odpáli mínu, do ktorej narazí.

## Míny

| Symbol na mape | Druh | Farba lampy | Správanie |
|---|---|---|---|
| `m` | Lovec | červená | Spí, kým je loď ďalej ako 16 jednotiek. Potom pláva za ňou rýchlosťou 6,5 (loď vie až 12). Vzdá sa, až keď je loď ďalej ako 26. |
| `h` / `v` | Hliadka | žltá | Kĺže tam a späť o 6 jednotiek na každú stranu, vodorovne (`h`) alebo zvisle (`v`), rýchlosťou 3,5. |
| `s` | Stena | žltá | Stojí na mieste v hustom rade. Jeden výstrel odpáli reťazovo celú stenu. |
| `M` | Ťažká mína | oranžová | Veľká (2,4 jednotky), vydrží 3 zásahy, jej výbuch je dvakrát väčší. |
| `R` | Prstenec | žltá | 5 mín krúži okolo kyslíkovej fľaše (polomer 6, rýchlosť 3). Sú od seba ďalej, než dosiahne výbuch, takže treba ich rozoberať po jednej. |

Spoločné pravidlá:

- Lampa v strede míny bliká. Lovec, ktorý loď prenasleduje, bliká rýchlo.
- **Dotyk lode** = výbuch a trhlina v trupe.
- **Výbuch** odtlačí všetko v okruhu **4,5** jednotky, po 0,12 sekundy odpáli
  susedné míny (reťaz) a kamene v okruhu 2,5 dostanú jeden zásah.
- Mínu odpáli aj kameň alebo meteor, ktorý do nej narazí rýchlosťou vyššou ako 5.
- Mína zničená strelou zvýši sériu a nechá 4 kryštály.

Taktiky: lovca sa dá zlákať do kameňov alebo do čiernej diery, stenu odpáliť z diaľky
jediným výstrelom, ťažkú mínu zostreliť skôr, ako sa hniezdo okolo nej zobudí.

## Čierne diery

| Symbol na mape | Druh | Tmavé jadro | Dosah ťahu | Sila | Navyše |
|---|---|---|---|---|---|
| `O` | Veľká | 2,2 | 20 | 520 | okolo krúži 6 kameňov na dvoch kruhoch opačnými smermi |
| `c` | Malá | 1,1 | 11 | 200 | okolo krúžia 2 kamene |
| `x` / `y` | Plávajúca malá | 1,1 | 11 | 200 | kĺže 8 jednotiek tam a späť, vodorovne (`x`) alebo zvisle (`y`) |

- Ťah: zrýchlenie = sila / vzdialenosť², najviac 14.
- Ťahá loď, strely, míny a kryštály. Kamene ťah necítia.
- Strely ťahá **2,5×** silnejšie, takže letia viditeľne po oblúku a dá sa strieľať „za roh“.
- Kde ťah pôsobí, ukazuje mäkká teplá žiara, ktorá hustne k diere: kde svieti, tam ťahá.
- Strely, míny a kryštály, ktoré padnú do jadra, zmiznú. Lovca sa dá zlákať do diery.
- **Loď sa v diere nezničí:** diera je červia diera. Loď vyletí z jednej z dvoch najbližších
  iných dier rýchlosťou 10, s trhlinou v trupe a s −10 kyslíka. Môže ju hodiť dopredu
  aj dozadu, je to hazard, nie skratka. Dve sekundy po vylete ju diery nepohltia.
- Obrázok diery sa pomaly otáča.

## Planéta (pripravená, v úrovniach zatiaľ nie je)

- Symbol `@`. Ťahá loď a strely v okruhu **22** jednotiek (zrýchlenie 288 / vzdialenosť², najviac 9).
- Okraj poľa ukazuje bodkovaný kruh. Popri planéte sa dá zatočiť bez streľby.
- Dotyk planéty = prehra „crashed“. Kamene a míny ťah planéty necítia.

## Hranice úrovne

- Úroveň má obdĺžnikové hranice, ktoré ukazuje tenký rám (farbu určuje úroveň).
- Von sa dá vyletieť, ale vzduch tam ubúda **4×** rýchlejšie, obrazovka sčervenie
  a bliká nápis „turn back“.
- Kamene sa na hranici otáčajú späť.

## Pristátie (dokovanie) k cieľovej lodi

- Cieľová loď je zatiaľ zástupný obdĺžnik 6 × 2,5 jednotky.
- Dotyk rýchlosťou do **5** = úspech („docked“), koniec úrovne.
- Rýchlejší dotyk: loď sa odrazí (60 % rýchlosti, aspoň 3), stratí 5 kyslíka,
  ukáže sa nápis „too fast“ a kamera sa zatrasie.
- Od vzdialenosti 22 jednotiek sa okolo cieľovej lode objavuje krúžok:
  **zelený** = loď je dosť pomalá, **červený** = príliš rýchla.
- Kým cieľová loď nie je na obrazovke, šípka pri okraji obrazovky ukazuje smer k nej
  a vzdialenosť.

## Kamera

- Ortografická, výška pohľadu 20 jednotiek (asi 35,5 × 20).
- Plynulo sleduje loď a pri každom výstrele sa jemne zatrasie.
  Pri trhline v trupe sa trasie dlhšie a silnejšie.
