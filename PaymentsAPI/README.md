# PaymentsAPI

Serviço de pagamentos do ecossistema FIAPGame, construído com ASP.NET Core (.NET 8), C# 12, MassTransit e RabbitMQ, utilizando Postgres e Entity Framework Core. Expõe health checks e processa eventos de pedidos para orquestrar pagamentos, publicando eventos de saída.

## Tecnologias
- .NET 8 (ASP.NET Core Minimal APIs)
- C# 12
- Entity Framework Core + Npgsql
- MassTransit (RabbitMQ) + Outbox (EF)
- Health Checks

## Endpoints
- GET /            — ping simples do serviço
- GET /health      — health check (inclui verificação do DbContext)

## Arquitetura e Integrações
- Persistência: PaymentsDbContext (Postgres) configurado via connection string "Default".
- Mensageria: RabbitMQ via MassTransit.
    - Consumer: OrderPlacedConsumer (consome eventos de pedidos).
    - Exchange de entrada: fcg.catalog (topic), com binding para routing key v1.order-placed no endpoint payments.order-placed.
    - Evento publicado: PaymentProcessedEventV1
        - Exchange: fcg.payments (topic).
- Outbox: MassTransit EntityFramework Outbox habilitada para garantir consistência entre DB e mensagens (UseBusOutbox + Postgres).

## Configuração
Defina as seguintes configurações (appsettings ou variáveis de ambiente):

- Conexão Postgres:
    - ConnectionStrings__Default="Host=...;Port=5432;Database=...;Username=...;Password=..."
- RabbitMQ:
    - RabbitMq__Host=amqp://seu-host
    - RabbitMq__Username=usuario
    - RabbitMq__Password=senha
    - RabbitMq__VirtualHost=/ (opcional)

Ambiente:
- ASPNETCORE_URLS=http://0.0.0.0:8080 (exemplo)
- ASPNETCORE_ENVIRONMENT=Development|Staging|Production

## Executando localmente
- dotnet restore
- dotnet run
- Acesse:
    - http://localhost:5000 ou https://localhost:7000 (conforme URLs padrão do Kestrel/launchSettings)
    - Health: /health

Para hot reload:
- dotnet watch run

## Migrações (EF Core)
Caso utilize migrações:
- dotnet ef migrations add Init -p PaymentsAPI
- dotnet ef database update -p PaymentsAPI

Certifique-se de ter o pacote de ferramentas dotnet-ef instalado:
- dotnet tool install --global dotnet-ef

## Observabilidade
- HealthChecks: AddDbContextCheck("paymentsdb").
- Configure níveis de log em appsettings.* (Logging:LogLevel).

## Publicação
- Framework-dependent:
    - dotnet publish -c Release
- Self-contained (ex.: Linux x64):
    - dotnet publish -c Release -r linux-x64 --self-contained true

Artefatos em bin/Release/net8.0/[runtime]/publish.

## Boas práticas de produção
- Usar Outbox habilitada (já configurada).
- Proteger credenciais via secrets/KeyVault/variáveis de ambiente.
- Habilitar políticas de retry e circuit breaker no RabbitMQ (MassTransit) se necessário.
- Monitorar filas e conexões do RabbitMQ.

## Contribuição
1. Crie um branch a partir de main.
2. Faça commits pequenos e descritivos.
3. Garanta build e testes.
4. Abra um PR descrevendo mudanças e impactos.

#### Por Marco Antonio
