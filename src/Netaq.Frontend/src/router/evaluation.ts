import type { RouteRecordRaw } from 'vue-router';

const evaluationRoutes: RouteRecordRaw[] = [
  {
    path: '/tenders/:tenderId/proposals',
    name: 'ProposalManagement',
    component: () => import('@/views/evaluation/ProposalManagementView.vue'),
    meta: {
      titleAr: 'إدارة العروض',
      titleEn: 'Proposal Management',
      requiresAuth: true,
      permissions: ['proposals.view']
    }
  },
  {
    path: '/tenders/:tenderId/evaluation/:proposalId/compliance',
    name: 'ComplianceCheck',
    component: () => import('@/views/evaluation/ComplianceCheckView.vue'),
    meta: {
      titleAr: 'الفحص النظامي',
      titleEn: 'Compliance Check',
      requiresAuth: true,
      permissions: ['evaluation.compliance']
    }
  },
  {
    path: '/tenders/:tenderId/evaluation/:proposalId',
    name: 'TechnicalEvaluation',
    component: () => import('@/views/evaluation/TechnicalEvaluationView.vue'),
    meta: {
      titleAr: 'التقييم الفني',
      titleEn: 'Technical Evaluation',
      requiresAuth: true,
      permissions: ['evaluation.technical']
    }
  },
  {
    path: '/tenders/:tenderId/evaluation/financial',
    name: 'FinancialEvaluation',
    component: () => import('@/views/evaluation/FinancialEvaluationView.vue'),
    meta: {
      titleAr: 'التقييم المالي',
      titleEn: 'Financial Evaluation',
      requiresAuth: true,
      permissions: ['evaluation.financial']
    }
  },
  {
    path: '/tenders/:tenderId/evaluation/reports',
    name: 'EvaluationReports',
    component: () => import('@/views/evaluation/EvaluationReportsView.vue'),
    meta: {
      titleAr: 'محاضر التقييم',
      titleEn: 'Evaluation Reports',
      requiresAuth: true,
      permissions: ['evaluation.reports']
    }
  }
];

export default evaluationRoutes;
