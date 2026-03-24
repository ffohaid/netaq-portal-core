#!/bin/bash
# ============================================================
# NETAQ Portal — SSL Certificate Initialization
# Sprint 5: Deployment & DevOps
# ============================================================
# Obtains initial Let's Encrypt SSL certificate using Certbot.
# Run AFTER docker-compose.prod.yml is up with nginx.init.conf.
# ============================================================

set -euo pipefail

DOMAIN="${DOMAIN:-netaq.pro}"
EMAIL="${CERTBOT_EMAIL:-admin@netaq.pro}"
COMPOSE_FILE="docker-compose.prod.yml"
NGINX_INIT="nginx/nginx.init.conf"
NGINX_PROD="nginx/nginx.prod.conf"

echo "=============================================="
echo "  NETAQ Portal — SSL Certificate Setup"
echo "=============================================="
echo "  Domain: $DOMAIN"
echo "  Email:  $EMAIL"
echo "=============================================="

# Step 1: Ensure we are using the init config (no SSL)
echo "[1/5] Switching to initial Nginx config (no SSL)..."
cp "$NGINX_INIT" nginx/nginx.prod.conf.active 2>/dev/null || true
docker compose -f "$COMPOSE_FILE" up -d nginx

# Wait for Nginx to be ready
echo "[2/5] Waiting for Nginx to start..."
sleep 5

# Step 3: Request certificate
echo "[3/5] Requesting SSL certificate from Let's Encrypt..."
docker compose -f "$COMPOSE_FILE" run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email "$EMAIL" \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d "$DOMAIN" \
    -d "www.$DOMAIN"

# Step 4: Switch to production Nginx config (with SSL)
echo "[4/5] Switching to production Nginx config (with SSL)..."
cp "$NGINX_PROD" nginx/nginx.prod.conf.active 2>/dev/null || true

# Step 5: Reload Nginx
echo "[5/5] Reloading Nginx with SSL configuration..."
docker compose -f "$COMPOSE_FILE" exec nginx nginx -s reload

echo ""
echo "=============================================="
echo "  SSL Certificate Obtained Successfully!"
echo "=============================================="
echo "  https://$DOMAIN is now active."
echo "  Certificate auto-renewal is handled by Certbot container."
echo ""
