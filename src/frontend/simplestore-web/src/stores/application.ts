import { defineStore } from 'pinia'

export const useApplicationStore = defineStore('application', {
  state: () => ({
    name: 'SimpleStore',
    foundationStatus: 'Sẵn sàng',
  }),
})
