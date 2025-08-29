## Programátorská dokumentace

*Tady vysvětlete hlavní koncepty a strukturu vašeho programu. Pokud je váš program rozdělen do několika souborů, napište, k čemu každý soubor slouží a jakou funkcionalitu (třídy nebo funkce) v něm najdu. Pokud v programu používáte třídy, stručně popište význam těch nejdůležitějších (co mají za úkol).*

# High-level struktura programu:

Projekt je dekomponován do hlavního zdrojového kódu a do jeho testů.

Hlavní zdrojový kód pak obsahuje obecný Interface prefixového stromu trie a jeho očekávaných vlastností a jeho tři konkrétní implementace: TrieInFile, TrieInRAM, LinkedListRAMTrie

Každá z těchto implementací pak obsahuje i další metody, které buďto slouží jako pomocné k metodám z interface, nebo umožnují uživateli převod databáze mezi těmito jednotlivými typy Trie. Také obsahují konzolově-grafickou metodu ConsolePrint, která může sloužit k dalšímu ladění, či pochopení fungování knihovny.

Jednotlivé implementace jsou kompatibilní na úrovni základních charakteristik záznamů, jakými jsou délka záznamu a možné znaky klíče. Ve standartním režimu tyto třídy obsluhují 256 bytové záznamy, umožnující v klíči znaky anglické abecedy, číslice a mezeru, pričemž nerozlišuje velká a malá písmena. Pro případná data ke klíčům je zde vymezeno 107 bytů.

TrieInFile a TrieInRAM pak lze také využít v režimu BitWise, který jako přípustné znaky klíče bere pouze {0, 1}. Díky tomu dokážeme se stejnými 107 byty pro data snížit délku záznamu na 128 bytů. Pro LinkedListRAMTrie nedává BitWise přepínač konceptuálně smysl.

# TrieInFile
Třída obsluhující prefixový strom budovaný přímo v souboru.

Každý záznam (reprezentující prvních x znaků klíče), má fixní délku. V prvních několika bytech obsahuje na předem známých pozicích označujících následující znak čtyřbytové číslo, které určuje, kde hledat záznam, jehož klíč právě tímto znakem pokračuje.

Maximální velikost TrieInFile tedy (díky velikosti odkazů na syny) může být 4 294 967 296 záznamů neboli celkem 1 TiB velikost souboru.

Zároveň pak také takový záznam obsahuje indikační byte, který informuje, zda sám obsahuje data a případně daná data.

Díky fixní délce záznamu a předem známým pozicím informací o synech prvku můžeme využít standartně funkci paměti seek a průchod z otce na syna tak lze vykonat v konstantním čase. Dosáhneme tak hlavní devízy Trie, kterou je lineární složitost vyhledávání vůči délce klíče.

Jelikož záznam obsahuje absolutní polohu svých synů, nelze jednoduše mazat prvky z databáze. Metoda RemoveElement tak pouze deaktivuje daný záznam (skrze jeho indikační byte) a ponechá ho v databázi.
Pro přímé odstranění takových záznamů a nutnou úpravu všech dalších pak slouží funkce GarbageCollect. Ta prochází stromem a aktivní prvky přidává do nového stromu, načež smaže starý a nový přejmenuje. Může tedy vyžadovat až dvojnásobnou pamět na disku.

Ačkoliv Trie pracuje v souboru, vyžaduje běh knihovny samozřejmě i pamět RAM. Zejména metody, které vyžadují průchod stromem (RemoveBranch, BranchSize, AutoComplete, ConsolePrint a GarbageCollect), nutně obsahují frontu (popřípadě zásobník u ConsolePrint), která může nabýt značných velikostí, pokud je databáze velmi velká.

BitWise spínač změní v konstruktoru abecedu a fixní délku záznamu. Ostatní metody pracují vesměs stejně.

Nakonec obsahuje i velmi primitivní cache, která obsahuje poslední dotazované záznamy a jejich data. Její velikost volí uživatel při tvorbě trie a implicitně je stanovena na nula.

# TrieInRAM
Třída obsluhující prefixový strom budovaný skrze pole pointerů na další záznamy.

Obsahuje podtřídu záznam. Její instance obsahují odkazy na prvky následující danými písmeny a případně data (+ boolovskou promenou activated).

TrieInRAM obsahuje odkaz na kořen tohoto stromu (na záznam s klíčem "") a pak metody, kterými obsluhuje operace nad ním.

I zde není jednoduchou operací prvek přímo smazat. Metoda RemoveElement, tak prvek opět pouze deaktivuje.

GarbageCollect zde však funguje principiálně odlišně od TrieInFile. Nedochází zde ke kopírování aktivních prvků, ale k mazání všech větví, jejichž BranchSize (metoda vracející počet aktivních záznamů v podvětvi definované daným klícem), je nulová.
Oba algoritmy by měly stejný výsledek, ale v typickém použití není většina prvků smazána a rozhodl jsem se tedy použít tento, když je pro něj možnost.

TrieInRAM pak obsahuje i další metody, které slouží pro převod mezi jinými druhy prefixových stromů. Je tak jakýmsi mezičlánkem pro tento převod.
Těmito metodami jsou jmenovitě: SaveToFile, LoadFromFile, ConvertToLinkedListBased. Všechny tyto tři metody využívají metod tříd do kterých konverze probíhá.

