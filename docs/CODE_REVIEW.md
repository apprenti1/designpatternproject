# Revue de code — comment ça marche

Explication du fonctionnement du projet pour quelqu'un qui doit le comprendre rapidement (relecture
avant soutenance, reprise en main). Ne remplace pas `docs/DESIGN_PATTERNS.md` (justification détaillée
de chaque pattern + alternatives écartées) ni `docs/HYPOTHESES.md` (décisions d'interprétation du
sujet) — sert de point d'entrée qui renvoie vers les deux.

## Sommaire

- [Flux d'une requête](#flux-dune-requête)
- [Le modèle de domaine](#le-modèle-de-domaine-domain)
- [Les 4 design patterns](#les-4-design-patterns)
- [Les deux algorithmes centraux](#les-deux-algorithmes-centraux)
- [Persistance](#persistance)
- [Pour aller plus loin](#pour-aller-plus-loin)

## Flux d'une requête

```
index.html  --fetch("/api/produce", {args:"1 DXF-1"})-->  Program.cs (route)
            --> InstructionRegistry.TryGet("PRODUCE")
            --> IInstruction trouvée délègue à InstructionHandler.Produce(args)
            --> IEnumerable<string> (logique métier pure, zéro appel Console, donc testable
                directement sans serveur HTTP)
            --> sérialisé en { "lines": [...] } par la route minimal API
```

`InstructionHandler` (`Commands/InstructionHandler.cs`) contient toute la logique métier — une
méthode par instruction du sujet (`Stocks`, `NeededStocks`, `Instructions`, `Verify`, `Produce`,
`AddTemplate`, `Receive`, `Transfer`, `Order`, `Send`, `ListOrder`). Chaque méthode retourne les
lignes de sortie attendues, jamais une exception laissée filer — toute entrée invalide produit une
ligne `ERROR ...`.

## Le modèle de domaine (`Domain/`)

- `PieceCatalog` / `SystemCatalog` / `DroneCatalog` : catalogues statiques transcrits du sujet
  (§6.2), fixes et codés en dur (readonly).
- `DroneTemplate` : record décrivant un drone. Depuis la phase 3 (§5.1.2), `Generators` et
  `MovementModules` sont des **listes** (1 à 2 générateurs, 1 à 3 modules de déplacement) plutôt
  qu'un seul `string` chacun. `RequiredPieces` aplatit tout ça en une séquence de pièces (avec
  doublons si, par ex., 2 générateurs identiques).
- `DroneTemplateBuilder.TryBuild(nom, pièces)` : classe une liste brute de noms de pièces en slots
  (coque / module principal / générateurs / modules de déplacement / module de contrôle / système)
  et valide tout d'un coup :
  - un seul de chaque slot singulier (coque, module principal, module de contrôle, système) ;
  - compatibilité module principal ⊇ système, module de contrôle ∩ système ≠ ∅ ;
  - règle de construction §5.1.2 : ≥ 2 modules de déplacement ⇒ exactement 2 générateurs ;
  - au moins une catégorie (§4.2), sinon rejeté.

  **Réutilisé à deux endroits distincts** : `ADD_TEMPLATE` (§4.3) et la résolution des modificateurs
  `WITH`/`WITHOUT`/`REPLACE` (§5.2.1). C'est le point de couture qui évite de dupliquer ces règles de
  validation dans deux endroits du code.

## Les 4 design patterns

### 1. Strategy — catégories de drones

- **Où** : `Domain/Categories/` (`ICategoryRule`, `AerienRule`/`MarinRule`/`TerrestreRule`/
  `SubmersibleRule`, `CategoryClassifier`)
- **Pourquoi** : le sujet définit 4 catégories, chacune avec sa propre combinaison de tags à
  vérifier. Une classe par catégorie plutôt qu'un `switch`/`if` géant : ajouter une 5e catégorie ne
  toucherait à aucune des règles existantes (OCP).
- **Fonctionnement** : chaque règle implémente `Matches(DroneTemplate)`. `CategoryClassifier.Classify`
  applique toutes les règles et combine les résultats en un `[Flags] enum DroneCategory`.

### 2. Repository — persistance

- **Où** : `Storage/` (`IStockRepository`, `ITemplateRepository`, `IOrderRepository`,
  `IMovementRepository`, `IFactoryRegistry`)
- **Pourquoi** : `InstructionHandler` ne doit jamais savoir que le stock est en réalité un fichier
  JSON. Ça le rend testable sans I/O réel, et ça a permis d'ajouter le multi-usines (§5.2.4) en
  phase 3 sans changer une ligne d'`InstructionHandler`.
- **Fonctionnement** : `IFactoryRegistry` (implémenté par `FactoryStore`) est un « repository de
  repositories » — il donne accès par nom d'usine (`Usine1`, `Usine2`) à autant d'`IStockRepository`
  que nécessaire. `InstructionHandler` résout la bonne usine (`TryResolveFactory`) à partir du
  qualificatif `IN Usine1` extrait par `FactoryQualifier`.

### 3. Command — dispatch des instructions

- **Où** : `Commands/Instructions/` (`IInstruction` + une classe par instruction),
  `InstructionRegistry.cs`
- **Pourquoi** : le nombre d'instructions grossit à chaque phase (5 → 6 → 12). Un `switch` central
  mélangerait dispatch et logique métier et deviendrait illisible.
- **Fonctionnement** : chaque instruction est une classe fine implémentant `IInstruction`
  (`Name`, `Execute(args)`) qui délègue à `InstructionHandler`. `InstructionRegistry` les indexe par
  nom dans un dictionnaire et expose `TryGet`. Les routes (`Program.cs`) passent toutes par
  `registry.TryGet(nom, ...).Execute(args)`.

### 4. Decorator — traçabilité des mouvements

- **Où** : `Commands/Instructions/LoggingInstruction.cs`, câblé dans `InstructionRegistry.cs`
- **Pourquoi** : `GET_MOVEMENTS` (§5.2.3) doit renvoyer l'historique de tout ce qui a impacté le
  stock. Ajouter la journalisation directement dans chaque méthode d'`InstructionHandler` aurait
  dispersé cette responsabilité et rendu facile d'oublier l'appel dans une future instruction.
- **Fonctionnement** : `LoggingInstruction` enveloppe un autre `IInstruction`. Il exécute la
  commande, et si le résultat ne commence pas par `ERROR`, enregistre le mouvement
  (`IMovementRepository.Record`). `InstructionRegistry` n'enveloppe que les commandes qui impactent
  le stock (`RECEIVE`, `PRODUCE`, `SEND`, `TRANSFER`) — pas les lectures, pas `ORDER` (qui réserve
  sans toucher au stock). Repose directement sur le Command ci-dessus : comme toute instruction est
  déjà une simple implémentation d'`IInstruction`, l'envelopper ne demande aucune modification des
  commandes existantes.

## Les deux algorithmes centraux

### `AssemblyPlanner.BuildInstructions` (§3.2.3)

Construit la séquence `GET_OUT_STOCK` / `INSTALL` / `ASSEMBLE` / `FINISHED` pour un drone. Respecte
les 4 contraintes du sujet : sortie du stock avant usage, seul le générateur peut rejoindre la coque
avant le module principal, le module de déplacement après la coque, système installé avant
assemblage. Généralisé pour §5.1.2 : boucle sur les générateurs (compteur `TMP{n}` qui s'incrémente),
puis le module principal, puis boucle sur les modules de déplacement — l'ordre imposé par le sujet
reste respecté quel que soit le nombre de pièces.

### `DroneOrderParser.TryParse` (§5.2.1)

Détecte si `ARGS` contient un mot-clé `WITH`/`WITHOUT`/`REPLACE` ou un `;` :

- **Non** → délègue à `ArgsParser` (comportement classique `,` inchangé depuis la phase 1, doublons
  sommés).
- **Oui** → sépare sur `;`, et pour chaque segment : part du sac de pièces du template de base
  (`DroneTemplate.RequiredPieces`), applique les opérations dans l'ordre où elles apparaissent dans
  le texte (`WITH` ajoute, `WITHOUT` retire, `REPLACE` retire puis ajoute par paires), puis rappelle
  `DroneTemplateBuilder.TryBuild` pour reclasser et **revalider** le résultat comme un vrai template
  (mêmes règles que `ADD_TEMPLATE`). Un modificateur qui casse la structure du drone (ex. retirer la
  seule coque sans la remplacer) est donc rejeté avec une `ERROR` claire, pas silencieusement accepté.

## Persistance

Pas de vraie base de données — chaque repository (`StockStore`, `TemplateStore`, `OrderStore`,
`MovementStore`) est un dictionnaire/liste en mémoire, chargé au démarrage depuis un fichier JSON et
réécrit en entier à chaque mutation (`Save()`). Fichiers `*.seed.json` suivis par git (état de
départ), fichiers live gitignorés (régénérés depuis le seed au premier lancement). Aucun verrou ni
transaction : un seul process est censé écrire dessus à la fois (voir `docs/HYPOTHESES.md` pour le
détail complet, table des fichiers incluse).

## Pour aller plus loin

- `docs/DESIGN_PATTERNS.md` — justification détaillée de chaque pattern + alternatives envisagées et
  écartées (support direct pour la soutenance, §2.4 du sujet).
- `docs/HYPOTHESES.md` — chaque endroit où le sujet est sous-spécifié et la décision retenue, phase
  par phase.
- `docs/CHECKLIST.md` — état d'avancement par rapport au sujet.
- `presentation.md` — script de soutenance avec commandes à exécuter dans l'ordre.
