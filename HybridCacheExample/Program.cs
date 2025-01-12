using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024;
    options.MaximumKeyLength = 1024;
    options.ReportTagMetrics = false;
    options.DisableCompression = false;
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5),
        Flags = HybridCacheEntryFlags.DisableDistributedCache
    };
});
#pragma warning restore EXTEXP0018 
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hybrid Cache Example API",
        Version = "v1"
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/hybrid-cache/{key}",
    async (string key, HybridCache cache, string[]? tags, CancellationToken cancellationToken) =>
{
    HybridCacheEntryOptions options = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    return await cache.GetOrCreateAsync(
        key,
        async ct => await SomeFuncAsync(key, ct),
        options,
        tags,
        cancellationToken);
});

app.MapPost("/hybrid-cache/{key}/without-capture",
    async (string key, string[]? tags, HybridCache cache, CancellationToken cancellationToken) =>
{
    HybridCacheEntryOptions options = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    return await cache.GetOrCreateAsync(
        key,
        (key),
        static async (key, token) => await SomeFuncAsync(key, token),
        options,
        tags,
        cancellationToken);
});

app.MapPut("/hybrid-cache/{key}",
    async (string key, string[]? tags, HybridCache cache, CancellationToken cancellationToken) =>
{
    HybridCacheEntryOptions options = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    var someObj = await SomeFuncAsync(key, cancellationToken);
    await cache.SetAsync(
        key,
        someObj,
        options,
        tags,
        cancellationToken);
});

app.MapDelete("/hybrid-cache/{key}",
    async (string key, HybridCache cache, CancellationToken cancellationToken) =>
{
    await cache.RemoveAsync(key, cancellationToken);
});

app.MapDelete("/hybrid-cache",
    async (string[] keys, HybridCache cache, CancellationToken cancellationToken) =>
{
    await cache.RemoveAsync(keys, cancellationToken);
});

app.MapDelete("/hybrid-cache/{tag}/by-tag",
    async (string tag, HybridCache cache, CancellationToken cancellationToken) =>
{
    await cache.RemoveByTagAsync(tag, cancellationToken);
});

app.MapDelete("/hybrid-cache/by-tags",
    async (string[] tags, HybridCache cache, CancellationToken cancellationToken) =>
{
    await cache.RemoveByTagAsync(tags, cancellationToken);
});

static async ValueTask<SomeObj> SomeFuncAsync(string key, CancellationToken token)
{
    if (token.IsCancellationRequested)
    {
        await ValueTask.FromCanceled(token);
    }

    return await ValueTask.FromResult(new SomeObj(key));
}

app.Run();

file record SomeObj(string Key);