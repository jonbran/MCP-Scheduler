# Docker Setup Guide

This guide explains how to run the MCP Scheduler using Docker for both local development and production deployment.

## Prerequisites

- Docker and Docker Compose installed
- MariaDB/MySQL database (for production)

## Local Development with Docker

### Quick Start

1. **Copy environment template:**

   ```bash
   cp .env.example .env
   ```

2. **Update environment variables in `.env`:**

   ```bash
   # Update these values
   DB_PASSWORD=your_secure_password
   JWT_KEY=your-super-secret-jwt-key-that-is-at-least-256-bits-long
   ```

3. **Start services:**
   ```bash
   ./start-docker.sh
   ```

### Manual Setup

1. **Build and start services:**

   ```bash
   docker-compose up --build -d
   ```

2. **Check service status:**

   ```bash
   docker-compose ps
   ```

3. **View logs:**

   ```bash
   docker-compose logs -f
   ```

4. **Stop services:**
   ```bash
   docker-compose down
   ```

## Production Deployment

### Azure Container Instances

1. **Build and push image:**

   ```bash
   # Build image
   docker build -t mcpscheduler .

   # Tag for Azure Container Registry
   docker tag mcpscheduler your-registry.azurecr.io/mcpscheduler:latest

   # Push to registry
   docker push your-registry.azurecr.io/mcpscheduler:latest
   ```

2. **Deploy to Azure:**
   ```bash
   # Using Azure CLI
   az container create \
     --resource-group your-rg \
     --name mcp-scheduler \
     --image your-registry.azurecr.io/mcpscheduler:latest \
     --ports 8080 \
     --environment-variables \
       ASPNETCORE_ENVIRONMENT=Production \
       DATABASE_PROVIDER=mysql \
       CONNECTION_STRING="Server=your-db;Database=McpSchedulerDb;User=user;Password=pass;" \
       JWT_KEY="your-jwt-key"
   ```

### Azure Container Apps

1. **Use the production docker-compose:**

   ```bash
   docker-compose -f docker-compose.prod.yml up
   ```

2. **Set environment variables in Azure:**
   - `CONNECTION_STRING`: Your MariaDB/MySQL connection string
   - `DB_PASSWORD`: Database password
   - `JWT_KEY`: JWT signing key

## Environment Variables

| Variable                 | Description                          | Required | Default     |
| ------------------------ | ------------------------------------ | -------- | ----------- |
| `DB_PASSWORD`            | Database password                    | Yes      | -           |
| `JWT_KEY`                | JWT signing key (256+ bits)          | Yes      | -           |
| `CONNECTION_STRING`      | Database connection string           | Yes      | -           |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | No       | Development |
| `DATABASE_PROVIDER`      | Database provider (mysql/sqlserver)  | No       | mysql       |

## Health Checks

- **API Health:** `http://localhost:8080/health`
- **Swagger UI:** `http://localhost:8080/swagger` (Development only)

## Troubleshooting

### Container won't start

```bash
# Check logs
docker-compose logs mcp-scheduler-api

# Check container status
docker-compose ps
```

### Database connection issues

```bash
# Test database connectivity
docker-compose exec mariadb mysql -u mcpuser -p McpSchedulerDb

# Check database logs
docker-compose logs mariadb
```

### Performance issues

```bash
# Check resource usage
docker stats

# Scale services (if needed)
docker-compose up --scale mcp-scheduler-api=2
```
