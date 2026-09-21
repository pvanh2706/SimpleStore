import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import ProductForm from './ProductForm.vue'

describe('ProductForm', () => {
  it('requires opening cost when opening quantity is positive', async () => {
    const wrapper = mount(ProductForm)
    await wrapper.get('#name').setValue('Nước suối')
    await wrapper.get('#unit').setValue('chai')
    await wrapper.get('#sale-price').setValue('10000')
    await wrapper.get('#opening-quantity').setValue('2')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.get('[role="alert"]').text()).toContain('giá vốn đầu')
    expect(wrapper.emitted('submit')).toBeUndefined()
  })

  it('emits normalized numeric input when valid', async () => {
    const wrapper = mount(ProductForm)
    await wrapper.get('#name').setValue('Nước suối')
    await wrapper.get('#unit').setValue('chai')
    await wrapper.get('#sale-price').setValue('10000')
    await wrapper.get('#opening-quantity').setValue('2')
    await wrapper.get('#opening-cost').setValue('5000')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]?.[0]).toMatchObject({ salePrice: 10000, openingQuantity: 2, openingCost: 5000 })
  })
})
