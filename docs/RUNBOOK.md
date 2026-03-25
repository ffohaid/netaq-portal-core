# NETAQ Portal — Operations Runbook

> **Version:** 1.0 | **Sprint:** 6 (Support & Maintenance) | **Last Updated:** 2026-03-25

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Architecture](#2-architecture)
3. [Access Information](#3-access-information)
4. [Service Management](#4-service-management)
5. [Deployment Procedures](#5-deployment-procedures)
6. [Backup & Recovery](#6-backup--recovery)
7. [Monitoring & Alerting](#7-monitoring--alerting)
8. [SSL Certificate Management](#8-ssl-certificate-management)
9. [Troubleshooting Guide](#9-troubleshooting-guide)
10. [Security Procedures](#10-security-procedures)
11. [Scaling Guidelines](#11-scaling-guidelines)

---

## 1. System Overview

**NETAQ Portal** (منصة نِطاق) is an enterprise-grade, internal-only, bilingual (Arabic/English) Saudi GovTech platform for RFP drafting, dynamic workflow approvals, and AI-assisted evaluations.

| Component | Technology | Version |
|-----------|-----------|---------|
| Backend API | .NET 8 Web API | 8.0 |
| Frontend | Vue 3 + TypeScript + Tailwind CSS | 3.x |
| Database | SQL Server 2022 | 2022 |
| Cache | Redis | 7.x |
| Object Storage | MinIO | Latest |
| Vector DB | Qdrant | Latest |
| Local LLM | Ollama | Latest |
| Reverse Proxy | Nginx | 1.27 |
| Container Runtime | Docker + Docker Compose | 29.x |

---

## 2. Architecture

```
Internet → Nginx (SSL/443) → Frontend (Vue 3 SPA)
                            → API (.NET 8 / port 5000)
                            → WebSocket (SignalR /hubs/)

API → SQL Server (port 1433)
    → Redis (port 6379)
    → MinIO (port 9000)
    → Qdrant (port 6333)
    → Ollama (port 11434)
```

All services run as Docker containers orchestrated by Docker Compose.

---

## 3. Access Information

| Resource | Details |
|----------|---------|
| Production URL | https://netaq.pro |
| Server IP | 187.124.41.141 |
| SSH Access | `ssh root@187.124.41.141` |
| Project Directory | `/opt/netaq` |
| GitHub Repository | https://github.com/ffohaid/netaq-portal-core |
| Backup Directory | `/opt/netaq/backups` |
| Log Directory | `/opt/netaq/logs` |

---

## 4. Service Management

### View All Container Status

```bash
cd /opt/netaq
docker compose -f docker-compose.prod.yml ps
```

### Start All Services

```bash
cd /opt/netaq
docker compose -f docker-compose.prod.yml up -d
```

### Stop All Services

```bash
cd /opt/netaq
docker compose -f docker-compose.prod.yml down
```

### Restart a Specific Service

```bash
docker restart netaq-api
docker restart netaq-nginx
docker restart netaq-frontend
```

### View Service Logs

```bash
# Real-time logs
docker logs -f netaq-api

# Last 100 lines
docker logs --tail 100 netaq-api

# Logs from last hour
docker logs --since 1h netaq-api
```

### Container Names Reference

| Container | Service |
|-----------|---------|
| `netaq-nginx` | Reverse Proxy + SSL |
| `netaq-api` | .NET 8 Backend API |
| `netaq-frontend` | Vue 3 Frontend |
| `netaq-sqlserver` | SQL Server 2022 |
| `netaq-redis` | Redis Cache |
| `netaq-minio` | MinIO Object Storage |
| `netaq-qdrant` | Qdrant Vector DB |
| `netaq-ollama` | Ollama Local LLM |

---

## 5. Deployment Procedures

### Standard Deployment (Code Update)

```bash
cd /opt/netaq

# 1. Pull latest code
git pull origin main

# 2. Rebuild changed services
docker compose -f docker-compose.prod.yml build api
docker compose -f docker-compose.prod.yml build frontend

# 3. Restart with zero downtime
docker compose -f docker-compose.prod.yml up -d api frontend

# 4. Verify
curl -sf https://netaq.pro/api/health
curl -sf https://netaq.pro/health
```

### Full Rebuild (Infrastructure Changes)

```bash
cd /opt/netaq

# 1. Pull latest code
git pull origin main

# 2. Stop all services
docker compose -f docker-compose.prod.yml down

# 3. Rebuild everything
docker compose -f docker-compose.prod.yml build --no-cache

# 4. Start all services
docker compose -f docker-compose.prod.yml up -d

# 5. Verify all services
docker compose -f docker-compose.prod.yml ps
```

### Database Migration

Database migrations are applied automatically when the API starts. If manual migration is needed:

```bash
# Check current migration status
docker logs netaq-api 2>&1 | grep -i "migrat"

# Force re-run migrations (restart API)
docker restart netaq-api
```

---

## 6. Backup & Recovery

### Automated Backups

Backups run daily at **2:00 AM Saudi time** via cron. The backup script is located at `/opt/netaq/scripts/backup.sh`.

**What gets backed up:**

| Component | File Pattern | Retention |
|-----------|-------------|-----------|
| SQL Server | `NetaqDb_YYYYMMDD_HHMMSS.bak` | 7 days |
| MinIO | `minio_YYYYMMDD_HHMMSS.tar.gz` | 7 days |
| Qdrant | `qdrant_YYYYMMDD_HHMMSS.tar.gz` | 7 days |
| Redis | `redis_YYYYMMDD_HHMMSS.tar.gz` | 7 days |
| Config | `env_YYYYMMDD_HHMMSS.bak` | 7 days |

### Manual Backup

```bash
/opt/netaq/scripts/backup.sh
```

### Restore SQL Server Database

```bash
# 1. Stop API
docker stop netaq-api

# 2. Copy backup to container
docker cp /opt/netaq/backups/NetaqDb_YYYYMMDD_HHMMSS.bak netaq-sqlserver:/tmp/

# 3. Restore database
docker exec netaq-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" -C \
    -Q "RESTORE DATABASE NetaqDb FROM DISK = '/tmp/NetaqDb_YYYYMMDD_HHMMSS.bak' WITH REPLACE"

# 4. Start API
docker start netaq-api
```

### Restore MinIO Data

```bash
docker stop netaq-minio
docker run --rm -v netaq_minio-data:/data -v /opt/netaq/backups:/backup \
    alpine tar xzf /backup/minio_YYYYMMDD_HHMMSS.tar.gz -C /data
docker start netaq-minio
```

---

## 7. Monitoring & Alerting

### Health Monitoring

The health monitor runs every **5 minutes** via cron at `/opt/netaq/scripts/health-monitor.sh`.

**Checks performed:**
- Container status (running/stopped)
- Container health (healthy/unhealthy)
- Disk usage (alert at 85%)
- Memory usage (alert at 90%)
- SSL certificate expiry (auto-renew at 14 days)

### View Monitoring Logs

```bash
# Health monitor log
tail -50 /opt/netaq/logs/health-monitor.log

# Alert log (critical issues only)
cat /opt/netaq/logs/alerts.log
```

### Manual Health Check

```bash
# API health
curl -sf https://netaq.pro/api/health

# Nginx health
curl -sf https://netaq.pro/health

# All containers
docker ps -a --format "table {{.Names}}\t{{.Status}}"
```

---

## 8. SSL Certificate Management

### Certificate Details

| Property | Value |
|----------|-------|
| Provider | Let's Encrypt |
| Domain | netaq.pro |
| Expiry | Auto-renewed every 60 days |
| Auto-renewal | Twice daily via cron |

### Manual Certificate Renewal

```bash
cd /opt/netaq
docker run --rm \
    -v netaq_certbot-webroot:/var/www/certbot \
    -v netaq_certbot-certs:/etc/letsencrypt \
    certbot/certbot renew

# Reload Nginx to use new certificate
docker exec netaq-nginx nginx -s reload
```

### Check Certificate Expiry

```bash
docker run --rm -v netaq_certbot-certs:/certs \
    alpine cat /certs/live/netaq.pro/fullchain.pem | \
    openssl x509 -enddate -noout
```

---

## 9. Troubleshooting Guide

### API Not Responding

```bash
# 1. Check container status
docker ps | grep api

# 2. Check logs
docker logs --tail 50 netaq-api

# 3. Restart API
docker restart netaq-api

# 4. If persistent, rebuild
cd /opt/netaq
docker compose -f docker-compose.prod.yml build api
docker compose -f docker-compose.prod.yml up -d api
```

### Database Connection Issues

```bash
# 1. Check SQL Server status
docker ps | grep sqlserver

# 2. Test connection
docker exec netaq-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" -C -Q "SELECT 1"

# 3. Check disk space (SQL Server needs space)
df -h

# 4. Restart SQL Server (last resort)
docker restart netaq-sqlserver
```

### Frontend Not Loading

```bash
# 1. Check Nginx
docker logs --tail 20 netaq-nginx

# 2. Check Frontend container
docker logs --tail 20 netaq-frontend

# 3. Test internally
curl -sf http://localhost:80/

# 4. Rebuild frontend
cd /opt/netaq
docker compose -f docker-compose.prod.yml build frontend
docker compose -f docker-compose.prod.yml up -d frontend
docker restart netaq-nginx
```

### SSL Certificate Issues

```bash
# 1. Check certificate status
docker run --rm -v netaq_certbot-certs:/etc/letsencrypt \
    certbot/certbot certificates

# 2. Force renewal
docker run --rm \
    -v netaq_certbot-webroot:/var/www/certbot \
    -v netaq_certbot-certs:/etc/letsencrypt \
    certbot/certbot certonly --webroot -w /var/www/certbot \
    -d netaq.pro --email admin@netaq.pro \
    --agree-tos --no-eff-email --force-renewal

# 3. Reload Nginx
docker exec netaq-nginx nginx -s reload
```

### High Memory Usage

```bash
# 1. Check memory per container
docker stats --no-stream --format "table {{.Name}}\t{{.MemUsage}}\t{{.MemPerc}}"

# 2. Prune unused Docker resources
docker system prune -f

# 3. Restart memory-heavy containers
docker restart netaq-ollama  # Ollama uses most memory
```

---

## 10. Security Procedures

### Firewall Rules (UFW)

```bash
# View current rules
ufw status verbose

# Current allowed ports:
# - 22/tcp (SSH)
# - 80/tcp (HTTP)
# - 443/tcp (HTTPS)
```

### Fail2Ban

```bash
# Check banned IPs
fail2ban-client status sshd

# Unban an IP
fail2ban-client set sshd unbanip <IP_ADDRESS>
```

### Security Checklist

- All API keys encrypted with AES-256 in database
- Row-Level Security (RLS) enforced via EF Core Interceptors
- No cascade deletes (DeleteBehavior.NoAction)
- Session tokens: 60-min access, 8-hour refresh in Redis
- All internal services on isolated Docker network
- Only ports 22, 80, 443 exposed externally

---

## 11. Scaling Guidelines

### Vertical Scaling (Current Server)

The current Hostinger VPS can be upgraded for more resources. After upgrade:

```bash
# Update Docker memory limits in docker-compose.prod.yml
# Restart all services
cd /opt/netaq
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d
```

### Horizontal Scaling (Future)

For horizontal scaling, consider:
1. Separate SQL Server to dedicated instance
2. Use Redis Cluster for session management
3. Deploy multiple API instances behind load balancer
4. Use external MinIO cluster for object storage

---

## Emergency Contacts

| Role | Contact |
|------|---------|
| System Admin | admin@netaq.pro |
| DevOps | Via GitHub Issues |
| Repository | https://github.com/ffohaid/netaq-portal-core |

---

*This runbook is maintained as part of the NETAQ Portal project. For updates, submit changes via pull request.*
