# 3. Úrovne

Hra má tri dlhé úrovne. Každá sa hrá inak a učí iný spôsob, ako získať vzduch.
Vo všetkých troch hráč začína dole a cieľová loď čaká hore.

## Prehľad

| # | Názov v hre | Veľkosť (bunky) | Veľkosť (jednotky) | Vzduch na štarte | Meteory | Hlavná výzva |
|---|---|---|---|---|---|---|
| 1 | `rocks` | 20 × 90 | 40 × 180 | 65 | každých 5 – 8 s | kamene a meteory, vzduch iba z kameňov |
| 2 | `mines` | 24 × 84 | 48 × 168 | 70 | nie | mínové pole, asi 83 mín, vzduch iba z mín |
| 3 | `holes` | 22 × 80 | 44 × 160 | 70 | nie | 10 čiernych dier, vzduch na ich obežných dráhach |

Na začiatku úrovne sa na tri sekundy ukáže jej poradie a názov, napríklad „1/3 ROCKS“.

## Úroveň 1: rocks (kamene)

- **Obsah:** 38 malých, 18 stredných a 7 veľkých kameňov, jediná kyslíková fľaša.
  35 % kameňov sú „letci“, ktorí sa pohybujú rýchlejšie.
- **Ako sa hrá:** vzduchu je na štarte menej (65) a fľaša je len jedna. Takmer všetok
  kyslík je v kameňoch: strieľaj, rozbíjaj a zbieraj kryštály, kým nezhasnú.
  Pred cieľovou loďou sú husté rady stredných a veľkých kameňov, cez ktoré si treba
  prestrieľať cestu.
- **Nebezpečenstvo:** meteory zhora každých 5 – 8 sekúnd, niekedy celý roj. Mieria tam,
  kam loď letí, takže treba meniť smer.
- **Téma:** pokojné modré riedke oblaky (sada `calm`), v diaľke trosky kruhovej
  stanice v tieni, chladné hviezdy.

## Úroveň 2: mines (míny)

- **Obsah:** 17 lovcov, 22 hliadok (15 vodorovných, 7 zvislých), 33 mín v stenách,
  1 ťažká mína, 2 prstence po 5 mín okolo kyslíkových fliaš, len 5 kameňov.
- **Ako sa hrá:** kamene tu takmer nie sú, vzduch je iba v mínach. Každý výstrel
  ťa však odhodí dozadu a míny sú aj za tebou. Lovci sa prebúdzajú a prenasledujú,
  hliadky treba prekĺznuť v medzere, steny sa dajú odpáliť jediným výstrelom
  (a dajú veľa kryštálov naraz). Ťažká mína stojí v strede spiaceho hniezda:
  treba ju zostreliť z diaľky skôr, ako sa hniezdo zobudí. Fľaše strážia prstence mín.
- **Téma:** poplachová úroveň: husté karmínové oblaky (sada `alert`), červenkavé svetlo
  na objektoch, teplejšie hviezdy, menej hviezd.

## Úroveň 3: holes (čierne diery)

- **Obsah:** 5 veľkých, 3 malé a 2 plávajúce čierne diery; 7 lovcov, 4 hliadky,
  7 mín v stene, 1 ťažká mína, 1 prstenec mín okolo fľaše, 2 voľné fľaše, 4 malé kamene.
- **Ako sa hrá:** diery ťahajú loď, strely aj kryštály. Strely letia po oblúku,
  takže sa dá trafiť aj okolo diery. Vzduch je hlavne v kameňoch, ktoré krúžia
  okolo dier: treba ich zostreliť a chytiť kryštály skôr, ako ich diera pohltí.
  Lovcov sa oplatí zlákať do diery. Kto do diery spadne, vyletí z inej, s trhlinou
  a s menej vzduchu. Úroveň je rozdelená na päť „miestností“, ktorých polia ťahu
  pokrývajú celú šírku. Na konci je brána medzi dvoma veľkými dierami a v nej stena mín.
- **Téma:** fialovo-tyrkysové oblaky zafarbené do fialova a stlmené, chladné svetlo,
  viac hviezd.

## Súbor úrovne

Úrovne sú zapísané v obyčajných textových súboroch
`Assets/_Resources/Levels/level1.txt`, `level2.txt`, `level3.txt`
(číslovanie za sebou, bez medzier). Dajú sa upravovať v Poznámkovom bloku.

Textový súbor je **len zdroj**. Do hry sa dostane až po spustení menu
**Filip > Rebuild Prefabs And Scene**, ktoré z neho vyrobí prefab
`Assets/Prefabs/Levels/LevelN.prefab` a farebné pozadie `Assets/Prefabs/Background_LevelN.prefab`.
Pozor: opätovné spustenie menu prepíše aj prefaby úrovní, ktoré niekto upravil ručne.

