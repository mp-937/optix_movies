using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http.Timeouts;

using OptixMovies.Api;
using OptixMovies.Api.Endpoints;
using OptixMovies.Core;
using OptixMovies.Data.Sqlite;
using OptixMovies.Embeddings.Onnx;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddErrorHandling();

var requestTimeout = builder.Configuration.GetValue<TimeSpan?>("RequestTimeout")
    ?? throw new InvalidOperationException("The RequestTimeout setting is missing.");
builder.Services.AddRequestTimeouts(options =>
    options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = requestTimeout });

var connectionString = builder.Configuration.GetConnectionString("Movies")
    ?? throw new InvalidOperationException("The Movies connection string is missing.");

builder.Services.AddSessionRateLimiting(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddSqliteData(connectionString);
builder.Services.AddSingleton<ITextEmbedder, OnnxTextEmbedder>();
builder.Services.AddSingleton(new TitleSuggestionSettings(
    builder.Configuration.GetSection("TitleSuggestions:IgnoredWords").Get<string[]>() ?? []));
builder.Services.AddSingleton(new SimilarMovieSettings(
    builder.Configuration.GetValue<double>("SimilarMovies:MinSimilarity")));
builder.Services.AddScoped<MovieService>();
builder.Services.AddHealthChecks().AddCheck<MovieDataHealthCheck>("movie-data");

var app = builder.Build();
app.UseSecurityHeaders();
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    app.MapGet("/", () => TypedResults.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseSessionRateLimiting();
app.UseRequestTimeouts();
app.MapMovieEndpoints();
app.MapHealthChecks("/health");

if (!await app.CheckHealthAsync())
{
    return 1;
}

if (app.Configuration.GetValue("WarmUpOnStartup", defaultValue: true))
{
    await app.WarmUpAsync();
}

app.Run();
return 0;
