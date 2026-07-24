# Hypothèses d'implémentation

Le [sujet](../readme.md) laisse certains points sous-spécifiés ou internes incohérents pour la phase naïve. Ce document trace les interprétations retenues, pour pouvoir les ajuster facilement si une clarification arrive (prof, retours de soutenance).

## Stock initial

Rien dans le sujet ne définit un stock de départ ni un moyen d'en ajouter avant l'instruction `RECEIVE` (§5.1.1, phase "modules complémentaires"). Sans ça, `PRODUCE` échouerait systématiquement dès le premier lancement.

**Décision** : `data/stock.seed.json` (suivi par git) contient un stock de départ généreux (10 de chaque pièce de base, 0 de chaque drone). `data/stock.json` (ignoré par git) est la copie mutable créée depuis le seed au premier lancement — c'est elle que `PRODUCE` met à jour. Ce mécanisme sera naturellement remplacé/complété par `RECEIVE` en phase modules complémentaires.

## Algorithme d'assemblage (`INSTRUCTIONS ARGS`)

L'exemple détaillé du sujet (§7.1, `INSTRUCTIONS 1 DXF-1`) contient des incohérences :

- La ligne `ASSEMBLE TMP2 Core_C3D1{System_S3D1}` ne respecte pas le format documenté ailleurs dans le sujet (`ASSEMBLE Résultat Piece1 Piece2`, deux composants attendus) : un seul composant y est listé.
- L'explication qui suit réutilise la ligne `ASSEMBLE [TMP2, Core_C3D1{System_S3D1}] Processor_P3D1` pour justifier deux affirmations contradictoires ("on ne nomme pas le résultat car sa composition est inconnue" *et* "on ne le nomme pas car c'est un assemblage connu, le DXF-1").
- Plus gênant : l'ordre littéral de l'exemple (Hull+Generator, puis +Move, puis +Core) viole la contrainte du sujet elle-même — *"Seul le générateur peut être monté dans la coque avant le module principal"* — puisque le module de déplacement y rejoint l'assemblage contenant la coque avant le module principal.

Ce sont vraisemblablement des artefacts d'extraction du document source (PDF → texte), pas des règles voulues.

**Décision** : `AssemblyPlanner` implémente un algorithme déterministe qui respecte les quatre contraintes explicites du §3.2.3, sans chercher à reproduire l'exemple caractère pour caractère :

