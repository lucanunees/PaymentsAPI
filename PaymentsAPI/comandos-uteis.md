# 🚀 Guia de Operações — Infraestrutura

> Referência rápida para gerenciamento do ambiente Docker e Kubernetes do projeto **Payments API**.

---

## Índice

- [Docker Compose](#docker-compose)
- [Kubernetes — Cluster Local](#kubernetes--cluster-local)
    - [Aplicar Manifestos](#aplicar-manifestos)
    - [Monitoramento e Debug](#monitoramento-e-debug)
- [Build da Imagem Docker](#build-da-imagem-docker)
    - [Minikube](#minikube)
    - [Kind](#kind)
    - [Docker Desktop Kubernetes](#docker-desktop-kubernetes)

---

## Docker Compose

| Operação              | Comando                                    |
|-----------------------|--------------------------------------------|
| Subir ambiente        | `docker-compose up -d`                     |
| Subir com rebuild     | `docker-compose up -d --build`             |
| Parar ambiente        | `docker-compose down`                      |
| Ver logs em tempo real | `docker-compose logs -f payments-api`     |

---

## Kubernetes — Cluster Local

### Aplicar Manifestos

```bash
# Aplicar todos os manifestos com Kustomize (recomendado)
kubectl apply -k k8s/

# Ou aplicar individualmente, na ordem correta
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/postgres-deployment.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

### Monitoramento e Debug

```bash
# Listar pods no namespace
kubectl get pods -n fcg

# Acompanhar logs em tempo real
kubectl logs -f deployment/payments-api -n fcg

# Port-forward para testes locais
kubectl port-forward svc/payments-api 8080:80 -n fcg

# Verificar health da aplicação
curl http://localhost:8080/health
```

---

## Build da Imagem Docker

Escolha o procedimento de acordo com o ambiente de cluster utilizado.

### Minikube

```bash
eval $(minikube docker-env)
docker build -t payments-api:latest .
```

### Kind

```bash
docker build -t payments-api:latest .
kind load docker-image payments-api:latest
```

### Docker Desktop Kubernetes

```bash
docker build -t payments-api:latest .
```

> **Nota:** No Docker Desktop, a imagem já fica disponível automaticamente para o cluster após o build.  
> Nenhuma etapa extra de carregamento é necessária.

---

*Última atualização: março de 2026*