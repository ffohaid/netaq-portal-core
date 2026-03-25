#!/bin/bash
# ============================================================
# NETAQ Portal — Automated Backup Script
# Sprint 6: Support & Maintenance
# ============================================================
# This script performs daily backups of:
#   1. SQL Server database
#   2. MinIO object storage
#   3. Qdrant vector database
#   4. Redis data
#   5. Environment configuration
# ============================================================

set -euo pipefail

# ===== Configuration =====
BACKUP_DIR="/opt/netaq/backups"
RETENTION_DAYS=7
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="${BACKUP_DIR}/backup_${TIMESTAMP}.log"

# Load environment variables
source /opt/netaq/.env

# ===== Functions =====
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

cleanup_old_backups() {
    log "Cleaning up backups older than ${RETENTION_DAYS} days..."
    find "$BACKUP_DIR" -name "*.bak" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    find "$BACKUP_DIR" -name "*.tar.gz" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    find "$BACKUP_DIR" -name "*.log" -mtime +${RETENTION_DAYS} -delete 2>/dev/null || true
    log "Cleanup complete."
}

# ===== Main =====
mkdir -p "$BACKUP_DIR"

log "=========================================="
log "NETAQ Portal — Backup Started"
log "=========================================="

# 1. SQL Server Backup
log "[1/5] Backing up SQL Server database..."
docker exec netaq-sqlserver /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C \
    -Q "BACKUP DATABASE NetaqDb TO DISK = '/var/opt/mssql/backup/NetaqDb_${TIMESTAMP}.bak' WITH FORMAT, COMPRESSION, STATS = 10" 2>&1 | tee -a "$LOG_FILE"

# Copy backup from container
docker cp netaq-sqlserver:/var/opt/mssql/backup/NetaqDb_${TIMESTAMP}.bak "${BACKUP_DIR}/NetaqDb_${TIMESTAMP}.bak" 2>&1 | tee -a "$LOG_FILE"

# Remove backup from container to save space
docker exec netaq-sqlserver rm -f "/var/opt/mssql/backup/NetaqDb_${TIMESTAMP}.bak" 2>/dev/null || true

log "[OK] SQL Server backup complete: NetaqDb_${TIMESTAMP}.bak"

# 2. MinIO Backup (object storage data)
log "[2/5] Backing up MinIO data..."
docker run --rm \
    -v netaq_minio-data:/data:ro \
    -v "${BACKUP_DIR}:/backup" \
    alpine tar czf "/backup/minio_${TIMESTAMP}.tar.gz" -C /data . 2>&1 | tee -a "$LOG_FILE"
log "[OK] MinIO backup complete: minio_${TIMESTAMP}.tar.gz"

# 3. Qdrant Backup (vector database)
log "[3/5] Backing up Qdrant data..."
docker run --rm \
    -v netaq_qdrant-data:/data:ro \
    -v "${BACKUP_DIR}:/backup" \
    alpine tar czf "/backup/qdrant_${TIMESTAMP}.tar.gz" -C /data . 2>&1 | tee -a "$LOG_FILE"
log "[OK] Qdrant backup complete: qdrant_${TIMESTAMP}.tar.gz"

# 4. Redis Backup
log "[4/5] Backing up Redis data..."
docker exec netaq-redis redis-cli -a "${REDIS_PASSWORD}" BGSAVE 2>/dev/null || true
sleep 5
docker run --rm \
    -v netaq_redis-data:/data:ro \
    -v "${BACKUP_DIR}:/backup" \
    alpine tar czf "/backup/redis_${TIMESTAMP}.tar.gz" -C /data . 2>&1 | tee -a "$LOG_FILE"
log "[OK] Redis backup complete: redis_${TIMESTAMP}.tar.gz"

# 5. Environment Config Backup (encrypted)
log "[5/5] Backing up configuration..."
cp /opt/netaq/.env "${BACKUP_DIR}/env_${TIMESTAMP}.bak"
chmod 600 "${BACKUP_DIR}/env_${TIMESTAMP}.bak"
log "[OK] Configuration backup complete."

# Cleanup old backups
cleanup_old_backups

# Summary
log "=========================================="
log "NETAQ Portal — Backup Complete"
log "=========================================="
TOTAL_SIZE=$(du -sh "$BACKUP_DIR" 2>/dev/null | cut -f1)
log "Total backup size: ${TOTAL_SIZE}"
log "Backup location: ${BACKUP_DIR}"
log "=========================================="
