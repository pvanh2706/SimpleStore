import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it } from 'vitest'
import App from './App.vue'
import HomeView from './views/HomeView.vue'

describe('application shell', () => {
  it('renders the engineering foundation status', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/', component: HomeView }],
    })
    router.push('/')
    await router.isReady()

    const wrapper = mount(App, {
      global: {
        plugins: [createPinia(), router],
      },
    })

    expect(wrapper.get('h1').text()).toContain('vertical slice đầu tiên')
    expect(wrapper.text()).toContain('Nền tảng: Sẵn sàng')
  })
})
