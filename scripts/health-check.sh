#!/bin/bash
# ============================================================
# NETAQ Portal — Health Check & Monitoring Script
# Sprint 5: Deployment & DevOps
# ============================================================
# Can be run via cron for automated monitoring.
# Cron example: */5 * * * * /opt/netaq/scripts/health-check.sh
# ============================================================

set -euo pipefail

DOMAIN="netaq.pro"
LOG_FILE="/var/log/netaq-health.log"
ALERT_EMAIL="admin@netaq.pro"

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
FAILURES=0

check_service() {
    local name="$1"
    local container="$2"
    
    if docker inspect --format='{{.State.Running}}' "$container" 2>/dev/null | grep -q "true"; then
        echo -e "${GREEN}[OK]${NC} $name ($container) is running"
    else
        echo -e "${RED}[FAIL]${NC} $name ($container) is NOT running"
        FAILURES=$((FAILURES + 1))
    fi
}

echo "=============================================="
echo "  NETAQ Portal — Health Check"
echo "  Time: $TIMESTAMP"
echo "=============================================="

# Check all containers
check_service "Nginx"       "netaq-nginx"
check_service "API"         "netaq-api"
check_service "Frontend"    "netaq-frontend"
check_service "SQL Server"  "netaq-sqlserver"
check_service "Redis"       "netaq-redis"
check_service "MinIO"       "netaq-minio"
check_service "Qdrant"      "netaq-qdrant"
check_service "Ollama"      "netaq-ollama"
check_service "Certbot"     "netaq-certbot"

echo ""

# Check HTTPS endpoint
echo "--- Endpoint Checks ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "https://$DOMAIN" --max-time 10 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "301" ] || [ "$HTTP_CODE" = "302" ]; then
    echo -e "${GREEN}[OK]${NC} HTTPS endpoint: $HTTP_CODE"
else
    echo -e "${RED}[FAIL]${NC} HTTPS endpoint: $HTTP_CODE"
    FAILURES=$((FAILURES + 1))
fi

# Check API health
API_CODE=$(curl -s -o /dev/null -w "%{http_code}" "https://$DOMAIN/api/health" --max-time 10 2>/dev/null || echo "000")
if [ "$API_CODE" = "200" ]; then
    echo -e "${GREEN}[OK]${NC} API health: $API_CODE"
else
    echo -e "${RED}[FAIL]${NC} API health: $API_CODE"
    FAILURES=$((FAILURES + 1))
fi

# Check SSL certificate expiry
echo ""
echo "--- SSL Certificate ---"
CERT_EXPIRY=$(echo | openssl s_client -servername "$DOMAIN" -connect "$DOMAIN:443" 2>/dev/null | openssl x509 -noout -enddate 2>/dev/null | cut -d= -f2)
if [ -n "$CERT_EXPIRY" ]; then
    echo -e "${GREEN}[OK]${NC} SSL expires: $CERT_EXPIRY"
else
    echo -e "${RED}[FAIL]${NC} Could not check SSL certificate"
fi

# Disk usage
echo ""
echo "--- Disk Usage ---"
DISK_USAGE=$(df -h / | tail -1 | awk '{print $5}' | tr -d '%')
if [ "$DISK_USAGE" -lt 85 ]; then
    echo -e "${GREEN}[OK]${NC} Disk usage: ${DISK_USAGE}%"
else
    echo -e "${RED}[WARN]${NC} Disk usage: ${DISK_USAGE}% (above 85%)"
    FAILURES=$((FAILURES + 1))
fi

# Memory usage
MEM_USAGE=$(free | grep Mem | awk '{printf "%.0f", $3/$2 * 100}')
if [ "$MEM_USAGE" -lt 90 ]; then
    echo -e "${GREEN}[OK]${NC} Memory usage: ${MEM_USAGE}%"
else
    echo -e "${RED}[WARN]${NC} Memory usage: ${MEM_USAGE}% (above 90%)"
fi

echo ""
echo "=============================================="
if [ "$FAILURES" -eq 0 ]; then
    echo -e "${GREEN}All checks passed!${NC}"
else
    echo -e "${RED}$FAILURES check(s) failed!${NC}"
fi
echo "=============================================="

# Log result
echo "[$TIMESTAMP] Failures: $FAILURES" >> "$LOG_FILE" 2>/dev/null || true
