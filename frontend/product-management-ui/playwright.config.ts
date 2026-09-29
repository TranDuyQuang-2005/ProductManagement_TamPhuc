import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    channel: 'chrome',
    launchOptions: {
      args: ['--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox']
    }
  },
  projects: [
    {
      name: 'chrome',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
});
