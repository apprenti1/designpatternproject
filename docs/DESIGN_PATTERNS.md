# Design patterns

Ce document recense les design patterns mis en place dans `DroneFactory`, pourquoi ils ont été choisis, et où les retrouver dans le code. Il sert de base à la soutenance (§2.4 du [sujet](../readme.md)) : présenter les choix de patterns **et** ceux qui ont été envisagés puis écartés.

## Sommaire

- [Strategy — règles de catégorie de drone](#strategy--règles-de-catégorie-de-drone)
- [Repository — persistance du stock et des templates](#repository--persistance-du-stock-et-des-templates)
- [Command — dispatch des instructions](#command--dispatch-des-instructions)

## Strategy — règles de catégorie de drone

- **Catégorie** : Comportemental
- **Où** : `src/DroneFactory/Domain/Categories/` (`ICategoryRule`, `AerienRule`/`MarinRule`/`TerrestreRule`/`SubmersibleRule`, `CategoryClassifier`)

**Problème résolu**
Le sujet (§4.2) définit 4 catégories de drones, chacune avec sa propre combinaison de tags à vérifier (mouvement, coque, système). Une nouvelle catégorie peut arriver dans les modules complémentaires listés sur MyGES. Un classement écrit en un seul bloc de conditions (`if`/`switch`) mélangerait les règles des 4 catégories dans une seule méthode, et toute catégorie supplémentaire obligerait à modifier ce bloc central.

**Fonctionnement**
Chaque catégorie est une classe implémentant `ICategoryRule` (`Category` + `Matches(DroneTemplate)`), qui ne connaît que sa propre règle. `CategoryClassifier.Classify` applique toutes les règles enregistrées et combine les résultats en un `[Flags] enum DroneCategory`. Ajouter une catégorie = ajouter une classe et l'enregistrer dans `CategoryClassifier`, sans toucher aux règles existantes (OCP). Utilisé par `ADD_TEMPLATE` pour rejeter un template n'appartenant à aucune catégorie (§4.2 : "un drone est obligé d'appartenir à une de ces catégories").

**Alternatives envisagées**
- Un `switch`/enchaînement de `if` unique calculant les 4 catégories en une seule méthode : rejeté, viole l'OCP dès qu'une 5e catégorie apparaît (modules MyGES) et complique les tests unitaires isolés par catégorie.
- Une propriété calculée directement sur `DroneTemplate` : rejeté pour garder `DroneTemplate` comme un simple porteur de données (record), sans dépendance vers les catalogues de pièces nécessaires à la résolution des tags.

## Repository — persistance du stock et des templates

- **Catégorie** : Structurel
- **Où** : `src/DroneFactory/Storage/IStockRepository.cs` (implémenté par `StockStore`), `ITemplateRepository.cs` (implémenté par `TemplateStore`)

**Problème résolu**
`InstructionHandler` a besoin de lire/écrire le stock et les templates sans connaître le détail de leur persistance (fichiers JSON aujourd'hui). Formaliser cette frontière permet de tester `InstructionHandler` sans dépendre du système de fichiers réel, et anticipe le module multi-usines (§5.2.4, phase 3) où chaque usine aura sa propre paire de repositories, sans changer le code appelant.

**Fonctionnement**
`IStockRepository` (`GetQuantity`/`HasAtLeast`/`Consume`/`Add`/`Save`) et `ITemplateRepository` (`All`/`Find`/`Add`) sont injectés dans `InstructionHandler` via son constructeur. `StockStore` et `TemplateStore` sont les implémentations JSON actuelles (seed + fichier mutable gitignoré, `data/stock.json` et `data/templates.json`), enregistrées dans le conteneur DI (`Program.cs`) comme singletons derrière ces interfaces.

**Alternatives envisagées**
- Garder `StockStore`/le catalogue statique tels quels, sans interface : rejeté, ça couple `InstructionHandler` à une implémentation JSON précise et empêche d'avoir plusieurs usines avec des stocks indépendants sans dupliquer la classe.
- Un unique repository générique `IRepository<T>` : rejeté, le stock (quantités agrégées par nom) et les templates (entités nommées) ont des opérations trop différentes (`Consume`/`HasAtLeast` n'ont pas de sens pour un template) pour partager une interface utile.

## Command — dispatch des instructions

- **Catégorie** : Comportemental
- **Où** : `src/DroneFactory/Commands/Instructions/` (`IInstruction` + une classe par instruction), `InstructionRegistry.cs`

**Problème résolu**
Le nombre d'instructions utilisateur grossit à chaque phase (5 en phase 1, +1 avec `ADD_TEMPLATE`, +4 prévues en phase 3). Un `switch` central (comme celui qu'on avait en phase 1 dans `Program.cs`) mélange le dispatch HTTP et la logique métier, et devient de moins en moins lisible à mesure que la liste s'allonge.

**Fonctionnement**
Chaque instruction est une classe implémentant `IInstruction` (`Name`, `Execute(string args)`), qui délègue à une méthode d'`InstructionHandler` (qui garde toute la logique métier et sa couverture de tests existante — le Command reste une coquille fine). `InstructionRegistry` les indexe par nom et expose `TryGet`. Les routes de l'API (`Program.cs`) passent toutes par `registry.TryGet(nom, ...).Execute(args)` plutôt que d'appeler `InstructionHandler` directement, donc ajouter une instruction ne touche qu'un nouveau fichier + une ligne d'enregistrement dans le registre. Prépare directement la traçabilité des flux (§5.2.3, phase 3) : un décorateur `LoggingInstruction : IInstruction` pourra envelopper n'importe quelle commande impactant le stock sans modifier les commandes elles-mêmes.

**Alternatives envisagées**
- Garder le `switch` de dispatch dans `Program.cs` : rejeté, ne scale pas au-delà de quelques instructions et empêche d'observer/décorer l'exécution d'une instruction de façon uniforme (utile pour `GET_MOVEMENTS`).
- Fusionner `IInstruction` et `InstructionHandler` (mettre toute la logique dans les classes Command) : rejeté pour cette passe, ça aurait cassé/dupliqué toute la suite de tests déjà écrite contre `InstructionHandler`.
