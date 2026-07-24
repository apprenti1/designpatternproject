# Design patterns

Ce document recense les design patterns mis en place dans `DroneFactory`, pourquoi ils ont été choisis, et où les retrouver dans le code. Il sert de base à la soutenance (§2.4 du [sujet](../readme.md)) : présenter les choix de patterns **et** ceux qui ont été envisagés puis écartés.

## Sommaire

- [Strategy — règles de catégorie de drone](#strategy--règles-de-catégorie-de-drone)
- [Repository — persistance du stock, des templates, des commandes et des usines](#repository--persistance-du-stock-des-templates-des-commandes-et-des-usines)
- [Command — dispatch des instructions](#command--dispatch-des-instructions)
- [Decorator — traçabilité des mouvements de stock](#decorator--traçabilité-des-mouvements-de-stock)

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

## Repository — persistance du stock, des templates, des commandes et des usines

- **Catégorie** : Structurel
- **Où** : `src/DroneFactory/Storage/IStockRepository.cs` (implémenté par `StockStore`), `ITemplateRepository.cs` (`TemplateStore`), `IOrderRepository.cs` (`OrderStore`), `IMovementRepository.cs` (`MovementStore`), `IFactoryRegistry.cs` (`FactoryStore`)

**Problème résolu**
`InstructionHandler` a besoin de lire/écrire le stock, les templates, les commandes et l'historique des mouvements sans connaître le détail de leur persistance (fichiers JSON aujourd'hui). Formaliser cette frontière permet de tester `InstructionHandler` sans dépendre du système de fichiers réel. C'est aussi ce qui a permis d'ajouter le module multi-usines (§5.2.4) en phase 3 sans toucher à `InstructionHandler` : chaque usine est juste une nouvelle instance d'`IStockRepository`, regroupée par `FactoryStore`.

**Fonctionnement**
`IStockRepository` (`GetQuantity`/`HasAtLeast`/`Consume`/`Add`/`Save`), `ITemplateRepository` (`All`/`Find`/`Add`), `IOrderRepository` (`All`/`Find`/`Create`/`UpdateRemaining`/`Remove`) et `IMovementRepository` (`All`/`Record`) sont chacun injectés là où ils sont nécessaires. `IFactoryRegistry` (`Names`/`Exists`/`GetStock`/`TotalQuantity`) est un repository "de repositories" : il donne à `InstructionHandler` un accès par nom d'usine à autant d'`IStockRepository` que nécessaire (`FactoryStore`, deux usines de démonstration dans `Program.cs`, `Usine1`/`Usine2`), sans qu'`InstructionHandler` sache combien il y en a ni comment elles sont persistées. `StockStore`/`TemplateStore`/`OrderStore`/`MovementStore` sont les implémentations JSON actuelles (seed + fichier mutable gitignoré), enregistrées dans le conteneur DI (`Program.cs`) comme instances singleton derrière ces interfaces. `InstructionHandler` garde un constructeur historique à 2 arguments (`IStockRepository`, `ITemplateRepository`) qui l'enveloppe dans un `FactoryStore` à une seule usine — la suite de tests de phase 1/2 n'a donc pas eu besoin d'être réécrite pour la phase 3.

**Alternatives envisagées**
- Garder `StockStore`/le catalogue statique tels quels, sans interface : rejeté, ça couple `InstructionHandler` à une implémentation JSON précise et empêche d'avoir plusieurs usines avec des stocks indépendants sans dupliquer la classe.
- Un unique repository générique `IRepository<T>` : rejeté, le stock (quantités agrégées par nom), les templates (entités nommées) et les commandes (état mutable avec transitions) ont des opérations trop différentes (`Consume`/`HasAtLeast` n'ont pas de sens pour un template ou une commande) pour partager une interface utile.
- Donner à chaque usine sa propre paire complète (stock + templates) : rejeté pour la phase 3, cf. `docs/HYPOTHESES.md` ("Multi-usines — portée") — les templates restent globaux, seul le stock est partitionné par usine.

## Command — dispatch des instructions

- **Catégorie** : Comportemental
- **Où** : `src/DroneFactory/Commands/Instructions/` (`IInstruction` + une classe par instruction), `InstructionRegistry.cs`

**Problème résolu**
Le nombre d'instructions utilisateur grossit à chaque phase (5 en phase 1, +1 avec `ADD_TEMPLATE`, +4 prévues en phase 3). Un `switch` central (comme celui qu'on avait en phase 1 dans `Program.cs`) mélange le dispatch HTTP et la logique métier, et devient de moins en moins lisible à mesure que la liste s'allonge.

**Fonctionnement**
Chaque instruction est une classe implémentant `IInstruction` (`Name`, `Execute(string args)`), qui délègue à une méthode d'`InstructionHandler` (qui garde toute la logique métier et sa couverture de tests existante — le Command reste une coquille fine). `InstructionRegistry` les indexe par nom et expose `TryGet`. Les routes de l'API (`Program.cs`) passent toutes par `registry.TryGet(nom, ...).Execute(args)` plutôt que d'appeler `InstructionHandler` directement, donc ajouter une instruction ne touche qu'un nouveau fichier + une ligne d'enregistrement dans le registre — vérifié en phase 3 avec l'ajout de `RECEIVE`/`ORDER`/`SEND`/`LIST_ORDER`/`GET_MOVEMENTS`/`TRANSFER`, six nouvelles commandes sans toucher aux six précédentes. C'est aussi ce qui a rendu la traçabilité des flux (§5.2.3) triviale à greffer : voir le pattern Decorator ci-dessous.

**Alternatives envisagées**
- Garder le `switch` de dispatch dans `Program.cs` : rejeté, ne scale pas au-delà de quelques instructions et empêche d'observer/décorer l'exécution d'une instruction de façon uniforme (utile pour `GET_MOVEMENTS`).
- Fusionner `IInstruction` et `InstructionHandler` (mettre toute la logique dans les classes Command) : rejeté pour cette passe, ça aurait cassé/dupliqué toute la suite de tests déjà écrite contre `InstructionHandler`.

## Decorator — traçabilité des mouvements de stock

- **Catégorie** : Structurel
- **Où** : `src/DroneFactory/Commands/Instructions/LoggingInstruction.cs`, câblé dans `InstructionRegistry.cs`

**Problème résolu**
`GET_MOVEMENTS` (§5.2.3) doit renvoyer l'historique de toutes les instructions ayant eu un impact sur le stock (`RECEIVE`, `PRODUCE`, `SEND`, `TRANSFER`). Ajouter cette journalisation directement dans `InstructionHandler` aurait mélangé logique métier et traçabilité dans les mêmes méthodes, et obligé à s'assurer qu'aucun futur point de sortie du stock n'oublie l'appel de journalisation.

**Fonctionnement**
`LoggingInstruction` implémente `IInstruction` et enveloppe une autre instance d'`IInstruction` : il délègue l'exécution, puis enregistre un mouvement (`IMovementRepository.Record`) uniquement si la sortie ne commence pas par `ERROR`. `InstructionRegistry` enveloppe uniquement les commandes qui impactent le stock (`ReceiveCommand`, `ProduceCommand`, `SendCommand`, `TransferCommand`) — les commandes de lecture (`STOCKS`, `VERIFY`, …) et `ORDER` (qui réserve sans toucher au stock) restent non décorées. `GetMovementsCommand` lit directement `IMovementRepository` pour restituer l'historique, filtrable par nom d'élément. Ce pattern s'appuie directement sur le Command ci-dessus : comme toute instruction est déjà une simple implémentation d'`IInstruction`, l'envelopper ne demande aucune modification des commandes existantes (OCP).

**Alternatives envisagées**
- Journaliser dans `InstructionHandler` (un appel à `_movements.Record(...)` à la fin de `Receive`/`Produce`/`Send`/`Transfer`) : rejeté, disperse la responsabilité de traçabilité dans plusieurs méthodes métier au lieu de la centraliser à un seul endroit (le registre), et rend plus facile d'oublier l'appel dans une future instruction impactant le stock.
- Un événement / callback (`InstructionHandler` lève un événement `.NET` que `MovementStore` écoute) : rejeté, plus indirect à suivre et à tester qu'une décoration explicite visible dans `InstructionRegistry`, pour un gain d'abstraction inutile ici (un seul abonné).
