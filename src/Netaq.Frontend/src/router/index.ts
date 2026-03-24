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
          component: () => import('../views/dashboard/DashboardView.vue'),
        },
        {
          path: 'tasks',
          name: 'Tasks',
          component: () => import('../views/tasks/TaskListView.vue'),
        },
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
  } else if (to.path.startsWith('/auth') && token && to.name !== 'AcceptInvitation') {
    next({ name: 'Dashboard' })
  } else {
    next()
  }
})

export default router
