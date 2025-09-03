# Uživatelská dokumentace

Dokumentace obsahující základní popis knihovny, informace o jejím použití, očekávané fungování a složitost jednotlivých metod.

## Obecné informace

Databáze z knihovny využijete vytvořením instancí jejích tříd: `TrieInFile`, `TrieInRAM`, `LinkedListRAMTrie`.

Při vytváření instance pak u `TrieInFile` musíte zadat adresu souboru, ve kterém bude knihovna pracovat.

Pokud daný soubor už existuje, pak nebude přepsán a bude interpretován jako soubor trie.

U `TrieInFile` a `TrieInRAM` pak lze při vytváření parametrem ovlivnit, zda bude daná třída fungovat v režimu BitWise. `LinkedListRAMTrie` tento režim nepodporuje.

Specifikem `TrieInFile` je také volitelná cache, jejíž velikost se také nastavuje při vytváření instance. Implicitně je vypnutá.

*Maximální velikost* `TrieInFile` může být **4 294 967 296 záznamů** neboli celkem **1 TiB** velikost souboru. Velikosti `TrieInRAM` a `LinkedListRAMTrie` nejsou principielně omezeny.

## Společné metody všech tříd
* *AddElement(key, data, replace)* - Přidá prvek s daným klíčem a daty do trie, pokud je třeba, tak si vytvoří strukturální prvky které na něj vedou. Spínač replace, implicitně nastaven na true, rozhoduje, zda pokud již prvek v databázi je, bude přepsán. Očekávaná **složitost lineární k délce klíče**.
* *ReadElement(key)* - Vrátí data daného prvku, pokud je v databázi a null jinak. Očekávaná **složitost lineární k délce klíče**.
* *RemoveElement(key)* - Vymaže daný prvek z databáze. (podrobnosti v konkrétních implementacích) Očekávaná **složitost lineární k délce klíče**.
* *RemoveBranch(key)* - Vymaže daný prvek a všechny jeho syny z databáze. (tudíž prvky jejichž klíč začíná znaky tohoto klíče) Očekávaná **složitost lineární k velikosti odstraňované větve**.
* *BranchSize(key)* - Vrátí počet aktivních (data majících) prvků v podstromě definovaném daným klíčem. Očekávaná **složitost lineární k velikosti zkoumané větve**
* *AutoComplete(key, numberOfCompletions)* - Vrátí pole dané velikosti klíčů, které jsou nejbližšími možnými doplněními daného klíče. Očekávaná **složitost lineární k počtu doplňovaných slov**.
* *ConsolePrint(key, depth)* - Konzolově zobrazí strukturu podstromu definovaného daným klíčem. Očekávaná **složitost lineární velikosti vypisované větve**.
* *GarbageCollector()* - Odstraní z trie neaktivní větve, které vznikly důsledkem používání *RemoveElement*. Tím dojde k uvolnění paměti. Očekávaná **složitost lineární k velikosti trie**.

## Metody specifické pro dané třídy

### TrieInRAM
* *SaveToFile(adress, garbageCollect)* - Uloží danou TrieInRAM do souboru kompatibilním s TrieInFile. Očekávaná **složitost lineární k velikosti trie**.
* *LoadFromFile(adress)* - Načte trie ze souboru. Očekávaná **složitost lineární k velikosti trie**.
* *ConvertToLinkedListBased(garbageCollect)* - Vrátí instanci třídy LinkedListRAMTrie se stejnými prvky, jako má současná instance TrieInRAM. Očekávaná **složitost lineární k velikosti trie**.

### LinkedListRAMTrie
* *ConvertToArrayBased(garbageCollect)* - Vrátí instanci třídy TrieInRAM se stejnými prvky, jako má současná instance LinkedListRAMTrie. Očekávaná **složitost lineární k velikosti trie**.

## Demonstrace použití knihovny
```
TrieInFile database = new TrieInFile("adress");
database.AddElement("key1", [0xAE, 0x59, 0x48, 0xF5, 0x2A]);
database.AddElement("A5", [100, 15, 4]);
byte[] data1 = database.ReadElement("A5");

TrieInRAM secondDatabase = new TrieInRAM();
secondDatabase.LoadFromFile("adress");
secondDatabase2.RemoveElement("A5");
byte[] data2 = secondDatabase.ReadElement("key1");

LinkedListRAMTrie thirdDB = secondDatabase.ConvertToLinkedListBased(false);
thirdDB.GarbageCollect();
thirdDB.ConsolePrint();
thirdDB.RemoveBranch("");
```

Vzorové použití všech metod knihovny se pak nachází v rámci testů knihovny.


