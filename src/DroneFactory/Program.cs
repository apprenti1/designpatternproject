using DroneFactory.Commands;
using DroneFactory.Domain.Categories;
using DroneFactory.Storage;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Drone Factory API",
        Version = "v1",
        Description = "API REST exposant les instructions du sujet (readme.md §3, §4, §5). "
            + "Chaque route délègue à `InstructionRegistry` (pattern Command) ; voir docs/DESIGN_PATTERNS.md.",
    });
    options.OperationFilter<EndpointDocsOperationFilter>();
});

var dataDirectory = RepoPaths.DataDirectory;
var indexHtmlPath = Path.Combine(RepoPaths.FindRepoRoot(), "index.html");
var factories = new FactoryStore(new Dictionary<string, IStockRepository>
{
    ["Usine1"] = new StockStore(dataDirectory),
    ["Usine2"] = new StockStore(dataDirectory, "stock.usine2"),
});
var templates = TemplateStore.CreateDefault();
var orders = OrderStore.CreateDefault();
var movements = MovementStore.CreateDefault();
var handler = new InstructionHandler(factories, templates, orders);
var registry = new InstructionRegistry(handler, movements);

builder.Services.AddSingleton<IFactoryRegistry>(factories);
builder.Services.AddSingleton<ITemplateRepository>(templates);
builder.Services.AddSingleton<IOrderRepository>(orders);
builder.Services.AddSingleton<IMovementRepository>(movements);
builder.Services.AddSingleton(handler);
builder.Services.AddSingleton(registry);

var app = builder.Build();

// Always on (not gated behind Development): this is a local/graded demo project, not a real
// production service, and browsing /swagger is part of the soutenance demo.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Drone Factory API v1");
    options.DocumentTitle = "Drone Factory — API";
});

// index.html is self-contained (Tailwind/Ionicons via CDN, no local assets), so a single
// explicit route serves it — no generic static-file middleware exposing the rest of the repo.
app.MapGet("/", () => Results.File(indexHtmlPath, "text/html")).ExcludeFromDescription();

app.MapGet("/api/stocks", (InstructionRegistry registry, string? inFactory) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "STOCKS", ToArgs(inFactory)))))
    .WithName("Stocks").WithTags("Stock").Produces<LinesResponse>();

app.MapPost("/api/needed-stocks", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "NEEDED_STOCKS", request.Args))))
    .WithName("NeededStocks").WithTags("Stock").Produces<LinesResponse>();

app.MapPost("/api/instructions", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "INSTRUCTIONS", request.Args))))
    .WithName("Instructions").WithTags("Assemblage").Produces<LinesResponse>();

app.MapPost("/api/verify", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "VERIFY", request.Args))))
    .WithName("Verify").WithTags("Assemblage").Produces<LinesResponse>();

app.MapPost("/api/produce", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "PRODUCE", request.Args))))
    .WithName("Produce").WithTags("Assemblage").Produces<LinesResponse>();

app.MapPost("/api/templates", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "ADD_TEMPLATE", request.Args))))
    .WithName("AddTemplate").WithTags("Templates").Produces<LinesResponse>();

app.MapGet("/api/templates", (ITemplateRepository templates) =>
    Results.Ok(templates.All.Select(t => new TemplateInfo(
        t.Name,
        CategoryClassifier.Names(CategoryClassifier.Classify(t)).ToArray(),
        t.Hull,
        t.MainModule,
        t.Generators.ToArray(),
        t.MovementModules.ToArray(),
        t.ControlModule,
        t.System)).ToArray()))
    .WithName("ListTemplates").WithTags("Templates").Produces<TemplateInfo[]>();

app.MapPost("/api/receive", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "RECEIVE", request.Args))))
    .WithName("Receive").WithTags("Stock").Produces<LinesResponse>();

app.MapPost("/api/transfer", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "TRANSFER", request.Args))))
    .WithName("Transfer").WithTags("Usines").Produces<LinesResponse>();

app.MapPost("/api/orders", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "ORDER", request.Args))))
    .WithName("CreateOrder").WithTags("Commandes").Produces<LinesResponse>();

app.MapPost("/api/orders/send", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "SEND", request.Args))))
    .WithName("SendOrder").WithTags("Commandes").Produces<LinesResponse>();

app.MapGet("/api/orders", (InstructionRegistry registry) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "LIST_ORDER", string.Empty))))
    .WithName("ListOrders").WithTags("Commandes").Produces<LinesResponse>();

app.MapGet("/api/movements", (InstructionRegistry registry, string? args) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "GET_MOVEMENTS", args ?? string.Empty))))
    .WithName("GetMovements").WithTags("Traçabilité").Produces<LinesResponse>();

app.MapGet("/api/factories", (IFactoryRegistry factories) => Results.Ok(factories.Names))
    .WithName("ListFactories").WithTags("Usines").Produces<string[]>();

app.Run();

static string ToArgs(string? inFactory) => string.IsNullOrWhiteSpace(inFactory) ? string.Empty : $"IN {inFactory}";

