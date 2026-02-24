using InternalApkDistribution.Api.Middleware;
using InternalApkDistribution.Core.Interfaces;
using InternalApkDistribution.Infrastructure.ApkParsing;
using InternalApkDistribution.Infrastructure.Persistence;
using InternalApkDistribution.Infrastructure.Storage;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
var conn = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
var dbName = builder.Configuration["MongoDb:DatabaseName"] ?? "InternalApkDistribution";
var client = new MongoClient(conn);
var database = client.GetDatabase(dbName);
builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddSingleton<IApkReleaseRepository>(sp => new MongoApkReleaseRepository(sp.GetRequiredService<IMongoDatabase>()));

// APK storage path
var basePath = builder.Configuration["ApkStorage:BasePath"] ?? Path.Combine(Path.GetTempPath(), "apk-storage");
builder.Services.AddSingleton<IApkFileStorage>(_ => new ApkFileStorage(basePath));

// APK metadata reader
builder.Services.AddSingleton<IApkMetadataReader, ApkNetMetadataReader>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS nội bộ (tùy chỉnh theo môi trường)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

// Fallback cho SPA/static UI
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
