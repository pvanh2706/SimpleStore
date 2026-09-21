import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { setUnauthorizedHandler } from './api/client'
import { useAuthStore } from './stores/auth'
import './styles.css'

const pinia = createPinia()
setUnauthorizedHandler(() => {
  const auth = useAuthStore(pinia)
  auth.$reset()
  auth.initialized = true
  void router.push({ name: 'login' })
})

createApp(App).use(pinia).use(router).mount('#app')
