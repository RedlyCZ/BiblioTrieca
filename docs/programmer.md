# Programátorská dokumentace

BiblioTrieca je knihovna umožnující tvorbu a obsluhu databáze založené na několika druzích prefixových stromů.
Tato část dokumentace slouží zejména k pochopení programu a případně k informování těch, kteří by jej jakožto knihovnu chtěli aplikovat.

## High-level struktura programu:

Projekt je dekomponován do hlavního zdrojového kódu a do jeho testů.

Hlavní zdrojový kód pak obsahuje obecný Interface prefixového stromu trie - **TrieDatabase** a jeho tři konkrétní implementace: **TrieInFile**, **TrieInRAM**, **LinkedListRAMTrie**

Každá z těchto implementací pak obsahuje i další metody, které buďto slouží jako pomocné k metodám z interface, nebo umožnují uživateli převod databáze mezi těmito jednotlivými typy Trie. Také obsahují konzolově-grafickou metodu *ConsolePrint*, která může sloužit k dalšímu ladění, či pochopení fungování knihovny.

Jednotlivé implementace jsou kompatibilní na úrovni základních charakteristik záznamů, jakými jsou délka záznamu a možné znaky klíče. Ve standartním režimu tyto třídy obsluhují *256 bytové záznamy*, umožnující v klíči znaky *anglické abecedy, číslice a mezeru*, pričemž *nerozlišuje* velká a malá písmena. Pro případná data ke klíčům je zde vymezeno *107 bytů*.

TrieInFile a TrieInRAM pak lze také využít v režimu **BitWise**, který jako přípustné znaky klíče bere pouze {0, 1}. Díky tomu dokážeme se stejnými 107 byty pro data snížit délku záznamu na *128 bytů*. Pro LinkedListRAMTrie nedává BitWise přepínač konceptuálně smysl.

## TrieDatabase - Co očekávat od metod?

* *AddElement(key, data, replace)* - Přidá prvek s daným klíčem a daty do trie, pokud je třeba, tak si vytvoří strukturální prvky které na něj vedou. Spínač replace, implicitně nastaven na true, rozhoduje, zda pokud již prvek v databázi je, bude přepsán.
* *ReadElement(key)* - Vrátí data daného prvku, pokud je v databázi a null jinak.
* *RemoveElement(key)* - Vymaže daný prvek z databáze. (podrobnosti v konkrétních implementacích)
* *RemoveBranch(key)* - Vymaže daný prvek a všechny jeho syny z databáze. (tudíž prvky jejichž klíč začíná znaky tohoto klíče)
* *BranchSize(key)* - Vrátí počet aktivních (data majících) prvků v podstromě definovaném daným klíčem.
* *AutoComplete(key, numberOfCompletions)* - Vrátí pole dané velikosti klíčů, které jsou nejbližšími možnými doplněními daného klíče.

## TrieInFile
Třída obsluhující prefixový strom budovaný přímo **v souboru**.

Každý záznam (reprezentující prvních x znaků klíče), má fixní délku. V prvních několika bytech obsahuje na předem známých pozicích označujících následující znak *čtyřbytové číslo*, které určuje, kde hledat záznam, jehož klíč právě tímto znakem pokračuje.

*Maximální velikost* TrieInFile tedy (díky velikosti odkazů na syny) může být **4 294 967 296 záznamů** neboli celkem **1 TiB** velikost souboru.

Zároveň pak také takový záznam obsahuje indikační byte, který informuje, zda sám obsahuje data a případně daná data.

Díky *fixní délce záznamu* a předem známým pozicím informací o synech prvku můžeme využít standartně funkci paměti *seek* a průchod z otce na syna tak lze vykonat *v konstantním čase*. Dosáhneme tak hlavní devízy Trie, kterou je **lineární složitost vyhledávání vůči délce klíče**.