1. Toutes les pièces sortent du stock avant tout usage (`GET_OUT_STOCK`).
2. Le système est installé sur le module principal avant qu'il ne soit assemblé.
3. `TMP1 = Coque + Générateur` (seule exception autorisée dans la coque avant le module principal).
4. `TMP2 = TMP1 + Module principal{Système}` (le module principal rejoint l'assemblage — plus aucune autre pièce ne doit toucher la coque avant lui, donc le module de déplacement attend ce point).
5. `TMP3 = TMP2 + Module de déplacement` (assemblé après que la coque a déjà été ajoutée — satisfait).
6. Assemblage final non nommé : `ASSEMBLE TMP3 Module de contrôle` (forme à deux arguments sans nom, §3.2.3), suivi de `FINISHED`.

## Sémantique de VERIFY / PRODUCE

Le sujet ne précise pas explicitement si la disponibilité se juge sur un stock de drones finis préexistant ou sur le stock de pièces nécessaires à l'assemblage.

**Décision** : `VERIFY`/`PRODUCE` évaluent la disponibilité sur le **stock de pièces** (agrégation façon `NEEDED_STOCKS`). `PRODUCE`, s'il réussit, consomme les pièces nécessaires et incrémente le compte de drones finis en stock du nombre produit (persistant dans `data/stock.json`). Un stock de pièces insuffisant renvoie une `ERROR` (cas "ne peut pas être produite", §3.2.5) plutôt qu'un `STOCK_UPDATED` partiel.

## Transport de l'API : HTTP au lieu de la console (phase 2)

Le sujet décrit littéralement une boucle console ("entrée textuelle en console", "sortie console",
§3.1/§3.4). Ce projet expose la même sémantique (mêmes instructions, mêmes formats d'entrée/sortie
ligne par ligne) via une API REST (`src/DroneFactory/Program.cs`, ASP.NET Core minimal API) plutôt
qu'une boucle `Console.ReadLine`, pour brancher un front HTML/JS (`index.html` à la racine).

**Décision** : déviation assumée sur le *transport* uniquement — la logique métier
(`InstructionHandler`) ignore totalement qu'elle est appelée depuis HTTP plutôt que depuis un flux
console (elle ne fait aucun appel à `Console`), donc le format d'entrée/sortie de chaque instruction
reste identique à ce que décrit le sujet. Le REPL console n'est plus l'exécutable produit par
`dotnet run --project src/DroneFactory`.

## Quelles pièces comptent pour les catégories de drones (§4.2)

Le sujet précise que "les modules principaux et de contrôle ne restreignent jamais la
catégorisation" mais reste vague sur ce que ça implique concrètement pour la lecture des tags
(F/M/L/S) utilisés par les 4 règles de catégorie.

**Décision** : seuls la coque, le générateur, le module de déplacement et le système installé
contribuent aux tags de catégorie ; le module principal et le module de contrôle sont ignorés,
même si leurs propres tags (2D/3D) existent par ailleurs pour la compatibilité système (cf.
section suivante). Cette lecture est nécessaire pour que le catalogue existant (§6.2) reste
cohérent : sans elle, `WDS-1` (Hull_HS1{S}, Generator_GS1{S}, Move_MS1{S}, System_S3D1{2D,3D})
n'appartiendrait à **aucune** catégorie, puisque son module principal `Core_C3D1` n'est jamais tagué
(S) — ce qui violerait la contrainte "un drone est obligé d'appartenir à une catégorie". Avec cette
lecture, `WDS-1` est bien Submersible (implémenté dans `Domain/Categories/CategoryRules.cs`, testé
dans `CategoryClassifierTests`).

## Compatibilité module principal / système / module de contrôle (ADD_TEMPLATE, §4.3)

Le sujet dit qu'"un module principal ne permet pas forcément l'installation de tous les systèmes"
et qu'"un module de contrôle doit être compatible avec le système installé", sans définir la règle
de compatibilité.

**Décision**, validée contre les 4 templates existants :

- **Module principal → système** : le module principal supporte le système si
  `système.Tags ⊆ moduleprincipal.Tags` (le module principal doit couvrir tous les tags requis par
  le système).
- **Module de contrôle → système** : compatible si `controle.Tags ∩ système.Tags ≠ ∅`
  (au moins un tag en commun). Un sous-ensemble strict (comme pour le module principal) est
  impossible à satisfaire avec le catalogue existant : `DXF-1` installe `System_S3D1{2D,3D}` sur
  `Processor_P3D1{3D}`, qui ne couvre pas (2D).

Implémenté dans `InstructionHandler.TryParseTemplate`.

## Sortie de succès de ADD_TEMPLATE (§4.3)

Le sujet spécifie le format d'erreur ("une tentative d'ajout ne les respectant pas devra renvoyer
une erreur claire et précise") mais pas le format de succès.

**Décision** : `TEMPLATE_ADDED {TEMPLATE_NAME}`, sur le même principe que `STOCK_UPDATED` pour
`PRODUCE`.

## Générateurs et modules de déplacement multiples (§5.1.2)

Le sujet autorise désormais jusqu'à 3 modules de déplacement et 2 générateurs, avec la règle
« ≥2 modules de déplacement ⇒ 2 générateurs obligatoires ». Plusieurs points restaient à trancher :

- **Catégorisation avec plusieurs pièces du même type** : les règles §4.2 parlent d'« un module de
  déplacement (X) ». **Décision** : lu comme "au moins un" (ANY) pour Aérien/Marin/Terrestre. Pour
  Submersible ("toutes les pièces sont de type (S)"), lu comme ALL — tous les générateurs et tous
  les modules de déplacement doivent porter le tag (S), en plus de la coque. Implémenté dans
  `Domain/Categories/CategoryRules.cs`.
- **`GET_OUT_STOCK` avec pièces dupliquées** : si un template a deux fois `Generator_GF1`, la sortie
  regroupe en une seule ligne `GET_OUT_STOCK 2 Generator_GF1` plutôt que deux lignes de quantité 1
  chacune — plus lisible et cohérent avec « sortir A exemplaires de la pièce Piece1 » (§3.2.3).
  Implémenté dans `Assembly/AssemblyPlanner.cs`. Même principe pour `NEEDED_STOCKS` (regroupement
  par pièce avant multiplication par la quantité commandée).
- **Ordre d'assemblage** : chaque générateur rejoint la coque à son tour (TMP1, TMP2, …) avant le
  module principal ; chaque module de déplacement rejoint ensuite l'assemblage à son tour — les
  quatre contraintes du §3.2.3 restent respectées quel que soit le nombre de pièces.
- La classification de pièces en slots (coque/module principal/générateurs/modules de
  déplacement/module de contrôle/système), utilisée par `ADD_TEMPLATE`, est désormais partagée avec
  la résolution des modificateurs de drone (§5.2.1, voir plus bas) via `Domain/DroneTemplateBuilder.cs`,
  pour ne pas dupliquer ces règles à deux endroits.

## RECEIVE (§5.1.1)

Le sujet ne précise ni la validation des éléments reçus, ni le format de succès.

**Décision** : `RECEIVE ARGS` accepte toute pièce (`PieceCatalog`), système (`SystemCatalog`) ou nom
de drone/template connu ; un élément inconnu renvoie une `ERROR` claire (cohérent avec la rigueur du
reste du système plutôt que d'accepter silencieusement n'importe quel nom). Succès :
`STOCK_UPDATED`, même convention que `PRODUCE`/`TRANSFER`.

## Modificateurs de drone WITH/WITHOUT/REPLACE (§5.2.1)

Plusieurs points d'interprétation :

- **`REPLACE B Piece1, C Piece2`** : lu comme une paire (retirer B exemplaires de Piece1, ajouter C
  exemplaires de Piece2), pas deux opérations indépendantes — cohérent avec l'exemple composé du
  sujet et avec la phrase « remplacer B exemplaires de Piece1 par C exemplaire de Piece2 ». Une liste
  `REPLACE` avec plus de deux entrées est donc lue comme plusieurs paires successives.
- **Ordre d'application** : les modificateurs (WITH/WITHOUT/REPLACE, éventuellement plusieurs sur la
  même entrée) sont appliqués dans l'ordre où ils apparaissent dans le texte, sur le sac de pièces du
  template de base (`DroneTemplate.RequiredPieces`, sans le système).
