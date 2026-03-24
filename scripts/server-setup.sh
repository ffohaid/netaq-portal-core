#!/bin/bash
# ============================================================
# NETAQ Portal — Server Provisioning & Hardening Script
# Sprint 5: Deployment & DevOps
# ============================================================
# Run as root on Ubuntu 24.04 LTS (Hostinger VPS)
# Usage: sudo bash server-setup.sh
# ============================================================

set -euo pipefail

echo "=============================================="
echo "  NETAQ Portal — Server Setup & Hardening"
echo "=============================================="

# ===== 1. System Update =====
echo "[1/7] Updating system packages..."
apt update && apt upgrade -y
apt install -y \
    curl wget git unzip software-properties-common \
    apt-transport-https ca-certificates gnupg lsb-release \
    ufw fail2ban htop net-tools

# ===== 2. Create Non-Root User for Docker =====
echo "[2/7] Creating deployment user..."
if ! id "netaq" &>/dev/null; then
    useradd -m -s /bin/bash -G sudo netaq
    echo "netaq ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/netaq
    chmod 440 /etc/sudoers.d/netaq
    echo "[INFO] User 'netaq' created. Set password manually: passwd netaq"
else
    echo "[INFO] User 'netaq' already exists."
fi

# ===== 3. SSH Hardening =====
echo "[3/7] Hardening SSH configuration..."
SSHD_CONFIG="/etc/ssh/sshd_config"
cp "$SSHD_CONFIG" "${SSHD_CONFIG}.bak"

# Disable root password login (allow SSH key only)
sed -i 's/^#\?PermitRootLogin.*/PermitRootLogin prohibit-password/' "$SSHD_CONFIG"
sed -i 's/^#\?PasswordAuthentication.*/PasswordAuthentication no/' "$SSHD_CONFIG"
sed -i 's/^#\?PubkeyAuthentication.*/PubkeyAuthentication yes/' "$SSHD_CONFIG"
sed -i 's/^#\?MaxAuthTries.*/MaxAuthTries 3/' "$SSHD_CONFIG"
sed -i 's/^#\?LoginGraceTime.*/LoginGraceTime 30/' "$SSHD_CONFIG"

# Ensure SSH key directory exists for netaq user
mkdir -p /home/netaq/.ssh
chmod 700 /home/netaq/.ssh
chown -R netaq:netaq /home/netaq/.ssh

echo "[INFO] SSH hardened. Ensure SSH keys are configured before restarting SSH."

# ===== 4. Firewall (UFW) =====
echo "[4/7] Configuring UFW firewall..."
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp comment 'SSH'
ufw allow 80/tcp comment 'HTTP (redirect to HTTPS)'
ufw allow 443/tcp comment 'HTTPS'
ufw --force enable
echo "[INFO] UFW enabled. Only ports 22, 80, 443 are open."

# ===== 5. Fail2Ban =====
echo "[5/7] Configuring Fail2Ban..."
cat > /etc/fail2ban/jail.local << 'EOF'
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5
backend = systemd

[sshd]
enabled = true
port = ssh
filter = sshd
logpath = /var/log/auth.log
maxretry = 3
bantime = 7200
EOF

systemctl enable fail2ban
systemctl restart fail2ban
echo "[INFO] Fail2Ban configured for SSH protection."

# ===== 6. Docker & Docker Compose =====
echo "[6/7] Installing Docker..."
if ! command -v docker &>/dev/null; then
    curl -fsSL https://get.docker.com | sh
    systemctl enable docker
    systemctl start docker
    usermod -aG docker netaq
    echo "[INFO] Docker installed and netaq user added to docker group."
else
    echo "[INFO] Docker already installed."
fi

# Verify Docker Compose plugin
docker compose version || {
    echo "[ERROR] Docker Compose plugin not found. Installing..."
    apt install -y docker-compose-plugin
}

# ===== 7. System Tuning =====
echo "[7/7] Applying system tuning..."
cat >> /etc/sysctl.conf << 'EOF'

# NETAQ Portal — System Tuning
net.core.somaxconn = 65535
net.ipv4.tcp_max_syn_backlog = 65535
net.ipv4.ip_local_port_range = 1024 65535
net.ipv4.tcp_tw_reuse = 1
vm.swappiness = 10
vm.overcommit_memory = 1
fs.file-max = 2097152
EOF
sysctl -p

echo ""
echo "=============================================="
echo "  Server Setup Complete!"
echo "=============================================="
echo ""
echo "Next Steps:"
echo "  1. Copy SSH public key to /home/netaq/.ssh/authorized_keys"
echo "  2. Test SSH login as 'netaq' user before restarting SSH"
echo "  3. Restart SSH: systemctl restart sshd"
echo "  4. Run: cd /opt/netaq && docker compose -f docker-compose.prod.yml up -d"
echo ""
