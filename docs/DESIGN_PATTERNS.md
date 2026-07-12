# Design patterns

Ce document recense les design patterns mis en place dans `DroneFactory`, pourquoi ils ont été choisis, et où les retrouver dans le code. Il sert de base à la soutenance (§2.4 du [sujet](../readme.md)) : présenter les choix de patterns **et** ceux qui ont été envisagés puis écartés.

## État actuel

Le projet en est à la **phase 1 (implémentation naïve)** — voir [readme.md §2.1](../readme.md#21-implémentation-naïve-dune-solution). Aucun design pattern n'est encore volontairement mis en place : c'est attendu à ce stade, les patterns arrivent en phase 2.

Ce fichier sera complété au fil de la phase 2, un pattern à la fois, en suivant le modèle ci-dessous. Supprimez cette section « État actuel » une fois le premier pattern documenté.

## Modèle pour chaque pattern

Dupliquez ce squelette pour chaque pattern ajouté, et mettez à jour le sommaire.

```markdown
### Nom du pattern

- **Catégorie** : Créationnel / Structurel / Comportemental
- **Où** : `src/DroneFactory/...` (fichiers, classes, interfaces concernées)

**Problème résolu**
Quelle contrainte du sujet (readme.md) ou quelle douleur du code naïf a motivé ce choix.

**Fonctionnement**
Comment le pattern est concrètement implémenté ici (rôles des classes/interfaces, qui appelle quoi).

**Alternatives envisagées**
Ce qui a été considéré à la place, et pourquoi ça a été écarté (même si retenu ailleurs).
```

## Sommaire

_(à remplir : un lien par pattern documenté, ex. `- [Factory Method](#factory-method)`)_
