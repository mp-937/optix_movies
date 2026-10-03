using System.Text.Json.Serialization;

using OptixMovies.Api.Endpoints;
using OptixMovies.Core;
using OptixMovies.Data.Sqlite;

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

builder.Services.AddSqliteData(connectionString);
builder.Services.AddScoped<MovieService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.MapMovieEndpoints();

app.Run();
