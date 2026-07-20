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
