# 1. O hre

## Príbeh

Malej lodi uprostred vesmíru došlo palivo. Motor je mŕtvy, ostala len zbraň.
Každý výstrel však loď odtlačí opačným smerom, a tak sa dá strieľaním letieť.
Lenže zbraň aj posádka potrebujú vzduch: kyslík ubúda každú sekundu
a s každým výstrelom. Treba sa dostať k cieľovej lodi a pripojiť sa k nej,
skôr ako vzduch dôjde.

Pôvodne mal byť hráč astronaut odnesený od lode, ktorý drží v rukách zbraň.
Tím neskôr rozhodol, že hráč je malá loď bez paliva. Pravidlá zostali rovnaké.
Hráčova loď je malá zelená loď od výtvarníčky. Natáča sa hlavnou turbínou ku kurzoru:
strely vyletujú z turbíny a spätný ráz ju tlačí nosom dopredu. Cieľová loď je veľká
fialová materská loď.

## Cieľ úrovne

Každá úroveň je dlhý pás vesmíru. Hráč začína dole, cieľová loď čaká hore.
K lodi sa treba **pomaly** priblížiť a dotknúť sa jej (dokovanie).
Ak loď letí príliš rýchlo, odrazí sa a stratí trochu vzduchu.

Po úspešnom pripojení sa ukáže, koľko vzduchu zostalo, a klávesom Enter
sa pokračuje na ďalšiu úroveň. Po poslednej úrovni sa hra začne odznova.

## Ovládanie

| Akcia | Ovládanie |
|---|---|
| Mierenie | myš (zbraň sa otáča za kurzorom) |
| Streľba | ľavé tlačidlo myši; držanie = 4 výstrely za sekundu |
| Reštart úrovne | **R** (kedykoľvek) |
| Ďalšia úroveň | **Enter** (po úspešnom pripojení) |
| Ukončenie hry | **Esc** (iba v zostavenej hre, nie v editore) |

Strela letí ku kurzoru, loď odletí na opačnú stranu. Kto chce letieť doprava,
strieľa doľava.

## Hlavná herná slučka

1. **Výstrel** posunie loď opačným smerom. Vesmír loď nebrzdí, letí, kým ju
   niečo nezastaví alebo kým znova nevystrelí.
2. **Strela rozbije kameň alebo mínu** a z nich vypadnú kryštály kyslíka.
3. **Kryštály ležia tam, kam si strieľal**, teda za tebou: výstrel ťa od nich
   odhodil. Treba sa vrátiť do trosiek a pozbierať ich skôr, ako zhasnú (3,5 s).
4. **Rýchle ničenie za sebou** zvyšuje sériu (až x5) a každý kryštál potom
   dá viac vzduchu.
5. **Tvrdý náraz prederaví trup.** Z trhliny uniká vzduch a kyslík ubúda
   rýchlejšie. Kyslíková fľaša jednu trhlinu zaplátá.
6. **Pri cieľovej lodi treba spomaliť:** príliš rýchly nálet ťa odrazí.

Na tomto stojí celé rozhodovanie hráča. Streľba je pohon, zbraň aj spôsob,
ako získať vzduch, no každý výstrel vzduch stojí. Kam vystreliť, kedy brzdiť
protismerným výstrelom a či sa oplatí vrátiť pre kryštály, rozhoduje o tom,
či hráč doletí.

## Prehra

- **„out of air“**: došiel kyslík.
- **„crashed“**: náraz do planéty. Planéta je v kóde pripravená, v súčasných
  úrovniach však nie je.

Prehra nie je trest na dlho: klávesom R sa úroveň okamžite začne odznova.

## Jazyk a texty

Všetky texty v hre sú po anglicky, krátke a ukazujú sa veľkými písmenami:
„docked“, „out of air“, „too fast“, názvy úrovní „rocks“, „mines“, „holes“.
Píšu sa vlastným pixelovým písmom. Zámerne nie sú v hre žiadne vety ani nápovedy:
rozhranie ukazuje ikony a čísla.
