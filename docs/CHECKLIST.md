# Checklist

Suivi de l'avancement par rapport au [sujet](../readme.md). Coché = fait et vérifié (build + tests
+ test manuel via l'API). Voir `docs/HYPOTHESES.md` pour les interprétations retenues et
`docs/DESIGN_PATTERNS.md` pour le détail des patterns.

## Administratif (§1)

- [x] Dépôt git avec phases distinguables par tags (`phase1`, `phase2`) et auteurs identifiables (§1.2)
- [ ] Slides de soutenance (§1.1)
- [ ] Rapport PDF (choix techniques, problèmes rencontrés, solutions) (§1.1)
- [ ] Soutenance finale (§2.4)

## Phase 1 — Implémentation naïve (§2.1, §3)

- [x] Parsing des commandes quantifiées `ARGS` (§3.1) — `ArgsParser`, doublons sommés
- [x] `STOCKS` (§3.2.1)
- [x] `NEEDED_STOCKS ARGS` (§3.2.2)
- [x] `INSTRUCTIONS ARGS` (§3.2.3) — `AssemblyPlanner`, respecte les 4 contraintes d'ordre (§3.2.3 + `docs/HYPOTHESES.md`)
- [x] `VERIFY ARGS` (§3.2.4)
- [x] `PRODUCE ARGS` (§3.2.5)
- [x] Notations `[Piece1, Piece2]` et `Piece1{System1}` (§3.3)
- [x] Validation de toute entrée, jamais de crash, `ERROR Message` clair (§3.4)
- [x] Programme utilisable pour plusieurs instructions sans relancer (§3.4) — via l'API, un process qui tourne

## Phase 2 — Design patterns + première extension (§2.2, §4)

- [x] Au moins un design pattern justifié (§4.1) — Strategy (catégories), Repository (persistance), Command (dispatch), voir `docs/DESIGN_PATTERNS.md`
- [x] Catégories de drones Aérien/Marin/Terrestre/Submersible (§4.2) — `Domain/Categories/`
- [x] `ADD_TEMPLATE TEMPLATE_NAME, Piece1, …, PieceN` (§4.3) — validation catégories + compatibilité système, persistance `data/templates.json`
- [x] Templates ajoutés utilisables dans toutes les instructions existantes (§4.3)

## API REST + front (hors sujet, extension personnelle)

- [x] API ASP.NET Core exposant chaque instruction (`/api/stocks`, `/api/needed-stocks`, `/api/instructions`, `/api/verify`, `/api/produce`, `/api/templates`)
- [x] `index.html` à la racine (Tailwind + Ionicons CDN), servi en statique par l'API (même origine, pas de CORS)
- [x] Déviation documentée dans `docs/HYPOTHESES.md` (transport HTTP au lieu de la console)

## Phase 3 — Modules complémentaires (§2.3, §5)

Modules choisis (les 4 décrits dans le sujet — le minimum demandé est 2) :

- [ ] **RECEIVE** (§5.1.1) — `RECEIVE ARGS` pour ajouter pièces/assemblages/drones en stock
- [ ] **Contraintes de construction étendues** (§5.1.2) — jusqu'à 3 modules de déplacement et 2 générateurs ; ≥2 modules de déplacement ⇒ 2 générateurs obligatoires, sinon template invalide ; à répercuter dans `AssemblyPlanner`, `ADD_TEMPLATE`, les catégories et `NEEDED_STOCKS`/`VERIFY`/`PRODUCE`
- [ ] **Modificateurs de drone** `WITH`/`WITHOUT`/`REPLACE` (§5.2.1) — nouveau séparateur `;` quand utilisé, ancien format `,` toujours valide, composable
- [ ] **Gestion de commandes** `ORDER`/`SEND`/`LIST_ORDER` (§5.2.2) — identifiant de commande, envoi partiel, `Remaining for ORDERID : ARGS` / `COMPLETED ORDERID`
- [ ] **Traçabilité des flux** `GET_MOVEMENTS [ARGS]` (§5.2.3) — historique de tous les mouvements de stock, filtrable
- [ ] **Multi-usines** `TRANSFER`/`IN Usine1` (§5.2.4) — plusieurs usines, transfert de stock, précision `IN Usine1` sur les instructions existantes, erreur `Missing target factory` si ambigu

## Qualité / bonus

- [x] Suite de tests xUnit (41 tests : `ArgsParser`, `AssemblyPlanner`, `InstructionHandler`, `StockStore`, `CategoryClassifier`, `ADD_TEMPLATE`)
- [x] Build sans warning (`dotnet build`, analyzers + StyleCop actifs)
- [ ] Étendre les tests à la phase 3 au fur et à mesure
