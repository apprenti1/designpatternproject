# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

Phase 1 (naive implementation, readme.md §2.1) is implemented for the base subject (§3): the five core instructions (`STOCKS`, `NEEDED_STOCKS`, `INSTRUCTIONS`, `VERIFY`, `PRODUCE`) over the fixed catalog of 4 drones (§6.2). `ADD_TEMPLATE` and drone categories (§4, "Première extension") and the modules complémentaires (§5: multi-factory, orders, movement history) are not implemented yet — that's the next increments.

Code layout under `src/DroneFactory`:
- `Domain/` — static catalogs (`PieceCatalog`, `SystemCatalog`, `DroneCatalog`) transcribed from readme.md §6.2, plus `DroneTemplate`/`Piece`/`SystemPart` records.
- `Storage/` — `StockStore` (JSON-backed quantities) and `RepoPaths` (locates the repo root by walking up to `DroneFactory.sln`, so paths resolve the same under `dotnet run`, `dotnet test`, or a published binary).
- `Assembly/` — `AssemblyPlanner`, which generates the internal `GET_OUT_STOCK`/`INSTALL`/`ASSEMBLE`/`FINISHED` sequence for one drone.
- `Commands/` — `ArgsParser` (quantified drone list parsing, §3.1) and `InstructionHandler` (one method per instruction, pure logic returning output lines — no `Console` calls, so it's testable directly). `Program.cs` is just the REPL loop + dispatch switch on top of `InstructionHandler`.

## Data / persistence

Stock is JSON-backed: `data/stock.seed.json` (tracked in git) holds the starting quantities; `data/stock.json` (gitignored) is the live, mutable copy `StockStore` creates from the seed on first run and rewrites after every successful `PRODUCE`. This was a deliberate choice over an in-memory-only or SQLite store — see the plan history for trade-offs — chosen to stay simple in phase 1 while still generalizing cleanly to the multi-factory/orders/movements modules later (§5.2).

## Documented interpretations

`docs/HYPOTHESES.md` records every place the subject (readme.md) is under-specified or internally inconsistent, and the interpretation this codebase follows — notably: how the initial stock is seeded (no `RECEIVE` instruction exists yet), the exact assembly-instruction algorithm (the §7.1 worked example is self-contradictory and its literal ordering actually violates the subject's own stated constraint), and the piece-stock-based semantics of `VERIFY`/`PRODUCE`. **Check it before changing behavior in these areas**, and update it if the interpretation changes.

## Design patterns documentation

`docs/DESIGN_PATTERNS.md` tracks every design pattern introduced from phase 2 onward: what it's for, where it lives, and alternatives that were considered and rejected — this feeds directly into the final defense (readme.md §2.4). **Update it in the same change whenever a pattern is introduced or reworked**, don't let it drift from the code.

## Common commands

Run from the repository root (where `DroneFactory.sln` lives):

- Build: `dotnet build`
- Run the console app: `dotnet run --project src/DroneFactory`
- Run all tests: `dotnet test`
- Run a single test: `dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"` (or any [xUnit filter expression](https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests))
- Format code (StyleCop/analyzer-aware): `dotnet format`
- Check formatting without modifying files (CI-style): `dotnet format --verify-no-changes`

### Toolchain

- .NET SDK 6.0 (`net6.0` target framework), `dotnet format` is bundled with the SDK.
- Analyzers: built-in .NET analyzers (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`) plus `StyleCop.Analyzers`, both wired into `src/DroneFactory/DroneFactory.csproj` only (the test project is left unconstrained). Style/analyzer severities are tuned in the root `.editorconfig`; StyleCop-specific settings (e.g. XML doc requirements are off) live in `src/DroneFactory/stylecop.json`.
- Test framework: xUnit (`tests/DroneFactory.Tests`).

## What this project is

A school assignment (École / MyGES) to build, in C#, a drone-factory production system, evolving it through **three graded phases**:

1. **Naive implementation** — satisfy the functional spec below with no attention to design patterns (a proof of concept, not a finished product).
2. **Design patterns pass** — refactor (or rewrite) the naive solution, introducing and justifying design patterns. Choices made here must be defensible at the final defense ("soutenance"), including patterns that were considered and rejected.
3. **Free supplementary modules** — add at least two additional modules (list provided via MyGES) on top of the pattern-based solution.

Git history must make these phases distinguishable (tags or branches), and every commit author must be identifiable — keep phase work in clearly separated commits/branches/tags rather than one continuous history.

## Domain model

A drone is assembled from exactly: 1 hull (`coque`), 1 main module (`module principal`), 1 generator (`générateur`), 1 movement module (`module de déplacement`), 1 control module (`module de contrôle`), plus a system (`système`) installed on the main module.

Later phases relax this: a drone may have up to 3 movement modules and 2 generators, but **if it has ≥2 movement modules it must also have 2 generators**, or the template is invalid.

### Drone categories

Every drone must belong to at least one category (never zero), and can belong to several simultaneously:

- **Aérien (F)** — a movement module (F) + a system of type (3D)
- **Marin (M)** — a watertight hull (S) + a system of type (2D) + a movement module (M)
- **Terrestre (L)** — a movement module (L) + a system of type (2D)
- **Submersible (S)** — every part is of type (S) + a system of type (3D)

The main and control modules never restrict categorization, but a main module doesn't necessarily support every system, and the control module must be compatible with the system installed on the main module.

### Notation rules

- An unnamed assembly is notated as its component parts in brackets, comma-separated, order/whitespace-insensitive: `[Piece1, Piece2, Piece3]`.
- A part with an installed system is notated as `PartName{SystemName}` with no spaces, e.g. `Piece1{System1}`.

## The instruction protocol

The program is a console REPL: it must accept repeated instructions without restarting, validate every input, and never crash — invalid input produces a clear `ERROR Message`, never a raw exception.

User-facing instructions accept a quantified drone list abbreviated `ARGS` throughout the spec, e.g. `A Drone1, B Drone2, C Drone1` (duplicate drones in one command are summed).

| Instruction | Purpose |
|---|---|
| `STOCKS` | List all available drones/parts in stock |
| `NEEDED_STOCKS ARGS` | List parts needed to produce a given order, per-drone and totaled |
| `INSTRUCTIONS ARGS` | Emit the full internal assembly-instruction sequence for an order |
| `VERIFY ARGS` | Check an order is well-formed and stock is sufficient (`AVAILABLE`/`UNAVAILABLE`/`ERROR`) |
| `PRODUCE ARGS` | Execute an order, mutating stock (`STOCK_UPDATED`/`ERROR`) |
| `ADD_TEMPLATE TEMPLATE_NAME, Piece1, …, PieceN` | Register a new drone template (validated against category rules) |

Added in the "modules complémentaires" phase:

| Instruction | Purpose |
|---|---|
| `RECEIVE ARGS` | Add parts/assemblies/drones into stock |
| `ORDER ARGS` → `SEND ORDERID, ARGS` | Open a backorder, then fulfill it incrementally; responds with `Remaining for ORDERID : ARGS` or `COMPLETED ORDERID` |
| `LIST_ORDER` | List outstanding orders |
| `GET_MOVEMENTS [ARGS]` | Stock movement history, optionally filtered to specific items |
| `TRANSFER Usine1, Usine2, ARGS` | Move stock between factories |
| `IN Usine1` suffix | Targets any stock/template-affecting instruction at a specific factory; omitting it when required must list which factories the operation is valid for (see `ERROR Missing target factory. Available factory for this instruction are …`) |

Drone modification qualifiers on `ARGS` (`WITH`, `WITHOUT`, `REPLACE`) are composable and, when used, switch the drone-list separator from `,` to `;` — but the original comma-separated format without qualifiers must keep working:
```
A Drone1 REPLACE B Piece1, C Piece2 WITH D Piece3; E Drone2; F Drone3 WITHOUT G Piece4
```

### Internal assembly instructions (not user-facing)

These are what `INSTRUCTIONS ARGS` outputs — they represent the assembly steps themselves, not commands a user types:

- `PRODUCING Drone1` / `FINISHED Drone1` — bracket one drone's build
- `GET_OUT_STOCK A Piece1` — pull parts from stock before use
- `ASSEMBLE [Assembly1] Piece1 Piece2` — combine parts; naming the result is optional but affects how it's referenced downstream (see `readme.md` §7.1 for the full worked example)
- `INSTALL System1 Piece1` — install a system onto a part

Ordering constraints that any implementation must enforce: parts must be pulled from stock before use; only the generator may be mounted into the hull before the main module; the movement module must be assembled after the hull; systems must be installed on their target part before that part is used in a further assembly.

## Reference

`readme.md` is the authoritative spec (in French) — consult it directly for exact wording, the full parts/drone catalog (§6.2), and the worked examples (§7) when implementing or verifying behavior, since this file summarizes rather than replaces it.
