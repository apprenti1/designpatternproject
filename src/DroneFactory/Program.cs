using DroneFactory.Commands;
using DroneFactory.Domain.Categories;
using DroneFactory.Storage;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IStockRepository>(StockStore.CreateDefault());
builder.Services.AddSingleton<ITemplateRepository>(TemplateStore.CreateDefault());
builder.Services.AddSingleton<InstructionHandler>();
builder.Services.AddSingleton<InstructionRegistry>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var repoRootFiles = new PhysicalFileProvider(RepoPaths.FindRepoRoot());
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = repoRootFiles });
app.UseStaticFiles(new StaticFileOptions { FileProvider = repoRootFiles });

app.MapGet("/api/stocks", (InstructionRegistry registry) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "STOCKS", string.Empty))));

app.MapPost("/api/needed-stocks", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "NEEDED_STOCKS", request.Args))));

app.MapPost("/api/instructions", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "INSTRUCTIONS", request.Args))));

app.MapPost("/api/verify", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "VERIFY", request.Args))));

app.MapPost("/api/produce", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "PRODUCE", request.Args))));

app.MapPost("/api/templates", (InstructionRegistry registry, ArgsRequest request) =>
    Results.Ok(new LinesResponse(Dispatch(registry, "ADD_TEMPLATE", request.Args))));

app.MapGet("/api/templates", (ITemplateRepository templates) =>
    Results.Ok(templates.All.Select(t => new TemplateInfo(
        t.Name,
        CategoryClassifier.Names(CategoryClassifier.Classify(t)).ToArray(),
        t.Hull,
        t.MainModule,
        t.Generator,
        t.MovementModule,
        t.ControlModule,
        t.System)).ToArray()));

app.Run();

static string[] Dispatch(InstructionRegistry registry, string name, string args)
    => registry.TryGet(name, out var instruction)
        ? instruction.Execute(args).ToArray()
        : new[] { $"ERROR Unknown instruction '{name}'" };
