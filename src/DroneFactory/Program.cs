using DroneFactory.Commands;
using DroneFactory.Storage;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(StockStore.CreateDefault());
builder.Services.AddSingleton<InstructionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var repoRootFiles = new PhysicalFileProvider(RepoPaths.FindRepoRoot());
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = repoRootFiles });
app.UseStaticFiles(new StaticFileOptions { FileProvider = repoRootFiles });

app.MapGet("/api/stocks", (InstructionHandler handler) =>
    Results.Ok(new LinesResponse(handler.Stocks().ToArray())));

app.MapPost("/api/needed-stocks", (InstructionHandler handler, ArgsRequest request) =>
    Results.Ok(new LinesResponse(handler.NeededStocks(request.Args).ToArray())));

app.MapPost("/api/instructions", (InstructionHandler handler, ArgsRequest request) =>
    Results.Ok(new LinesResponse(handler.Instructions(request.Args).ToArray())));

app.MapPost("/api/verify", (InstructionHandler handler, ArgsRequest request) =>
    Results.Ok(new LinesResponse(handler.Verify(request.Args).ToArray())));

app.MapPost("/api/produce", (InstructionHandler handler, ArgsRequest request) =>
    Results.Ok(new LinesResponse(handler.Produce(request.Args).ToArray())));

app.Run();
