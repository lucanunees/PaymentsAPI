# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos do projeto primeiro (para cache de restore)
COPY ["PaymentsAPI/PaymentsAPI.csproj", "PaymentsAPI/"]

# Restaurar dependências
RUN dotnet restore "PaymentsAPI/PaymentsAPI.csproj"

# Copiar todo o código fonte
COPY . .

# Build da aplicação
WORKDIR /src/PaymentsAPI
RUN dotnet build "PaymentsAPI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "PaymentsAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime (imagem final otimizada)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Criar usuário não-root para segurança
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copiar artefatos publicados
COPY --from=publish /app/publish .

# Expor porta da aplicação
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Definir variáveis de ambiente padrão
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entrypoint
ENTRYPOINT ["dotnet", "PaymentsAPI.dll"]
