using Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Consumers;
using PaymentsAPI.Infrastructure.Persistence;
using PaymentsAPI.Metrics;
using Prometheus;
using RedisCache.Library.Extensions;
using RedisCache.Library.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentsDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

// ─── Redis Cache via Kubernetes Secrets ────────────────────────
var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "";

var redisConnectionString = string.IsNullOrEmpty(redisPassword)
    ? $"{redisHost}:{redisPort}"
    : $"{redisHost}:{redisPort},password={redisPassword},abortConnect=false";

builder.Services.AddRedisCache(options =>
{
    options.ConnectionString = redisConnectionString;
    options.KeyPrefix = "payments:";
    options.DefaultExpirationInMinutes = 30;
    options.Enabled = true;
});

// Aplicar migrations ANTES de configurar MassTransit
using (var tempProvider = builder.Services.BuildServiceProvider())
{
    var db = tempProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

# region MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.AddEntityFrameworkOutbox<PaymentsDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"];
        var user = builder.Configuration["RabbitMq:Username"];
        var pass = builder.Configuration["RabbitMq:Password"];
        var vhost = builder.Configuration["RabbitMq:VirtualHost"] ?? "/";

        cfg.Host(host, vhost, h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        cfg.Message<PaymentProcessedEventV1>(m =>
        {
            m.SetEntityName("fcg.payments");
        });

        cfg.Publish<PaymentProcessedEventV1>(p =>
        {
            p.ExchangeType = "topic";
        });

        cfg.ReceiveEndpoint("payments.order-placed", e =>
        {
            e.ConfigureConsumeTopology = false;

            e.Bind("fcg.catalog", s =>
            {
                s.ExchangeType = "topic";
                s.RoutingKey = "v1.order-placed";
            });

            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });
    });
});
# endregion

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentsDbContext>("paymentsdb");

var app = builder.Build();

app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", context => "payments-api");
});

app.MapHealthChecks("/health");
app.MapMetrics();

app.MapGet("/", () => Results.Ok(new { service = "PaymentsAPI", status = "ok" }));

// ─── GET /api/payments/{id} — com cache ────────────────────────
app.MapGet("/api/payments/{id:guid}", async (
    Guid id,
    PaymentsDbContext db,
    ICacheService cacheService) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    var cacheKey = $"payment:{id}";
    var cached = await cacheService.GetAsync<object>(cacheKey);

    if (cached is not null)
    {
        AppMetrics.CacheHits.WithLabels("get_payment").Inc();
        stopwatch.Stop();
        AppMetrics.RequestDuration.WithLabels("get_payment").Observe(stopwatch.Elapsed.TotalSeconds);
        return Results.Ok(cached);
    }

    AppMetrics.CacheMisses.WithLabels("get_payment").Inc();

    var payment = await db.Payments.FindAsync(id);
    if (payment is null)
    {
        stopwatch.Stop();
        return Results.NotFound();
    }

    var paymentData = new
    {
        payment.Id,
        payment.OrderId,
        payment.UserId,
        Status = payment.Status.ToString(),
        payment.Reason,
        payment.ProcessedAtUtc
    };

    await cacheService.SetAsync(cacheKey, paymentData, TimeSpan.FromMinutes(30));

    stopwatch.Stop();
    AppMetrics.RequestDuration.WithLabels("get_payment").Observe(stopwatch.Elapsed.TotalSeconds);
    return Results.Ok(paymentData);
});

app.Run();