using DroneFactory.Commands;
using DroneFactory.Domain.Categories;
using DroneFactory.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = EndpointDocs.Info.Title,
        Version = EndpointDocs.Info.Version,
        Description = EndpointDocs.Info.Description,
    });
    options.OperationFilter<EndpointDocsOperationFilter>();
});

var dataDirectory = RepoPaths.DataDirectory;
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

var repoRootFiles = new PhysicalFileProvider(RepoPaths.FindRepoRoot());
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = repoRootFiles });
app.UseStaticFiles(new StaticFileOptions { FileProvider = repoRootFiles });

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
