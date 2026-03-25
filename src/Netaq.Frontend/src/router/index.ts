import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/auth',
      component: () => import('../views/auth/AuthLayout.vue'),
      children: [
        {
          path: 'login',
          name: 'Login',
          component: () => import('../views/auth/LoginView.vue'),
        },
        {
          path: 'otp',
          name: 'OTP',
          component: () => import('../views/auth/OtpView.vue'),
        },
        {
          path: 'accept-invitation',
          name: 'AcceptInvitation',
          component: () => import('../views/auth/AcceptInvitationView.vue'),
        },
        {
          path: 'forgot-password',
          name: 'ForgotPassword',
          component: () => import('../views/auth/ForgotPasswordView.vue'),
        },
        {
          path: 'reset-password',
          name: 'ResetPassword',
          component: () => import('../views/auth/ResetPasswordView.vue'),
        },
      ],
    },
    {
      path: '/',
      component: () => import('../components/layout/AppLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          redirect: '/dashboard',
        },
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('../views/dashboard/AdvancedDashboardView.vue'),
        },
        {
          path: 'tasks',
          name: 'Tasks',
          component: () => import('../views/tasks/TaskListView.vue'),
        },
        // User Profile
        {
          path: 'profile',
          name: 'UserProfile',
          component: () => import('../views/profile/UserProfileView.vue'),
        },
        // Sprint 2: Tenders
        {
          path: 'tenders',
          name: 'TenderList',
          component: () => import('../views/tenders/TenderListView.vue'),
        },
        {
          path: 'tenders/create',
          name: 'CreateTender',
          component: () => import('../views/tenders/CreateTenderView.vue'),
        },
        {
          path: 'tenders/:id',
          name: 'TenderDetail',
          component: () => import('../views/tenders/TenderDetailView.vue'),
        },
        {
          path: 'tenders/:id/edit',
          name: 'EditTender',
          component: () => import('../views/tenders/EditTenderView.vue'),
        },
        // Sprint 2: Templates
        {
          path: 'templates',
          name: 'TemplateList',
          component: () => import('../views/templates/TemplateListView.vue'),
        },
        {
          path: 'templates/:id',
          name: 'TemplateDetail',
          component: () => import('../views/templates/TemplateDetailView.vue'),
        },
        // Committees
        {
          path: 'committees',
          name: 'CommitteeList',
          component: () => import('../views/committees/CommitteeListView.vue'),
          meta: { roles: ['SystemAdmin', 'OrganizationAdmin', 'DepartmentManager'] },
        },
        {
          path: 'committees/create',
          name: 'CreateCommittee',
          component: () => import('../views/committees/CreateCommitteeView.vue'),
          meta: { roles: ['SystemAdmin', 'OrganizationAdmin'] },
        },
        {
          path: 'committees/:id',
          name: 'CommitteeDetail',
          component: () => import('../views/committees/CommitteeDetailView.vue'),
          meta: { roles: ['SystemAdmin', 'OrganizationAdmin', 'DepartmentManager'] },
        },
        // Inquiries
        {
          path: 'inquiries',
          name: 'InquiryList',
          component: () => import('../views/inquiries/InquiryListView.vue'),
        },
        {
          path: 'inquiries/create',
          name: 'CreateInquiry',
          component: () => import('../views/inquiries/CreateInquiryView.vue'),
        },
        {
          path: 'inquiries/:id',
          name: 'InquiryDetail',
          component: () => import('../views/inquiries/InquiryDetailView.vue'),
        },
        // Permission Matrix
        {
          path: 'admin/permissions',
          name: 'PermissionMatrix',
          component: () => import('../views/permissions/PermissionMatrixView.vue'),
          meta: { roles: ['SystemAdmin'] },
        },
        // Workflows
        {
          path: 'workflows',
          name: 'Workflows',
          component: () => import('../views/workflow/WorkflowListView.vue'),
        },
        {
          path: 'workflows/create',
          name: 'CreateWorkflow',
          component: () => import('../views/workflow/CreateWorkflowView.vue'),
        },
        {
          path: 'workflows/:id',
          name: 'WorkflowDetail',
          component: () => import('../views/workflow/WorkflowDetailView.vue'),
        },
        {
          path: 'audit',
          name: 'AuditTrail',
          component: () => import('../views/audit/AuditTrailView.vue'),
        },
        {
          path: 'admin/users',
          name: 'UserManagement',
          component: () => import('../views/admin/UserManagementView.vue'),
          meta: { roles: ['SystemAdmin', 'OrganizationAdmin'] },
        },
        // Reports
        {
          path: 'reports',
          name: 'Reports',
          component: () => import('../views/reports/ReportsView.vue'),
          meta: { roles: ['SystemAdmin', 'OrganizationAdmin', 'DepartmentManager'] },
        },
        // Settings
        {
          path: 'settings/organization',
          name: 'OrganizationSettings',
          component: () => import('../views/settings/OrganizationSettingsView.vue'),
          meta: { roles: ['SystemAdmin', 'OrganizationAdmin'] },
        },
        {
          path: 'settings/ai',
          name: 'AiConfiguration',
          component: () => import('../views/settings/AiConfigurationView.vue'),
          meta: { roles: ['SystemAdmin'] },
        },
        {
          path: 'settings/knowledge-base',
          name: 'KnowledgeBase',
          component: () => import('../views/settings/KnowledgeBaseView.vue'),
          meta: { roles: ['SystemAdmin'] },
        },
        {
          path: 'settings/system',
          name: 'SystemSettings',
          component: () => import('../views/settings/SystemSettingsView.vue'),
          meta: { roles: ['SystemAdmin'] },
        },
      ],
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'NotFound',
      component: () => import('../views/NotFoundView.vue'),
    },
  ],
})

// Navigation guard
router.beforeEach((to, _from, next) => {
  const token = localStorage.getItem('netaq_access_token')
  
  if (to.meta.requiresAuth && !token) {
    next({ name: 'Login' })
  } else if (to.path.startsWith('/auth') && token && to.name !== 'AcceptInvitation' && to.name !== 'ForgotPassword' && to.name !== 'ResetPassword') {
    next({ name: 'Dashboard' })
  } else {
    next()
  }
})

export default router