### Nastavenia (riadky na začiatku súboru, začínajú znakom `#`)

| Riadok | Význam |
|---|---|
| `# name=rocks` | názov úrovne (ukáže sa na začiatku) |
| `# radio=...` | jeden riadok príbehu (zatiaľ sa neukazuje) |
| `# oxygen=70` | kyslík na štarte (najviac 100) |
| `# meteors=off` | meteory vypnuté; alebo napr. `5-8`: jeden meteor každých 5 – 8 sekúnd |
| `# movers=0.4` | podiel rýchlejších kameňov: 0 žiadny, 1 všetky |
| `# stars=65,35` | koľko % kyslíka treba zachovať na 3 a na 2 hviezdičky (hodnotenie zatiaľ nie je hotové) |
| `# hint1=text` | nepoužíva sa: nápovedy boli z obrazovky odstránené; riadky a číslice na mape môžu zostať |

Čísla sa píšu s bodkou: `0.5`, nie `0,5`.

### Vzhľad úrovne (téma)

Tieto riadky nie sú povinné; bez nich bude vzhľad obvyklý.

| Riadok | Význam |
|---|---|
| `# nebula=calm` | sada oblakov: `calm` modré a riedke, `alert` karmínové a husté, `planet` zlato-zelené; bez riadku fialovo-tyrkysové |
| `# backdrop=sun` | veľký objekt v diaľke: `sun` slnko, `wreck` trosky stanice, `planet` planéta s prstencom; bez riadku nič |
| `# light=1.0,0.85,0.8` | farba svetla na kameňoch, lodi a cieli (červená, zelená, modrá od 0 do 1) |
| `# starcolor=0.8,0.9,1.0` | farba hviezd |
| `# stardensity=0.6` | počet hviezd: 1 obvykle, 0.5 polovica, 2 dvojnásobok |
| `# frame=1.0,0.3,0.25` | farba búrky na okraji úrovne (predvolená červená) |
| `# tint=0.9,0.8,1.0` | ďalší odtieň oblakov |
| `# clouds=0.6` | jas oblakov: 1 obvykle, 0.6 tlmenejšie, 1.3 jasnejšie |

### Mapa

Všetko ostatné v súbore je mapa. Jeden znak = jedna bunka 2 × 2 jednotky.
Prvý riadok mapy je horný okraj úrovne. Všetky riadky majú mať rovnakú dĺžku.
Obrazovka ukáže približne 18 buniek do šírky a 10 do výšky.

| Znak | Objekt |
|---|---|
| `P` | hráč (štart) |
| `S` | cieľová loď, zaberá 3 bunky do šírky |
| `.` | prázdno |
| `o` | kyslíková fľaša |
| `a` | malý kameň (1 bunka) |
| `A` | stredný kameň (2 × 2 bunky, stojí v strede svojej bunky) |
| `B` | veľký kameň (3 × 3 bunky, stojí v strede svojej bunky) |
| `m` | červená mína lovec |
| `h` | žltá mína hliadka, vodorovne (3 bunky na každú stranu) |
| `v` | žltá mína hliadka, zvisle |
| `s` | žltá mína v stene (stavať tesne vedľa seba: jeden výstrel odpáli celú stenu) |
| `M` | veľká oranžová mína (3 zásahy, veľký výbuch) |
| `R` | kyslíková fľaša s prstencom 5 mín (polomer 3 bunky) |
| `O` | veľká čierna diera (ťahá 10 buniek ďaleko, krúži okolo nej 6 kameňov) |
| `c` | malá čierna diera (ťahá 5,5 bunky ďaleko, krúžia okolo nej 2 kamene) |
| `x` | malá čierna diera, pláva vodorovne (4 bunky na každú stranu) |
| `y` | malá čierna diera, pláva zvisle |
| `@` | planéta (ťahá loď a strely, dotyk = prehra) |
| `1` – `9` | bývalé miesta nápovied (teraz bez účinku) |

Rady pre stavbu: okolo `A` nechať jednu prázdnu bunku, okolo `B` dve, inak sa kamene
objavia v sebe a rozletia sa.

### Ako úrovne vznikli

Prvé rozmiestnenie úrovní spravil skript `Assets/Editor/Tools~/mklevels.sh`
(spúšťa sa mimo Unity). Po pripomienkach z hrania sa rozmiestnenie upravovalo v tom istom skripte.
Úrovne boli rozmiestnené bez dlhého testovania hráčmi, takže ich bude treba ešte doladiť.
