import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import AppButton from './AppButton.vue'

describe('AppButton', () => {
  it('is a non-submitting button by default and passes native attributes and clicks through', async () => {
    const onClick = vi.fn()
    const wrapper = mount(AppButton, {
      attrs: { 'aria-label': 'Mở menu', onClick },
      slots: { default: 'Menu' },
    })

    const button = wrapper.get('button')
    expect(button.attributes('type')).toBe('button')
    expect(button.attributes('aria-label')).toBe('Mở menu')
    expect(button.text()).toBe('Menu')
    await button.trigger('click')
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('supports primary, secondary, danger and text roles with native button semantics', async () => {
    for (const variant of ['primary', 'secondary', 'danger', 'text'] as const) {
      const wrapper = mount(AppButton, { props: { variant, type: 'submit' } })
      expect(wrapper.get('button').classes()).toContain(`app-button--${variant}`)
      expect(wrapper.get('button').attributes('type')).toBe('submit')
    }
  })

  it('prevents click handling when disabled', async () => {
    const onClick = vi.fn()
    const wrapper = mount(AppButton, { props: { disabled: true }, attrs: { onClick } })
    const button = wrapper.get('button')
    expect(button.attributes('disabled')).toBeDefined()
    await button.trigger('click')
    expect(onClick).not.toHaveBeenCalled()
  })
})
