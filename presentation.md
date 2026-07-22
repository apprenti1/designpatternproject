# Guide de soutenance — Drone Factory

Script de présentation + commandes à exécuter, dans l'ordre. Basé sur les attentes du sujet
(§2.4) : présenter les choix de design patterns **et** ceux écartés, faire une démonstration,
mettre en avant les étapes/problèmes rencontrés. Durée indicative : ~12-15 min + questions.

Avant de commencer : ouvrir un terminal à la racine du dépôt, un navigateur, et avoir sous la main
`readme.md`, `docs/DESIGN_PATTERNS.md`, `docs/HYPOTHESES.md`, `docs/CHECKLIST.md`.

---

## 1. Introduction (1 min)

**À dire :**

> "Le sujet est un système d'assemblage de drones pour une usine : on part d'un stock de pièces,
> et le programme doit produire les instructions d'assemblage, vérifier et exécuter des commandes.
> Le projet est découpé en 3 phases notées séparément — implémentation naïve, design patterns,
> modules complémentaires. On est actuellement à la fin de la phase 2."

Montrer `readme.md` §3 (le sujet) en une phrase, pas la peine de le lire en détail.

---

## 2. Historique git — les phases sont distinguables (1 min)

**Commandes :**

```bash
git log --oneline
git tag
git log --oneline phase1 -1
git log --oneline phase2 -1
```

**À dire :**

> "Chaque fichier a son propre commit, avec un auteur identifiable par commit — trois personnes ont
> contribué. Le tag `phase1` marque la fin de l'implémentation naïve, `phase2` la fin du travail sur
> les patterns et les catégories de drones."

```bash
git shortlog -sne
```

---

## 3. Phase 1 — ce qui existait déjà (1 min, rapide)

**À dire :**

> "La phase 1 couvre les 5 instructions de base du sujet : `STOCKS`, `NEEDED_STOCKS`,
> `INSTRUCTIONS`, `VERIFY`, `PRODUCE`. Le point important pour la suite : `InstructionHandler`
> contient toute la logique métier et ne fait **aucun appel à `Console`** — elle retourne des
> lignes de texte. C'est ce qui a permis d'exposer tout ça en API REST sans toucher à la logique,
> et c'est aussi ce qui rend le projet testable directement."

Montrer rapidement `src/DroneFactory/Commands/InstructionHandler.cs` (juste la signature des
méthodes, pas le détail).

**Déviation à assumer clairement (question probable du jury) :**

> "Le sujet décrit une boucle console. On a choisi d'exposer la même logique via une API REST
> (ASP.NET Core minimal API) plutôt qu'un `Console.ReadLine`, pour pouvoir brancher un front
> HTML/JS. Le format d'entrée/sortie de chaque instruction reste identique à ce que décrit le
> sujet — seul le transport change. C'est documenté dans `docs/HYPOTHESES.md`."

---

## 4. Phase 2 — design patterns (5-6 min, le cœur de la soutenance)

Ouvrir `docs/DESIGN_PATTERNS.md` à l'écran, un pattern à la fois. Pour chacun : **problème →
solution → alternative écartée**. Ne pas lire le fichier mot à mot, s'en servir de support.

### 4.1 Strategy — catégories de drones (§4.2 du sujet)

**Fichiers à montrer :** `src/DroneFactory/Domain/Categories/ICategoryRule.cs` puis
`CategoryRules.cs` (une classe par catégorie) puis `CategoryClassifier.cs`.

**À dire :**

> "Le sujet définit 4 catégories (Aérien, Marin, Terrestre, Submersible), chacune avec sa propre
> combinaison de tags à vérifier. On a fait une classe par catégorie qui implémente `ICategoryRule`,
> plutôt qu'un seul gros `if`/`switch`. Ajouter une 5e catégorie — il y en a peut-être une dans les
> modules complémentaires — ne touche à aucune des règles existantes."

**Point technique à mentionner (bonne question du jury sinon) :** le sujet dit que les modules
principal et de contrôle "ne restreignent jamais la catégorisation" — décision : seuls la coque,
le générateur, le module de déplacement et le système comptent pour les tags de catégorie. Sans
cette lecture, `WDS-1` n'appartiendrait à aucune catégorie avec le catalogue fourni (détaillé dans
`docs/HYPOTHESES.md`).

**Alternative écartée à mentionner :** un `switch` unique — rejeté pour l'extensibilité (OCP).

### 4.2 Repository — persistance du stock et des templates

**Fichiers à montrer :** `src/DroneFactory/Storage/IStockRepository.cs` et
`ITemplateRepository.cs`.

**À dire :**

> "`InstructionHandler` ne connaît que ces deux interfaces, jamais directement le fichier JSON.
> Ça permet de tester la logique métier sans toucher au système de fichiers, et surtout ça prépare
> le module multi-usines de la phase 3 : chaque usine pourra avoir sa propre paire de
> repositories, sans changer une ligne de `InstructionHandler`."

**Alternative écartée à mentionner :** un unique `IRepository<T>` générique — rejeté, le stock
(quantités agrégées) et les templates (entités nommées) ont des opérations trop différentes.

### 4.3 Command — dispatch des instructions

**Fichiers à montrer :** `src/DroneFactory/Commands/Instructions/IInstruction.cs`, un exemple
(`ProduceCommand.cs`), puis `InstructionRegistry.cs`.

**À dire :**

> "Chaque instruction utilisateur est une petite classe qui délègue à `InstructionHandler` — la
> logique métier ne bouge pas, seul le dispatch devient table-driven au lieu d'un `switch` central.
> Ça prépare la traçabilité des flux de la phase 3 : on pourra décorer n'importe quelle commande
> pour logguer son exécution, sans toucher aux commandes elles-mêmes."