static string[] Dispatch(InstructionRegistry registry, string name, string args)
    => registry.TryGet(name, out var instruction)
        ? instruction.Execute(args).ToArray()
        : new[] { $"ERROR Unknown instruction '{name}'" };

/// <summary>
/// Fills in the Swagger summary/description per route (method, relative path), since minimal
/// API lambdas in net6.0 have no attribute-based equivalent to controller XML doc comments and
/// <c>WithOpenApi()</c> requires a package only available from net7.0 onward.
/// </summary>
#pragma warning disable SA1649 // File name should match first type name: intentionally kept alongside the top-level statements in Program.cs.
internal sealed class EndpointDocsOperationFilter : IOperationFilter
{
    private static readonly Dictionary<(string Method, string Path), (string Summary, string Description)> Docs = new()
    {
        [("GET", "api/stocks")] = (
            "STOCKS (§3.2.1) — inventaire des drones et pièces",
            "Sans `inFactory` : agrège toutes les usines (§5.2.4). Avec `inFactory=Usine1` : uniquement cette usine."),
        [("POST", "api/needed-stocks")] = (
            "NEEDED_STOCKS ARGS (§3.2.2) — pièces nécessaires à une commande",
            "Détail par drone puis total. `ARGS` accepte le format classique et les modificateurs WITH/WITHOUT/REPLACE (§5.2.1)."),
        [("POST", "api/instructions")] = (
            "INSTRUCTIONS ARGS (§3.2.3) — séquence d'assemblage interne",
            "GET_OUT_STOCK / INSTALL / ASSEMBLE / FINISHED pour chaque drone de la commande (voir AssemblyPlanner)."),
        [("POST", "api/verify")] = (
            "VERIFY ARGS (§3.2.4) — validité + disponibilité d'une commande",
            "`AVAILABLE` / `UNAVAILABLE` / `ERROR`. `ARGS` peut se terminer par `IN Usine1` (§5.2.4)."),
        [("POST", "api/produce")] = (
            "PRODUCE ARGS (§3.2.5) — exécute une commande, met à jour le stock",
            "`STOCK_UPDATED` ou `ERROR`. Sans `IN Usine1` et avec plusieurs usines : liste celles où le stock suffit (§5.2.4)."),
        [("POST", "api/templates")] = (
            "ADD_TEMPLATE TEMPLATE_NAME, Piece1, ..., PieceN (§4.3) — enregistre un nouveau template",
            "Validé contre les règles de catégorie (§4.2) et de construction (§5.1.2). `TEMPLATE_ADDED {NOM}` ou `ERROR`."),
        [("GET", "api/templates")] = (
            "Liste des templates + catégories dérivées",
            "Commodité pour le front (index.html) — pas une instruction du sujet."),
        [("POST", "api/receive")] = (
            "RECEIVE ARGS (§5.1.1) — ajoute des pièces/drones au stock",
            "Valide chaque élément contre les catalogues. `ARGS` peut se terminer par `IN Usine1`."),
        [("POST", "api/transfer")] = (
            "TRANSFER Usine1, Usine2, ARGS (§5.2.4) — déplace du stock entre usines",
            "`STOCK_UPDATED` ou `ERROR` (usine inconnue, usines identiques, stock insuffisant)."),
        [("POST", "api/orders")] = (
            "ORDER ARGS (§5.2.2) — ouvre une commande client",
            "Renvoie un identifiant `ORDERID` incrémental (ex. `ORDER1`) à réutiliser avec SEND."),
        [("POST", "api/orders/send")] = (
            "SEND ORDERID, ARGS (§5.2.2) — sort du stock les drones d'une commande",
            "Corps attendu : `\"args\": \"ORDER1, 1 DXF-1\"` (éventuellement suivi de `IN Usine1`). "
                + "`Remaining for ORDERID : ARGS` ou `COMPLETED ORDERID`."),
        [("GET", "api/orders")] = (
            "LIST_ORDER (§5.2.2) — commandes restant à satisfaire",
            string.Empty),
        [("GET", "api/movements")] = (
            "GET_MOVEMENTS [ARGS] (§5.2.3) — historique des mouvements de stock",
            "Alimenté par le décorateur `LoggingInstruction` (voir docs/DESIGN_PATTERNS.md). "
                + "Sans `args` : tout l'historique. Avec `args=Piece1,Piece2` : filtré."),
        [("GET", "api/factories")] = (
            "Liste des usines connues",
            "Commodité pour le front (sélecteur `IN Usine1`) — pas une instruction du sujet."),
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod ?? string.Empty;
        var path = context.ApiDescription.RelativePath ?? string.Empty;

        if (Docs.TryGetValue((method, path), out var doc))
        {
            operation.Summary = doc.Summary;
            if (!string.IsNullOrEmpty(doc.Description))
            {
                operation.Description = doc.Description;
            }
        }
    }
}
#pragma warning restore SA1649
