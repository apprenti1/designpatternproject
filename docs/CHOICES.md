# Choix techniques et justifications

Liste condensée de tous les choix faits sur ce projet, avec la justification de chacun. Sert de
base au rapport PDF demandé par le sujet (§1.1 : "un rapport PDF précisant vos choix, les problèmes
techniques rencontrés et les solutions trouvées"). Pour le détail complet : `docs/HYPOTHESES.md`
(interprétations du sujet) et `docs/DESIGN_PATTERNS.md` (patterns + alternatives écartées).

## Architecture générale

- **API REST (ASP.NET Core minimal API) plutôt que boucle console**
  *Justification* : le sujet décrit littéralement une boucle console (§3.1/§3.4), mais la logique
  métier (`InstructionHandler`) ne fait aucun appel à `Console` — le format d'entrée/sortie de
  chaque instruction reste identique à ce que décrit le sujet, seul le transport change. Choisi
  pour pouvoir brancher un front HTML/JS (`index.html`) et démontrer l'API visuellement en
  soutenance. Déviation assumée et documentée (`docs/HYPOTHESES.md`).

- **Persistance JSON plutôt qu'une vraie base de données**
  *Justification* : rester simple pour un projet d'école plutôt que sur-ingénierer avec SQLite/
  Postgres, tout en gardant l'abstraction `IStockRepository`/`ITemplateRepository`/... propre
  derrière le pattern Repository — remplaçable sans toucher à `InstructionHandler` si besoin.
  Chaque repository est un dictionnaire/liste en mémoire, chargé au démarrage et réécrit en entier
  à chaque mutation.

- **Fichiers seed + fichiers live séparés, seed suivi par git**
  *Justification* : le sujet ne définit aucun stock de départ ni moyen d'en ajouter avant `RECEIVE`
  (module phase 3). `data/*.seed.json` (suivi git) fixe un état de départ reproductible ; les
  fichiers live (gitignorés) sont régénérés depuis le seed au premier lancement, pour ne jamais
  polluer le dépôt avec de l'état mutable.

## Design patterns (voir `docs/DESIGN_PATTERNS.md` pour le détail + alternatives écartées)

- **Strategy pour les catégories de drones (§4.2)** — une classe par catégorie plutôt qu'un `switch`
  géant, pour respecter l'OCP quand une catégorie s'ajoute.
- **Repository pour la persistance** — `InstructionHandler` ne connaît que des interfaces, jamais le
  JSON derrière ; a permis d'ajouter le multi-usines (phase 3) sans toucher à sa logique.
- **Command pour le dispatch des instructions** — chaque instruction est une classe fine indexée
  par nom dans `InstructionRegistry`, plutôt qu'un `switch` central mêlant dispatch et logique.
- **Decorator pour la traçabilité (§5.2.3)** — `LoggingInstruction` enveloppe les commandes qui
  impactent le stock pour alimenter `GET_MOVEMENTS`, sans modifier les commandes elles-mêmes.

## Interprétations du sujet (sous-spécifié à plusieurs endroits)

- **Algorithme d'assemblage (`INSTRUCTIONS`)** : l'exemple détaillé du sujet (§7.1) est
  intrinsèquement contradictoire (son ordre littéral viole sa propre contrainte "seul le générateur
  peut être monté dans la coque avant le module principal"). Choix : respecter les 4 contraintes
  explicites du §3.2.3 plutôt que reproduire l'exemple caractère pour caractère.
- **Sémantique de VERIFY/PRODUCE** : disponibilité jugée sur le **stock de pièces** (agrégation façon
  `NEEDED_STOCKS`), pas sur un stock de drones finis préexistant — le sujet ne tranche pas.
- **Tags comptant pour la catégorisation (§4.2)** : seuls coque/générateur(s)/module(s) de
  déplacement/système comptent, pas le module principal ni le module de contrôle — nécessaire pour
  que `WDS-1` (catalogue §6.2) appartienne bien à une catégorie, sinon contradiction avec la règle
  "un drone est obligé d'appartenir à une catégorie".
- **Compatibilité module principal/système/module de contrôle (`ADD_TEMPLATE`, §4.3)** : module
  principal = sur-ensemble des tags du système ; module de contrôle = intersection non vide — les
  deux validés contre les 4 templates existants du catalogue.
- **Sortie de succès de `ADD_TEMPLATE`** : `TEMPLATE_ADDED {NOM}`, non spécifiée par le sujet,
  choisie par symétrie avec `STOCK_UPDATED` pour `PRODUCE`.
- **Générateurs/modules de déplacement multiples (§5.1.2)** : catégorisation lue en ANY (au moins un
  module correspond) pour Aérien/Marin/Terrestre, en ALL (tous) pour Submersible ("toutes les
  pièces sont de type S") ; `GET_OUT_STOCK` regroupe les pièces dupliquées en une seule ligne
  plutôt que plusieurs lignes de quantité 1.
- **RECEIVE (§5.1.1)** : valide chaque élément contre les catalogues (pièce/système/drone connu)
  plutôt que d'accepter n'importe quel nom ; succès `STOCK_UPDATED` par cohérence avec `PRODUCE`.
- **REPLACE en paires (§5.2.1)** : `REPLACE B Piece1, C Piece2` lu comme une seule opération
  (retirer B de Piece1, ajouter C de Piece2), pas deux retraits indépendants — cohérent avec la
  phrase du sujet et l'exemple composé donné.
- **Validation post-modificateurs** : le sac de pièces obtenu après WITH/WITHOUT/REPLACE est
  reclassé et revalidé avec les mêmes règles que `ADD_TEMPLATE` (via `DroneTemplateBuilder`
  partagé) — un modificateur qui casse la structure du drone est rejeté, pas silencieusement
  accepté.
- **ORDER/SEND/LIST_ORDER restent globaux (pas par usine)** : le sujet ne mentionne pas `IN` pour
  `ORDER`/`LIST_ORDER`, et une commande client n'a pas de raison d'être liée à une usine de
  production précise. `SEND`, qui sort réellement du stock, accepte `IN Usine1` comme les autres
  instructions impactant le stock.
- **`GET_MOVEMENTS` reproduit l'instruction exécutée** (`{INSTRUCTION} {ARGS}`) plutôt qu'un format
  inventé — lecture la plus directe de "l'intégralité des instructions ayant eu un impact sur le
  stock" (§5.2.3). Seules les instructions qui mutent effectivement le stock sont journalisées.
- **Multi-usines limité au stock (pas aux templates ni aux commandes)** : le sujet ne détaille un
  exemple que pour `PRODUCE`/`GET_STOCKS` (§5.2.4) ; dupliquer les templates par usine aurait
  démesurément alourdi le module par rapport à sa description. Deux usines de démonstration
  (`Usine1`/`Usine2`), pas d'instruction de création dynamique (non demandée par le sujet).
- **Message "usine manquante" filtré par suffisance de stock uniquement pour `PRODUCE`** —
  reproduit littéralement l'exemple du sujet. Pour les autres instructions (`RECEIVE`/`VERIFY`/
  `SEND`), dont la "validité" ne se limite pas à une question de stock, le message liste toutes
  les usines disponibles sans filtrage.

## Choix techniques complémentaires

- **Swagger toujours actif (pas limité à `Development`)** : projet de démo/évaluation locale, pas
  un vrai service en production — la surface `/swagger` fait partie de la démo de soutenance.
- **Documentation Swagger par `IOperationFilter` plutôt que `WithOpenApi()`** : `WithOpenApi()`
  nécessite un package indisponible pour `net6.0` (apparu en .NET 7) ; un filtre Swashbuckle avec
  un dictionnaire (méthode, route) → (résumé, description) donne le même résultat sans dépendance
  supplémentaire ni changement de version du framework.
- **`index.html` servi par une route dédiée, pas par un middleware de fichiers statiques
  générique** : évite d'exposer accidentellement tout le contenu du dépôt (docs, données...) via
  HTTP — seul `index.html` est servi, explicitement.
- **Suite de tests xUnit (79 tests)** pour le bonus qualité (§1.1 : "un bonus pourra être attribué
  si votre code est suffisamment testé") — un fichier de test par domaine fonctionnel, y compris
  les cas d'erreur (formats invalides, règles de construction violées, ambiguïté multi-usines).
