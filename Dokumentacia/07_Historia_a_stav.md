# 7. Ako hra vznikala a čo ešte chýba

## Ako hra vznikala

Hra vznikala počas jamu v krokoch: po každom kroku sa hrala, tím povedal, čo nefunguje,
a podľa toho sa upravila. Prácu programátora a výtvarníčky táto verzia
nemenila, iba z nej vychádzala.

### 13. 11.

- **Pôvodný nápad:** astronaut odnesený od lode. Nevie sa pohybovať, v rukách má zbraň
  a spätný ráz ho posúva. Kyslík ubúda časom aj výstrelmi, kamene zavadzajú, fľaše
  s kyslíkom ležia mimo priamej cesty, planéty priťahujú. Plán bol šesť krátkych úrovní.
- **Základ hry:** pohyb na spätný ráz, kamene, fľaše, hranice úrovne, kompas k lodi,
  pristátie iba pri malej rýchlosti.
- **Pozadie:** hmlovina, cez ktorú sa letí (vrstvy s paralaxou), a hviezdy z 3D modelu
  výtvarníčky. Hviezdy najprv pôsobili ploché, preto dostali farebnú svätožiaru.
- **Kamene od výtvarníčky:** 3D modely premenené na 12 obrázkov z rôznych uhlov.
- **Meteory:** rýchle kamene zhora s výstrahou na okraji obrazovky.
- **Skúška „ničí, aby si dýchal“:** pôvodne bola streľba len výdavok. Podľa vzoru
  úspešných hier na spätný ráz sa pridali kryštály kyslíka zo zničených kameňov a séria.
  Skúška sa ujala a stala sa jadrom hry.

### 14. 11.

- **Rozhodnutie tímu:** hráč je loď, nie astronaut. Raketa s dvoma kývajúcimi sa
  zbraňami sa vyskúšala, ale neujala sa. Zostal zástupný obrázok.
- **Témy úrovní:** každá úroveň dostala vlastné farby, oblaky, hviezdy a objekt v diaľke.
  Trosky stanice v pozadí boli najprv plochý obrázok, potom 3D render stmavený a rozmazaný,
  aby pôsobili ďaleko.
- **Míny:** prvé spiace míny, ktoré sa zobudia a plávajú za loďou.
- **Trhliny v trupe namiesto životov** (nápad Filipa): tvrdý náraz prederaví trup,
  vzduch uniká rýchlejšie. Fľaša zaplátá jednu trhlinu.
- **Skúšobný build** pre Windows priamo z menu.
- **Gravitácia:** planéta, ktorá ťahá loď aj strely (kód zostal, v úrovniach zatiaľ nie je).
- **Tri dlhé úrovne namiesto štyroch krátkych** (rozhodnutie Filipa a výtvarníčky),
  každá sa hrá inak. Kontrolné body sa vyskúšali a zrušili, R vždy začína úroveň odznova.
- **Ladenie podľa hrania:**
  - meteory rýchlejšie, mieria s predstihom a chodia aj v rojoch;
  - lovci rýchlejší a citlivejší;
  - prstenec mín okolo fľaše sa rozoberá po jednej;
  - pribudla ťažká mína;
  - čierne diery ťahajú silnejšie a okolo nich krúžia kamene;
  - pád do čiernej diery už nezabíja, je to červia diera;
  - bodkovaný okraj poľa diery nahradila mäkká žiara.
- **Balans vzduchu:** kyslík z kameňov 2 → 3,5 → 2,8 na kryštál, z mín viac.
- **Rozhranie bez „strojového“ dojmu:** najprv kratšie a tichšie texty, potom úplne nové
  rozhranie: vlastné pixelové písmo a ikony, žiadne nápovedy, šípka k lodi pri okraji
  obrazovky, krúžok pri pristávaní, koniec úrovne s klávesom v rámčeku.
- **Prechod na štruktúru programátora:** hra sa už nestavia kódom pri spustení,
  ale z prefabov a scény; čísla balansu sú v assete `Tuning`. Celá doterajšia práca sa
  preniesla do novej štruktúry a overila sa automatickým testom na všetkých troch úrovniach.
- **Loď hráča od výtvarníčky:** 3D model (`spaceship.fbx`) obsahuje dve lode. Malá
  s dvoma zbraňami pri motore je hotová aj s textúrami a bola zapečená do obrázka hráča:
  loď sa otáča zadnou časťou (motorom a zbraňami) ku kurzoru, zbrane strieľajú striedavo a spätný ráz ju tlačí nosom
  dopredu. Na žiadosť tímu bola zatiaľ vrátená späť, uložená je pod značkou
  `ship-player-backup`. Veľká materská loď z toho istého súboru zatiaľ textúry nemá.

## Čo je hotové

- pohyb na spätný ráz, streľba, kyslík, kryštály, séria;
- kamene troch veľkostí s rozpadom, meteory a roje;
- päť druhov mín a reťazové výbuchy;
- čierne diery s ťahom, obežnými kameňmi a červími dierami;
- trhliny v trupe s prúdom vzduchu, fľaše, ktoré ich plátajú;
- pristátie s krúžkom a šípka k lodi;
- tri úrovne s vlastnými témami a pozadiami;
- rozhranie s pixelovým písmom a koniec úrovne;
- reštart a prechod medzi úrovňami bez načítania scény;
- skúšobný build pre Windows;
- generovanie prefabov a pečenie grafiky z 3D modelov priamo v Unity.

## Čo ešte chýba alebo je len rozpracované

- **Zvuk a hudba**: zatiaľ nie sú žiadne.
- **Efekty**: výbuchy sú zatiaľ jednoduchá žiara, chýba napr. záblesk pri výstrele.
- **Hodnotenie hviezdičkami** na konci úrovne: prahy už sú v súboroch úrovní
  (`# stars=45,20`), zobrazenie ešte nie je.
- **„Posledný nádych“**: krátka núdzová rezerva, keď kyslík klesne na nulu.
- **Prepojenie úvodného menu programátora s hrou**: menu načíta ďalšiu scénu v Build
  Settings, scéna `DraftScene` tam zatiaľ nie je.
- **Grafika od výtvarníčky**: loď hráča je pripravená (pozri vyššie), materská loď ako cieľ
  pristátia čaká na textúry, model míny ešte nie je.
- **Planéta v úrovniach**: kód je hotový, žiadna úroveň ju zatiaľ nepoužíva.
- **Testovanie úrovní hráčmi a doladenie balansu**: úrovne sú rozmiestnené bez dlhého hrania.
- **Finálny build a zlúčenie do `main`**: iba so súhlasom tímu.