- **Validation du résultat** : une fois les modificateurs appliqués, le sac de pièces obtenu est
  reclassé en slots et revalidé exactement comme pour `ADD_TEMPLATE` (mêmes règles de compatibilité
  système/module principal/module de contrôle, mêmes règles de construction §5.1.2, même règle de
  catégorie) via `DroneTemplateBuilder.TryBuild` — un modificateur qui casse la structure du drone
  (ex : retirer la seule coque sans la remplacer) est donc rejeté avec une `ERROR` claire.
  Implémenté dans `Commands/DroneOrderParser.cs`.
- **Détection du mode `;`** : le séparateur devient `;` dès qu'un mot-clé WITH/WITHOUT/REPLACE ou un
  `;` apparaît dans `ARGS` ; sinon l'ancien format `,` (avec sommation des doublons, §3.1) reste
  utilisé tel quel, sans passer par la logique de modificateurs.
- **Pas de fusion par nom en mode `;`** : deux entrées du même drone dans une liste `;` ne sont pas
  sommées (contrairement au mode `,`), car elles peuvent porter des modificateurs différents et donc
  représenter des variantes différentes du même drone.
- Le drone modifié reste crédité/consommé en stock sous le nom du drone de base (pas de nom de
  variante distinct) — le sujet ne prévoit pas de mécanisme de nommage pour les drones modifiés.

## Gestion de commandes ORDER/SEND/LIST_ORDER (§5.2.2) — portée

