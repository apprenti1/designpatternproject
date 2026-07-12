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
