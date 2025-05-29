#!/bin/bash

# MCP Scheduler Docker Setup Script

set -e

echo "🚀 Starting MCP Scheduler with Docker..."

# Check if .env file exists, if not copy from template
if [ ! -f ".env" ]; then
    echo "📝 Creating .env file from template..."
    cp .env.example .env
    echo "⚠️  Please update the .env file with your actual values before continuing."
    echo "📍 Edit .env file and then run this script again."
    exit 1
fi

# Source environment variables
set -a
source .env
set +a

echo "🔧 Environment variables loaded"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "🐳 Docker is running"

# Build and start services
echo "🏗️  Building and starting services..."
docker-compose up --build -d

echo "⏳ Waiting for services to be healthy..."

# Wait for MariaDB to be ready
echo "🔍 Checking MariaDB health..."
while ! docker-compose exec -T mariadb mysqladmin ping -h localhost --silent; do
    echo "⏳ Waiting for MariaDB to be ready..."
    sleep 2
done

echo "✅ MariaDB is ready"

# Wait for API to be ready
echo "🔍 Checking API health..."
max_attempts=30
attempt=1

while [ $attempt -le $max_attempts ]; do
    if curl -f http://localhost:8080/health > /dev/null 2>&1; then
        echo "✅ API is ready"
        break
    fi
    
    if [ $attempt -eq $max_attempts ]; then
        echo "❌ API failed to start within expected time"
        echo "📋 Checking logs..."
        docker-compose logs mcp-scheduler-api
        exit 1
    fi
    
    echo "⏳ Waiting for API to be ready... (attempt $attempt/$max_attempts)"
    sleep 5
    ((attempt++))
done

echo ""
echo "🎉 MCP Scheduler is running successfully!"
echo ""
echo "📍 Available endpoints:"
echo "   • API: http://localhost:8080"
echo "   • Health: http://localhost:8080/health"
echo "   • Swagger: http://localhost:8080/swagger"
echo "   • MariaDB: localhost:3306"
echo ""
echo "🔧 Useful commands:"
echo "   • View logs: docker-compose logs -f"
echo "   • Stop services: docker-compose down"
echo "   • Restart services: docker-compose restart"
echo "   • Connect to MariaDB: docker-compose exec mariadb mysql -u mcpuser -p McpSchedulerDb"
echo ""
