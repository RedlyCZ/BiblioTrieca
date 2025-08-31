# Informace o projektu a jeho vývoji

Tato část dokumentace popisuje významné body vývoje projektu, komentář autora, hodnocení a případné náměty na jeho rozšíření.

### Rejstříkové informace
* BiblioTrieca
* Knihovna pro tvorbu a obsluhu prefixových stromů
* Marek Pučejdl, 1. ročník MFF
* Letní semestr 2024/2025
* Programování 2 - NPRG031

## Volba algoritmů
Vzhledem k vytyčenému zadání úloha *nevyžadovala* zásadní dilemata při volbě algoritmů. 

V programu se na mnoha místech vyskytují průchody stromem DFS i BFS, které mají své specifické vlastnosti a tudíž si většinou nelze přímo vybírat. Krom nejnutnějších případů (metody ConsolePrint) jsem se snažil vyhnout rekurzivní variantě DFS a nahrazoval ji formou s knihovním zásobníkem.

U odstranění prvků z databáze jsem se pak nakonec ve všech případech uchýlil ke kaňkování s vlastním Garbage Collectorem.

## Komentář k průběhu vývoje
Vývoj je zaznamenán v Git. 

Probíhal víceméně bezproblémově, ačkoliv jsem se několikrát musel vracet k fundamentům fungování, jako například k zahrnutí mezery do možných znaků a tak i změně struktury záznamu.

Struktura a dekompozice byla od počátku dobře rozvrhnuta a nedocházelo tak k zásadnímu předělání.

## Zavrhnuté nápady ze zadání
Valná většina funkcí z původního nástinu zadání byla programu implementována, přičemž byla doplněna ještě o mnohé další. Přesto však zůstávají některé náměty neimplementované. Toto jsou ony, doplněny zdůvodněním.

* **Dekompozice velkých podstromů do vlastních souborů** - Tento nápad se ukázal konfliktní s potenciálními implementacemi Garbage Collectoru. Jeho reálný význam je pak pochybný a byl tak z projektu zcela vypuštěn.
* **Volba obecné abecedy** - Nakonec nebyl implementován, protože by přenášel přílišnou část zodpovědnosti na uživatele. Ten by totiž nejen volil abecedu, ale ještě by musel vhodným způsobem volit rozsah dat a stanovit tak velikost záznamů. Díky využití implicitních hodnot téměř u všech výpočtů je ale případná úprava poměrně jednoduchá.
* **Přehešování klíčů pro rovnoměrnější rozložení** - Zavrhnut víceméně pro nesmyslnost. Do programu by zbytečně vnášel značnou komplikovanost a využití by bylo velmi pochybné. Navíc do určité míry je účel nahrazen pomocí BitWise trie, do kterého lze všechny data převést a má tentenci být hustčí.

## Náměty k rozšíření

* Test srovnávající BiblioTriecu s jinými knihovními databázemi
* Implementace RadixTrie, jako další třídy trie
* Hledání nejbližších příbuzných klíčů, tj. AutoComplete, ale i nahoru

## Hodnocení vývoje
Vývoj jsem si velmi užil, neobsahoval přílíš ubíjejích částí. Zároveň byl algoritmicky poměrně jednoduchý, ale přesto ponechával možnost promyslet a vybrat správné řešení problémů. 
Rozsah úlohy byl spíše kratší než přílíš dlouhý, ale vzhledem k prozíravému zadání nebyl problém dále rozšiřovat.
Z hlediska osobního přínosu mi nejlepší přišla hned první třída, tudíž TrieInFile, protože obsahovala nejen práci se stromem, ale také tvorbu a binární zápis do souborů, což byla problematika, se kterou jsem se do té doby nesetkal, ale vlastně nebyla nikterak nemožně složitá.