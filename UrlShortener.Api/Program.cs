using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.Api.BackgroundJobs;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.Persistence;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ShortUrlOptions>(
    builder.Configuration.GetSection(ShortUrlOptions.SectionName));

builder.Services.AddDbContext<UrlShortenerDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(redisConnectionString!);
});

builder.Services.AddScoped<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IUrlShorteningService, UrlShorteningService>();
builder.Services.AddScoped<IRedirectService, RedirectService>();
builder.Services.AddScoped<IMetadataService, MetadataService>();

builder.Services.AddSingleton<ClickEventChannel>();
builder.Services.AddSingleton<IClickEventPublisher>(sp => sp.GetRequiredService<ClickEventChannel>());
builder.Services.AddHostedService<ClickCountBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
