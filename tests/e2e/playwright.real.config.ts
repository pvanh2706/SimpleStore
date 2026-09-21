import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './real-specs',
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'https://127.0.0.1:4173',
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../../src/backend/SimpleStore.Api --configuration Release --no-build --no-launch-profile',
      url: 'https://localhost:7237/health',
      ignoreHTTPSErrors: true,
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command: 'pnpm --dir ../../src/frontend/simplestore-web dev --host 127.0.0.1 --port 4173',
      url: 'https://127.0.0.1:4173',
      ignoreHTTPSErrors: true,
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
})
