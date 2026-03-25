#!/bin/bash
# ============================================================
# NETAQ Portal — SSL Certificate Auto-Renewal
# Sprint 6: Support & Maintenance
# ============================================================

set -euo pipefail

LOG_FILE="/opt/netaq/logs/ssl-renew.log"
mkdir -p /opt/netaq/logs

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_FILE"
}

log "Starting SSL certificate renewal check..."

cd /opt/netaq

# Run certbot renewal
docker run --rm \
    -v netaq_certbot-webroot:/var/www/certbot \
    -v netaq_certbot-certs:/etc/letsencrypt \
    certbot/certbot renew --quiet 2>&1 >> "$LOG_FILE"

RESULT=$?

if [ $RESULT -eq 0 ]; then
    log "Certificate renewal check complete."
    # Reload Nginx to pick up new certificates
    docker exec netaq-nginx nginx -s reload 2>/dev/null && log "Nginx reloaded." || log "WARNING: Nginx reload failed."
else
    log "ERROR: Certificate renewal failed with exit code $RESULT"
fi

log "SSL renewal script complete."