**Alternative écartée à mentionner :** fusionner `IInstruction` et `InstructionHandler` — rejeté
pour cette passe, aurait cassé la suite de tests déjà écrite.

---

## 5. ADD_TEMPLATE — démonstration de la validation (2 min)

**À dire :**

> "`ADD_TEMPLATE` doit valider les catégories avant d'accepter un nouveau template — c'est le
> point de couture entre le pattern Strategy et la logique métier."

Enchaîner directement sur la démo live (section 6) pour illustrer ce point avec l'exemple invalide.

---

## 6. Démonstration live (4-5 min)

### 6.1 Build + tests (avant de lancer, pour rassurer sur "ça compile et c'est testé")

```bash
dotnet build
dotnet test
```

**À dire pendant que ça tourne :**

> "41 tests xUnit — parsing des arguments, planification d'assemblage, les instructions,
> la persistance du stock, la classification par catégorie, et la validation d'`ADD_TEMPLATE`."

### 6.2 Lancer l'API

```bash
dotnet run --project src/DroneFactory
```

Ouvrir l'URL affichée (`http://localhost:PORT`) dans le navigateur.

### 6.3 Parcours de démo dans le front

Chaque carte a des exemples cliquables (chips) — cliquer dessus remplit le champ **et** exécute
l'instruction automatiquement. Basculer le thème clair/sombre (bouton en haut à droite) à un
moment pour montrer que c'est géré.

1. **STOCKS** — déjà affiché au chargement : le catalogue de pièces + les 4 drones de base.
2. **NEEDED_STOCKS** → chip `2 DXF-1, 1 RDL-1` — montre le détail par drone puis le total.
3. **INSTRUCTIONS** → chip `1 DXF-1` — la séquence interne d'assemblage, colorée par type
   d'instruction (bleu = début/fin, gris = sortie de stock, violet = assemblage/installation).
   Lien direct avec les contraintes d'ordre du §3.2.3 du sujet.
4. **VERIFY** → chip `1 DXF-1, 1 Cat (erreur...)` — reproduit l'exemple du sujet §7.2
   (`ERROR \`Cat\` is not a recognized drone`).
5. **PRODUCE** → chip `50 RDL-1 (stock insuffisant)` — `ERROR Insufficient stock...`, puis chip
   `1 DXF-1 (stock suffisant)` — `STOCK_UPDATED`, et montrer que **STOCKS se met à jour tout seul**
   juste au-dessus.
6. **ADD_TEMPLATE** → chip "exemple invalide (aucune catégorie)" d'abord — montre l'`ERROR` de
   validation de catégorie décrite en section 5. Puis chip "exemple valide (Aérien)" —
   `TEMPLATE_ADDED`, et montrer qu'il apparaît immédiatement dans la carte **Templates &
   catégories** juste en dessous, avec sa catégorie calculée.
7. **Templates & catégories** — pointer que `WDS-1` est bien classé Submersible malgré le module
   principal jamais tagué (S) — le point d'interprétation mentionné en 4.1.

### 6.4 (Optionnel si le temps le permet) Swagger

En environnement de développement, `/swagger` liste tous les endpoints REST générés — utile si le
jury demande à voir la surface de l'API plutôt que le front.

---

## 7. Ce qu'il reste (30s)

**À dire :**

> "La phase 3 ajoute des modules complémentaires — on a prévu d'implémenter les 4 décrits dans le
> sujet : modificateurs de drone (WITH/WITHOUT/REPLACE), gestion de commandes (ORDER/SEND),
> traçabilité des mouvements (GET_MOVEMENTS), et multi-usines (TRANSFER/IN). Le pattern Repository
> et le pattern Command ont été conçus en phase 2 en anticipant ces deux derniers."

Montrer `docs/CHECKLIST.md` en une ligne si le jury demande l'état d'avancement global.

---

## 8. Questions probables — préparer une réponse courte

| Question | Réponse courte |
|---|---|
| Pourquoi une API REST et pas la console du sujet ? | Même logique métier, aucun `Console` dans `InstructionHandler` ; transport HTTP choisi pour brancher un front, documenté comme déviation assumée dans `docs/HYPOTHESES.md`. |
| Pourquoi ces 3 patterns et pas d'autres (Factory, Builder, Observer...) ? | Chacun répond à une douleur concrète du code naïf (catégories, persistance, dispatch) plutôt qu'un pattern plaqué sans besoin — voir "alternatives écartées" dans `docs/DESIGN_PATTERNS.md`. |
| Pourquoi ne pas avoir mis toute la logique dans les classes `Command` ? | Aurait cassé/dupliqué la suite de tests déjà écrite contre `InstructionHandler` ; le Command reste volontairement une coquille fine. |
| Comment WDS-1 est-il Submersible si son module principal n'a jamais le tag (S) ? | Le sujet précise explicitement que le module principal ne restreint jamais la catégorisation — donc seules coque/générateur/déplacement/système comptent. Détaillé dans `docs/HYPOTHESES.md`. |
| Que renvoie `ADD_TEMPLATE` en cas de succès ? Le sujet ne le dit pas. | Choix `TEMPLATE_ADDED {NOM}`, sur le même principe que `STOCK_UPDATED` pour `PRODUCE` — documenté comme hypothèse. |
| Le stock est-il persistant entre deux lancements ? | Oui, `data/stock.json` et `data/templates.json` (gitignorés, régénérés depuis `data/stock.seed.json` / le catalogue au premier lancement). |
