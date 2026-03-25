#!/bin/bash
# ============================================================
# NETAQ Portal — Health Monitoring Script
# Sprint 6: Support & Maintenance
# ============================================================
# Monitors all services and restarts unhealthy containers.
# Designed to run every 5 minutes via cron.
# ============================================================

set -euo pipefail

LOG_FILE="/opt/netaq/logs/health-monitor.log"
ALERT_FILE="/opt/netaq/logs/alerts.log"
MAX_LOG_SIZE=10485760  # 10MB

mkdir -p /opt/netaq/logs

# Rotate log if too large
if [ -f "$LOG_FILE" ] && [ $(stat -f%z "$LOG_FILE" 2>/dev/null || stat -c%s "$LOG_FILE" 2>/dev/null) -gt $MAX_LOG_SIZE ]; then
    mv "$LOG_FILE" "${LOG_FILE}.old"
fi

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_FILE"
}

alert() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] ALERT: $1"
    echo "$msg" >> "$ALERT_FILE"
    echo "$msg" >> "$LOG_FILE"
}

# ===== Service Health Checks =====
log "--- Health check started ---"

SERVICES=("netaq-nginx" "netaq-api" "netaq-sqlserver" "netaq-redis" "netaq-minio" "netaq-frontend" "netaq-qdrant" "netaq-ollama")
CRITICAL_SERVICES=("netaq-nginx" "netaq-api" "netaq-sqlserver" "netaq-redis")
ALL_HEALTHY=true

for svc in "${SERVICES[@]}"; do
    STATUS=$(docker inspect --format='{{.State.Status}}' "$svc" 2>/dev/null || echo "not_found")
    HEALTH=$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}no_healthcheck{{end}}' "$svc" 2>/dev/null || echo "unknown")

    if [ "$STATUS" != "running" ]; then
        ALL_HEALTHY=false
        alert "$svc is $STATUS — attempting restart..."
        docker start "$svc" 2>/dev/null || true

        # Check if it is a critical service
        if [[ " ${CRITICAL_SERVICES[*]} " =~ " ${svc} " ]]; then
            alert "CRITICAL: $svc was down and restarted!"
        fi
    elif [ "$HEALTH" = "unhealthy" ]; then
        ALL_HEALTHY=false
        log "WARNING: $svc is unhealthy"

        # Only restart critical services that are unhealthy for extended time
        if [[ " ${CRITICAL_SERVICES[*]} " =~ " ${svc} " ]]; then
            UNHEALTHY_COUNT=$(docker inspect --format='{{len .State.Health.Log}}' "$svc" 2>/dev/null || echo "0")
            if [ "$UNHEALTHY_COUNT" -gt 5 ]; then
                alert "$svc has been unhealthy for too long — restarting..."
                docker restart "$svc" 2>/dev/null || true
            fi
        fi
    else
        log "OK: $svc ($STATUS / $HEALTH)"
    fi
done

# ===== Disk Space Check =====
DISK_USAGE=$(df / | tail -1 | awk '{print $5}' | tr -d '%')
if [ "$DISK_USAGE" -gt 85 ]; then
    alert "Disk usage is at ${DISK_USAGE}%! Consider cleanup."
    # Auto-cleanup Docker
    docker system prune -f --volumes 2>/dev/null | tail -1 >> "$LOG_FILE"
fi

# ===== Memory Check =====
MEM_USAGE=$(free | grep Mem | awk '{printf "%.0f", $3/$2 * 100}')
if [ "$MEM_USAGE" -gt 90 ]; then
    alert "Memory usage is at ${MEM_USAGE}%!"
fi

# ===== SSL Certificate Expiry Check =====
if [ -f "/etc/letsencrypt/live/netaq.pro/fullchain.pem" ] || docker run --rm -v netaq_certbot-certs:/certs alpine test -f /certs/live/netaq.pro/fullchain.pem 2>/dev/null; then
    EXPIRY=$(docker run --rm -v netaq_certbot-certs:/certs alpine sh -c "cat /certs/live/netaq.pro/fullchain.pem" 2>/dev/null | openssl x509 -enddate -noout 2>/dev/null | cut -d= -f2)
    if [ -n "$EXPIRY" ]; then
        EXPIRY_EPOCH=$(date -d "$EXPIRY" +%s 2>/dev/null || echo "0")
        NOW_EPOCH=$(date +%s)
        DAYS_LEFT=$(( (EXPIRY_EPOCH - NOW_EPOCH) / 86400 ))
        if [ "$DAYS_LEFT" -lt 14 ]; then
            alert "SSL certificate expires in ${DAYS_LEFT} days! Renewing..."
            cd /opt/netaq && docker compose -f docker-compose.prod.yml run --rm certbot renew 2>&1 >> "$LOG_FILE"
            docker exec netaq-nginx nginx -s reload 2>/dev/null || true
        else
            log "SSL: Certificate valid for ${DAYS_LEFT} more days."
        fi
    fi
fi

if $ALL_HEALTHY; then
    log "All services are healthy."
fi

log "--- Health check complete ---"
