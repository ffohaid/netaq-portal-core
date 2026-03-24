#!/bin/bash
# ============================================================
# NETAQ Portal — GitHub Secrets Setup Guide
# Sprint 5: Deployment & DevOps
# ============================================================
# This script provides instructions for setting up required
# GitHub Secrets for CI/CD pipelines.
# Run: gh secret set <NAME> --body "<VALUE>"
# ============================================================

echo "=============================================="
echo "  NETAQ Portal — GitHub Secrets Setup"
echo "=============================================="
echo ""
echo "The following secrets MUST be configured in GitHub:"
echo "Repository: github.com/ffohaid/netaq-portal-core"
echo "Path: Settings → Secrets and variables → Actions"
echo ""
echo "Required Secrets:"
echo "  SERVER_HOST      — Production server IP (187.124.41.141)"
echo "  SERVER_USER      — SSH username (netaq)"
echo "  SSH_PRIVATE_KEY  — SSH private key for deployment"
echo ""
echo "Commands to set secrets (run with GitHub CLI):"
echo ""
echo '  gh secret set SERVER_HOST --body "187.124.41.141"'
echo '  gh secret set SERVER_USER --body "netaq"'
echo '  gh secret set SSH_PRIVATE_KEY < ~/.ssh/netaq_deploy_key'
echo ""
echo "=============================================="
echo ""
echo "SECURITY REMINDER:"
echo "  - NEVER store passwords or API keys in code or config files"
echo "  - All secrets are managed via GitHub Secrets exclusively"
echo "  - Environment variables on the server are in .env (not in Git)"
echo ""