Jelikož záznam obsahuje absolutní polohu svých synů, **nelze** jednoduše mazat prvky z databáze. Metoda RemoveElement tak pouze *deaktivuje* daný záznam (skrze jeho indikační byte) a ponechá ho v databázi.
Pro *přímé odstranění* takových záznamů a nutnou úpravu všech dalších pak slouží funkce *GarbageCollect*. Ta prochází stromem a aktivní prvky přidává do nového stromu, načež smaže starý a nový přejmenuje. Může tedy vyžadovat až dvojnásobnou pamět na disku.

Ačkoliv Trie pracuje v souboru, vyžaduje běh knihovny samozřejmě i pamět RAM. Zejména metody, které vyžadují průchod stromem (RemoveBranch, BranchSize, AutoComplete, ConsolePrint a GarbageCollect), nutně obsahují *frontu* (popřípadě *zásobník* u ConsolePrint), která může nabýt značných velikostí, pokud je databáze velmi velká.

**BitWise** spínač změní v konstruktoru abecedu a fixní délku záznamu (z 256 B na 128 B). Ostatní metody pracují vesměs stejně.

Nakonec obsahuje i velmi primitivní *cache*, která obsahuje poslední dotazované záznamy a jejich data. Její velikost volí uživatel při tvorbě trie a *implicitně je stanovena na nula*.

## TrieInRAM
Třída obsluhující prefixový strom budovaný skrze **pole pointerů** na další záznamy.

Obsahuje *podtřídu záznam*. Její instance obsahují pole odkazů na prvky následující danými znaky a případně data (+ boolovskou proměnnou activated).

TrieInRAM obsahuje odkaz na kořen tohoto stromu (na záznam s klíčem "") a pak metody, kterými obsluhuje operace nad ním.

I zde není jednoduchou operací prvek přímo smazat. Metoda *RemoveElement*, tak prvek opět **pouze deaktivuje**.

*GarbageCollect* zde však funguje principiálně odlišně od TrieInFile. Nedochází zde ke kopírování aktivních prvků, ale k mazání všech větví, jejichž BranchSize (metoda vracející počet aktivních záznamů v podvětvi definované daným klícem), je nulová.
Oba algoritmy by měly stejný výsledek, ale v typickém použití *není* většina prvků smazána a rozhodl jsem se tedy použít tento, když je pro něj možnost.

TrieInRAM pak obsahuje i další metody, které slouží pro převod mezi jinými druhy prefixových stromů. Je tak jakýmsi mezičlánkem pro tento převod.
Těmito metodami jsou jmenovitě: *SaveToFile, LoadFromFile, ConvertToLinkedListBased*. Všechny tyto tři metody využívají metod tříd do (popřípadě z) kterých konverze probíhá.

## LinkedListRAMTrie
Třída obsluhující prefixový strom budovaný skrze **spojový seznam pointerů** na další záznamy.

Obsahuje *podtřídu záznam*. Její instance obsahují spojový seznam odkazů na prvky následující danými znaky a případně data (+ booleovskou proměnnou activated).

Z vlastností trie založených na spojových seznamech **je obecně pomalejší než TrieInRAM**, ale zato, zejména *u řídkých stromů*, **významně spoří paměťový prostor**.

*Kvůli časové složitosti* metody RemoveElement zde dochází pouze ke **kaňkování "smazaných" prvků**. Vlastní *skutečné smazání* umožňuje až metoda *GarbageCollect*.

Krom standartních metod obsahuje navíc metodu *ConvertToArrayBased*, která vrací strom TrieInRAM se stejnými daty.

## Testy
V projektu jsou zahrnuty testy, které zároveň slouží jako vzorové ukázky použití metod knihovny.

Program testů je dekomponován do 5 skupin (metod), které vždy náleží k jednomu z hlavních druhů trie:
* TrieInFileTests - 10 testů
* TrieInRAMTests - 9 testů
* LinkedListRAMTrieTests - 7 testů
* TrieInFileBitWiseTests - 4 testy
* TrieInRAMBitWiseTests - 7 testů

Navíc je zde jedna metoda pro názornější výpis výsledků testů a metoda pro smazání souborů vzniklých v důsledku testů.

Ve stavu kódu, jaký se nachází v master branchi všechny testy prochází.

