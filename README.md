# Zápočtový program: BiblioTrieca

## Specifikace

Databázová knihovna obsluhující prefixové stromy (trie) a poskytující vývojářům solidní základ pro jejich další využití. Umožní jejich tvorbu, čtení i modifikaci, přičemž tyto úkony provádí efektivně.

Obsahuje tři hlavní druhy prefixových stromů: prefixový strom ukládaný v souboru - TrieInFile, prefixový strom ukládaný v paměti - TrieInRAM a prefixový strom, vhodnější pro řidčí data založený na spojových seznamech LinkedListRAMTrie.
Každá z těchto tříd pak obsahuje všechny podstatné metody pro jejich využití, jakož i pro převod mezi sebou.

Nedílnou součástí zápočtového projektu je pak také poměrně obsáhlá sada testů, které mimojiné ukazují vzorové využítí a fungování této knihovny.
Právě kód testů je také tou částí projektu, kterou má smysl spouštět.

## Instalace a spuštění

Pro spuštění přejděte do složky `Tests` a spusťte příkaz `dotnet run`. Pro správné fungování je kritická závislost na programu Bibliotrieca ve složce Bibliotrieca, kterou program Tests využívá jako knihovní, a která je těžištěm zápočtové práce.

## Dokumentace

Ačkoliv komentáře přímo v kódu jsou psané v anglickém jazyce, tak níže přiložená dokumentace je, pro jednoduchost na obou stranách, v češtině.

* [Uživatelská dokumentace](docs/user.md) - pro užití knihovny
* [Programátorská dokumentace](docs/programmer.md) - pro další vývoj knihovny
* [Projektová dokumentace](docs/project.md) - pro informace o vývoji