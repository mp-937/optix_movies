using System.Text.Json.Serialization;

using OptixMovies.Api;
using OptixMovies.Api.Endpoints;
using OptixMovies.Core;
using OptixMovies.Data.Sqlite;
using OptixMovies.Embeddings.Onnx;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var connectionString = builder.Configuration.GetConnectionString("Movies")
    ?? throw new InvalidOperationException("The Movies connection string is missing.");

builder.Services.AddSessionRateLimiting(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddSqliteData(connectionString);
builder.Services.AddSingleton<ITextEmbedder, OnnxTextEmbedder>();
builder.Services.AddSingleton(new TitleSuggestionSettings(
    builder.Configuration.GetSection("TitleSuggestions:IgnoredWords").Get<string[]>() ?? []));
builder.Services.AddScoped<MovieService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    app.MapGet("/", () => TypedResults.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseSessionRateLimiting();
app.MapMovieEndpoints();

if (!await app.CheckDataAsync())
{
    return 1;
}

if (app.Configuration.GetValue("WarmUpOnStartup", defaultValue: true))
{
    await app.WarmUpAsync();
}

app.Run();
return 0;
