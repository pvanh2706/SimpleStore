import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: 'html',
  use: {
    baseURL: 'https://127.0.0.1:4173',
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'pnpm --dir ../../src/frontend/simplestore-web dev --host 127.0.0.1 --port 4173',
    url: 'https://127.0.0.1:4173',
    ignoreHTTPSErrors: true,
    reuseExistingServer: !process.env.CI,
  },
})
