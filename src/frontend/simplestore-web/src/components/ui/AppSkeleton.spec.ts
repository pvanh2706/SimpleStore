import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import AppSkeleton from './AppSkeleton.vue'

describe('AppSkeleton', () => {
  it('exposes an accessible loading status', () => {
    const wrapper = mount(AppSkeleton)
    expect(wrapper.attributes('role')).toBe('status')
    expect(wrapper.attributes('aria-label')).toBe('Đang tải')
  })
})
