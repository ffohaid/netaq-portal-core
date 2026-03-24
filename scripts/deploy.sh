#!/bin/bash
# ============================================================
# NETAQ Portal — Production Deployment Script
# Sprint 5: Deployment & DevOps
# ============================================================
# Usage: bash scripts/deploy.sh [init|update|restart|status|logs]
# ============================================================

set -euo pipefail

COMPOSE_FILE="docker-compose.prod.yml"
PROJECT_DIR="/opt/netaq"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info()  { echo -e "${BLUE}[INFO]${NC} $1"; }
log_ok()    { echo -e "${GREEN}[OK]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ===== Initial Deployment =====
deploy_init() {
    echo "=============================================="
    echo "  NETAQ Portal — Initial Deployment"
    echo "=============================================="

    # Check .env file exists
    if [ ! -f "$PROJECT_DIR/.env" ]; then
        log_error ".env file not found! Copy .env.example to .env and fill in values."
        exit 1
    fi

    log_info "Building and starting all services..."
    cd "$PROJECT_DIR"

    # Use init nginx config first (no SSL)
    cp nginx/nginx.init.conf nginx/nginx.prod.conf.bak 2>/dev/null || true

    # Build images
    docker compose -f "$COMPOSE_FILE" build --no-cache

    # Start infrastructure services first
    log_info "Starting infrastructure services..."
    docker compose -f "$COMPOSE_FILE" up -d sqlserver redis minio qdrant ollama

    # Wait for SQL Server
    log_info "Waiting for SQL Server to be ready..."
    for i in $(seq 1 60); do
        if docker exec netaq-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$(grep DB_PASSWORD .env | cut -d= -f2)" -C -Q "SELECT 1" > /dev/null 2>&1; then
            log_ok "SQL Server is ready!"
            break
        fi
        echo -n "."
        sleep 3
    done

    # Start application services
    log_info "Starting application services..."
    docker compose -f "$COMPOSE_FILE" up -d api frontend nginx certbot

    # Load Ollama model
    log_info "Loading AI model into Ollama..."
    docker exec netaq-ollama ollama pull aya:8b 2>/dev/null || \
    docker exec netaq-ollama ollama pull llama3:8b 2>/dev/null || \
    log_warn "Could not pull AI model. Pull manually: docker exec netaq-ollama ollama pull aya:8b"

    log_ok "Initial deployment complete!"
    echo ""
    echo "Next Steps:"
    echo "  1. Ensure DNS A record points to server IP"
    echo "  2. Run: bash scripts/ssl-init.sh"
    echo "  3. Verify: https://netaq.pro"
}

# ===== Rolling Update =====
deploy_update() {
    echo "=============================================="
    echo "  NETAQ Portal — Rolling Update"
    echo "=============================================="

    cd "$PROJECT_DIR"

    # Pull latest code
    log_info "Pulling latest code from GitHub..."
    git pull origin main

    # Build new images
    log_info "Building updated images..."
    docker compose -f "$COMPOSE_FILE" build

    # Rolling update API (zero-downtime)
    log_info "Updating API service (rolling)..."
    docker compose -f "$COMPOSE_FILE" up -d --no-deps api

    # Wait for API health
    log_info "Waiting for API health check..."
    for i in $(seq 1 30); do
        if docker exec netaq-api curl -sf http://localhost:5000/api/health > /dev/null 2>&1; then
            log_ok "API is healthy!"
            break
        fi
        sleep 5
    done

    # Rolling update Frontend
    log_info "Updating Frontend service..."
    docker compose -f "$COMPOSE_FILE" up -d --no-deps frontend

    # Reload Nginx
    log_info "Reloading Nginx..."
    docker exec netaq-nginx nginx -s reload

    # Cleanup
    docker image prune -f

    log_ok "Rolling update complete!"
}

# ===== Restart Services =====
deploy_restart() {
    echo "=============================================="
    echo "  NETAQ Portal — Restart All Services"
    echo "=============================================="

    cd "$PROJECT_DIR"
    docker compose -f "$COMPOSE_FILE" restart
    log_ok "All services restarted."
}

# ===== Status Check =====
deploy_status() {
    echo "=============================================="
    echo "  NETAQ Portal — Service Status"
    echo "=============================================="

    cd "$PROJECT_DIR"
    docker compose -f "$COMPOSE_FILE" ps

    echo ""
    echo "--- Resource Usage ---"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" \
        netaq-nginx netaq-api netaq-frontend netaq-sqlserver netaq-redis netaq-minio netaq-qdrant netaq-ollama 2>/dev/null || true

    echo ""
    echo "--- Disk Usage (Volumes) ---"
    docker system df -v 2>/dev/null | grep -A 20 "VOLUME NAME" || true
}

# ===== View Logs =====
deploy_logs() {
    local service="${2:-}"
    cd "$PROJECT_DIR"

    if [ -z "$service" ]; then
        echo "Usage: deploy.sh logs [service_name]"
        echo "Services: nginx, api, frontend, sqlserver, redis, minio, qdrant, ollama"
        echo ""
        echo "Showing last 50 lines of all services..."
        docker compose -f "$COMPOSE_FILE" logs --tail=50
    else
        docker compose -f "$COMPOSE_FILE" logs --tail=100 -f "$service"
    fi
}

# ===== Backup =====
deploy_backup() {
    echo "=============================================="
    echo "  NETAQ Portal — Database Backup"
    echo "=============================================="

    cd "$PROJECT_DIR"
    BACKUP_DIR="/opt/netaq/backups"
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    mkdir -p "$BACKUP_DIR"

    log_info "Creating SQL Server backup..."
    docker exec netaq-sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$(grep DB_PASSWORD .env | cut -d= -f2)" -C \
        -Q "BACKUP DATABASE [NetaqDb] TO DISK = N'/var/opt/mssql/backup/NetaqDb_${TIMESTAMP}.bak' WITH FORMAT, INIT, NAME = 'NetaqDb-Full-${TIMESTAMP}'"

    log_ok "Backup saved: NetaqDb_${TIMESTAMP}.bak"
}

# ===== Main =====
case "${1:-help}" in
    init)    deploy_init ;;
    update)  deploy_update ;;
    restart) deploy_restart ;;
    status)  deploy_status ;;
    logs)    deploy_logs "$@" ;;
    backup)  deploy_backup ;;
    *)
        echo "NETAQ Portal — Deployment Manager"
        echo ""
        echo "Usage: bash scripts/deploy.sh <command>"
        echo ""
        echo "Commands:"
        echo "  init     — First-time deployment (build & start all)"
        echo "  update   — Rolling update (pull, build, deploy)"
        echo "  restart  — Restart all services"
        echo "  status   — Show service status and resource usage"
        echo "  logs     — View service logs (e.g., logs api)"
        echo "  backup   — Create database backup"
        ;;
esac
