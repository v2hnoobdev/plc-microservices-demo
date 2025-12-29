<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { LogIn, LogOut, Users, AlertCircle, Loader2, Ship } from 'lucide-vue-next';
import keycloak from './keycloak';
import { API_BASE_URL, type User } from './types';

const authenticated = ref(false);
const username = ref('');
const users = ref<User[]>([]);
const loading = ref(false);
const error = ref('');

// Initialize Keycloak
onMounted(async () => {
  try {
    const auth = await keycloak.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false
    });

    authenticated.value = auth;

    if (auth && keycloak.tokenParsed) {
      username.value = keycloak.tokenParsed.preferred_username as string;
    }

    // Auto refresh token
    keycloak.onTokenExpired = () => {
      keycloak.updateToken(30).then((refreshed) => {
        if (refreshed) {
          console.log('Token refreshed');
        }
      }).catch(() => {
        console.error('Failed to refresh token');
        keycloak.login();
      });
    };
  } catch (err) {
    console.error('Failed to initialize Keycloak', err);
    error.value = 'Failed to initialize authentication';
  }
});

const login = () => {
  keycloak.login();
};

const logout = () => {
  keycloak.logout();
};

const fetchUsers = async () => {
  if (!keycloak.token) {
    error.value = 'No authentication token available';
    return;
  }

  loading.value = true;
  error.value = '';

  try {
    const response = await fetch(`${API_BASE_URL}/api/users`, {
      headers: {
        'Authorization': `Bearer ${keycloak.token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    users.value = data;
  } catch (err) {
    console.error('Failed to fetch users', err);
    error.value = err instanceof Error ? err.message : 'Failed to fetch users';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
    <div class="container mx-auto px-4 py-8 max-w-6xl">
      <!-- Header -->
      <header class="text-center mb-8">
        <div class="flex items-center justify-center gap-3 mb-2">
          <Ship :size="40" class="text-indigo-600" />
          <h1 class="text-4xl font-bold text-gray-800">PLC Demo App</h1>
        </div>
        <p class="text-gray-600">Vue 3 + Keycloak Authorization Code Flow + PKCE</p>
      </header>

      <!-- Error Alert -->
      <div v-if="error" class="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
        <AlertCircle class="text-red-500 flex-shrink-0 mt-0.5" :size="20" />
        <div>
          <p class="text-red-800 font-medium">Error</p>
          <p class="text-red-600 text-sm">{{ error }}</p>
        </div>
      </div>

      <!-- Not Authenticated -->
      <div v-if="!authenticated" class="bg-white rounded-xl shadow-lg p-12 text-center">
        <div class="mb-6">
          <div class="inline-block p-4 bg-indigo-100 rounded-full mb-4">
            <LogIn :size="48" class="text-indigo-600" />
          </div>
          <h2 class="text-2xl font-bold text-gray-800 mb-2">Welcome!</h2>
          <p class="text-gray-600">Please login to continue with Keycloak</p>
        </div>
        <button
          @click="login"
          class="inline-flex items-center gap-2 px-6 py-3 bg-indigo-600 text-white rounded-lg font-medium hover:bg-indigo-700 transition-colors"
        >
          <LogIn :size="20" />
          Login with Keycloak
        </button>
      </div>

      <!-- Authenticated -->
      <div v-else class="space-y-6">
        <!-- User Info Card -->
        <div class="bg-white rounded-xl shadow-lg p-6">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-sm text-gray-500 mb-1">Logged in as</p>
              <h2 class="text-2xl font-bold text-gray-800">{{ username }}</h2>
            </div>
            <div class="flex gap-3">
              <button
                @click="logout"
                class="inline-flex items-center gap-2 px-4 py-2 bg-gray-600 text-white rounded-lg font-medium hover:bg-gray-700 transition-colors"
              >
                <LogOut :size="18" />
                Logout
              </button>
            </div>
          </div>
        </div>

        <!-- API Actions Card -->
        <div class="bg-white rounded-xl shadow-lg p-6">
          <h3 class="text-xl font-bold text-gray-800 mb-4">User Service API</h3>
          <button
            @click="fetchUsers"
            :disabled="loading"
            class="inline-flex items-center gap-2 px-6 py-3 bg-indigo-600 text-white rounded-lg font-medium hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Loader2 v-if="loading" :size="20" class="animate-spin" />
            <Users v-else :size="20" />
            {{ loading ? 'Loading...' : 'Get All Users' }}
          </button>

          <!-- Users Table -->
          <div v-if="users.length > 0" class="mt-6">
            <div class="flex items-center justify-between mb-4">
              <h4 class="text-lg font-semibold text-gray-700">Users ({{ users.length }})</h4>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full">
                <thead>
                  <tr class="bg-gray-50 border-b border-gray-200">
                    <th class="px-4 py-3 text-left text-sm font-semibold text-gray-600">Username</th>
                    <th class="px-4 py-3 text-left text-sm font-semibold text-gray-600">Email</th>
                    <th class="px-4 py-3 text-left text-sm font-semibold text-gray-600">First Name</th>
                    <th class="px-4 py-3 text-left text-sm font-semibold text-gray-600">Last Name</th>
                    <th class="px-4 py-3 text-left text-sm font-semibold text-gray-600">Role</th>
                    <th class="px-4 py-3 text-left text-sm font-semibold text-gray-600">Created At</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200">
                  <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50 transition-colors">
                    <td class="px-4 py-3 text-sm font-medium text-gray-900">{{ user.username }}</td>
                    <td class="px-4 py-3 text-sm text-gray-600">{{ user.email }}</td>
                    <td class="px-4 py-3 text-sm text-gray-600">{{ user.firstName || '-' }}</td>
                    <td class="px-4 py-3 text-sm text-gray-600">{{ user.lastName || '-' }}</td>
                    <td class="px-4 py-3 text-sm">
                      <span
                        v-if="user.role"
                        :class="user.role === 'admin'
                          ? 'bg-purple-100 text-purple-800'
                          : 'bg-blue-100 text-blue-800'"
                        class="px-2 py-1 rounded-full text-xs font-medium"
                      >
                        {{ user.role }}
                      </span>
                      <span v-else class="text-gray-400">-</span>
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-600">
                      {{ new Date(user.createdAt).toLocaleString() }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
