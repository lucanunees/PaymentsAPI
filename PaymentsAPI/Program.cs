using Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsAPI.Consumers;
using PaymentsAPI.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentsDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

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

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "PaymentsAPI", status = "ok" }));

app.Run();