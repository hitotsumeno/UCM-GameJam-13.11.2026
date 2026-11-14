# Dokumentácia hry

Hra pre **UCM GameJam (13. – 15. 11. 2026)**. Povinná mechanika jamu: **streľba**.
Žáner: 2D vesmírna hra s pohľadom zhora. Hráč sa nevie pohybovať sám,
posúva ho iba spätný ráz vlastných výstrelov.

Táto dokumentácia opisuje stav vetvy `filip/draft`, teda verziu hry postavenú
na štruktúre projektu, ktorú navrhol programátor vo vetve `main`.
Súbory v tomto priečinku sú len texty: hru nemenia a Unity ich nenačítava.

## Obsah

1. [O hre](01_O_hre.md): príbeh, cieľ, ovládanie, hlavná myšlienka.
2. [Herné mechaniky](02_Mechaniky.md): kyslík, streľba, kryštály, séria, trhliny
   v trupe, kamene, meteory, míny, čierne diery, pristátie. Všetko aj s číslami.
3. [Úrovne](03_Urovne.md): tri úrovne, ako sa prechádzajú a ako je zapísaný súbor úrovne.
4. [Vizuál a rozhranie](04_Vizual_a_rozhranie.md): obrazovka hry, písmo, ikony,
   pozadia, grafika a spôsob, akým sa pripravuje.
5. [Technické riešenie](05_Technicke_riesenie.md): Unity, štruktúra projektu,
   skripty, prefaby, nastavenia, nástroje.
6. [Návody](06_Navody.md): ako zmeniť čísla, úroveň, obrázky, texty a ako spraviť skúšobný build.
7. [Ako hra vznikala a čo ešte chýba](07_Historia_a_stav.md).

## V skratke

- **Myšlienka:** malá loď nemá palivo. Letí iba vďaka spätnému rázu svojej zbrane.
  Každý výstrel ju odtlačí opačným smerom.
- **Vzduch:** kyslík ubúda neustále a s každým výstrelom. Keď dôjde, hra sa končí.
- **„Ničí, aby si dýchal“:** zničené kamene a míny zanechávajú kryštály kyslíka.
  Streľba je teda pohon, zbraň aj zdroj vzduchu zároveň.
- **Bez životov:** tvrdý náraz prederaví trup a vzduch začne unikať rýchlejšie.
- **Cieľ úrovne:** doletieť k cieľovej lodi a pomaly sa k nej pripojiť.
- **Tri úrovne**, každá sa hrá inak: kamene a meteory, mínové pole, čierne diery.
- Všetky texty v hre sú po anglicky, napísané vlastným pixelovým písmom.

## Ako hru spustiť v Unity

1. Otvoriť projekt v Unity 6 (6000.0.85f2).
2. Otvoriť scénu `Assets/_Scenes/DraftScene.unity`.
3. Stlačiť **Play**.