`ORDER`/`SEND`/`LIST_ORDER` restent volontairement **globaux** (non rattachés à une usine précise) :
le sujet ne mentionne pas `IN` pour `ORDER`/`LIST_ORDER`, et une commande client n'a pas de raison
d'être liée à une usine de production particulière. `SEND`, en revanche, sort réellement du stock
(« il faudra ensuite envoyer (sortir du stock) ») donc accepte la précision `IN Usine1` comme les
autres instructions impactant le stock (§5.2.4), avec la même règle d'ambiguïté que `RECEIVE`/`VERIFY`
si plusieurs usines existent et qu'aucune n'est précisée.

## Traçabilité des flux GET_MOVEMENTS (§5.2.3)

Le sujet dit que l'instruction « renverra l'intégralité des instructions ayant eu un impact sur le
stock ». **Décision** : chaque ligne de sortie reproduit l'instruction utilisateur telle qu'exécutée
(`{INSTRUCTION} {ARGS}`, ex. `RECEIVE 5 Hull_HF1`), dans l'ordre chronologique. Seules les
instructions qui modifient effectivement le stock sont journalisées : `RECEIVE`, `PRODUCE`, `SEND`,
`TRANSFER` — pas `ORDER` (qui ne fait que réserver), ni les instructions de lecture. Une instruction
qui échoue (sortie commençant par `ERROR`) n'est pas journalisée. `ARGS` sur `GET_MOVEMENTS` est une
liste de noms d'éléments (pas nécessairement quantifiée, malgré la convention générale ARGS du
§3.1) — les deux formats (`Piece1` ou `2 Piece1`) sont acceptés en pratique. Implémenté via le pattern
Decorator (`LoggingInstruction`, voir `docs/DESIGN_PATTERNS.md`) plutôt que dans `InstructionHandler`,
pour ne pas mélanger la logique métier et la journalisation.

## Multi-usines TRANSFER/IN (§5.2.4) — portée et messages

- **Portée** : la précision `IN Usine1` et l'agrégation multi-usines couvrent le **stock**
  (`STOCKS`, `RECEIVE`, `PRODUCE`, `VERIFY`, `SEND`, `TRANSFER`). Les **templates** restent globaux
  (partagés entre toutes les usines) — le sujet se concentre sur le stock dans son exemple détaillé
  (§5.2.4 ne donne un exemple que pour `PRODUCE`/`GET_STOCKS`), et dupliquer les templates par usine
  aurait démesurément alourdi ce module par rapport à sa description. De même, `ORDER`/`LIST_ORDER`
  restent globaux (voir section précédente).
- **Ensemble d'usines** : fixé au démarrage (`Program.cs`), deux usines de démonstration `Usine1`
  (stock historique, `data/stock.seed.json`) et `Usine2` (`data/stock.usine2.seed.json`) — le sujet
  ne décrit pas d'instruction de création d'usine.
- **Message d'usine manquante** : reproduit littéralement l'exemple du sujet pour `PRODUCE`
  (`ERROR Missing target factory. Available factory for this instruction are Usine1 and Usine3`),
  en ne listant que les usines dont le stock est suffisant pour la commande. Si aucune usine ne
  suffit, `ERROR Insufficient stock to produce this order in any factory` (le sujet ne couvre pas ce
  cas). Pour les autres instructions (`RECEIVE`, `VERIFY`, `SEND`), dont la "validité" ne se limite
  pas à une question de stock suffisant au même sens, le message liste **toutes** les usines
  disponibles sans filtrage.
- **`STOCKS` sans `IN`** : agrège les quantités de toutes les usines (« renvoyant alors l'intégralité
  du stock de toutes les usines confondues », §5.2.4). Avec une seule usine (tests, constructeur
  historique d'`InstructionHandler`), la précision `IN` n'est jamais nécessaire — aucune ambiguïté
  ne peut survenir avec un seul candidat.
- **Sortie de succès de `TRANSFER`** : non spécifiée par le sujet — `STOCK_UPDATED`, même convention
  que `PRODUCE`/`RECEIVE`.
