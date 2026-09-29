import { expect, Page, test } from '@playwright/test';

const adminUsername = 'admin';
const adminPassword = 'ChangeMeAdmin123';
const categoryLabel = 'E2E-CAT - E2E Category';

test.beforeEach(async ({ page }) => {
  await login(page);
});

test('ADMIN login succeeds', async ({ page }) => {
  await expect(page.getByTestId('nav-products')).toBeVisible();
  await expect(page.getByText('E2E Admin')).toBeVisible();
});

test('Create Product shows success, appears in list, and starts with zero stock', async ({ page }) => {
  const name = uniqueName('Create');

  const code = await createProduct(page, { name, price: '12.34' });

  await expect(page.locator('.alert.success')).toContainText(code);
  await searchProduct(page, code);
  await expect(productRow(page, code)).toContainText(name);
  await expectStock(page, code, '0');
});

test('Validation UI blocks invalid Product create', async ({ page }) => {
  await page.getByTestId('add-product').click();
  await page.getByTestId('product-category').selectOption({ label: categoryLabel });
  await page.getByTestId('product-name').fill('');
  await page.getByTestId('product-unit').fill('kg');
  await page.getByTestId('product-price').fill('-0.01');

  const createRequests: string[] = [];
  page.on('request', request => {
    if (request.method() === 'POST' && request.url().endsWith('/api/products/create')) {
      createRequests.push(request.url());
    }
  });

  await page.getByTestId('product-submit').click();

  await expect(page.getByTestId('product-name')).toHaveClass(/ng-invalid/);
  await expect(page.getByTestId('product-price')).toHaveClass(/ng-invalid/);
  expect(createRequests).toHaveLength(0);
});

test('Search handles exact code, code prefix, Unicode name and special keywords', async ({ page }) => {
  const unicodeName = uniqueName('Cafe sua da');
  const code = await createProduct(page, { name: unicodeName, price: '19.99' });
  const prefix = code.slice(0, 4);

  await searchProduct(page, code);
  await expect(productRow(page, code)).toContainText(unicodeName);

  await searchProduct(page, prefix);
  await expect(productRow(page, code)).toBeVisible();

  await searchProduct(page, unicodeName);
  await expect(productRow(page, code)).toBeVisible();

  for (const keyword of ['%', '_', "'"]) {
    await searchProduct(page, keyword);
    await expect(page.locator('.alert.error')).toHaveCount(0);
    await expect(productRow(page, code)).toHaveCount(0);
  }
});

test('Stock-in accumulates stock, metadata update preserves stock, and history records STOCK_IN', async ({ page }) => {
  const name = uniqueName('StockIn');
  const code = await createProduct(page, { name, price: '10' });

  await expectStock(page, code, '0');
  await stockInProduct(page, code, '10', 'PN-10');
  await expectStock(page, code, '10');
  await stockInProduct(page, code, '2.5', 'PN-2-5');
  await expectStock(page, code, '12.5');

  await updateProduct(page, code, { name: `${name} updated`, price: '88.25' });

  await searchProduct(page, code);
  await expect(productRow(page, code)).toContainText('88.25');
  await expect(productRow(page, code)).toContainText(`${name} updated`);
  await expectStock(page, code, '12.5');

  await openHistory(page, code);
  await expect(page.getByTestId(`history-row-STOCK_IN-${code}`)).toHaveCount(2);
});

test('Soft Delete moves zero-stock Product from normal list to Trash', async ({ page }) => {
  const code = await createProduct(page, { name: uniqueName('SoftDelete') });

  await softDeleteProduct(page, code);
  await expect(productRow(page, code)).toHaveCount(0);
  await openTrash(page);
  await expect(trashRow(page, code)).toBeVisible();
});

test('Restore moves zero-stock Product from Trash back to normal list', async ({ page }) => {
  const code = await createProduct(page, { name: uniqueName('Restore') });
  await softDeleteProduct(page, code);

  await openTrash(page);
  await restoreProductFromTrash(page, code);
  await closeTrash(page);
  await searchProduct(page, code);
  await expect(productRow(page, code)).toBeVisible();
});

test('Permanent Delete removes zero-stock Product from Trash', async ({ page }) => {
  const code = await createProduct(page, { name: uniqueName('PermanentDelete') });
  await softDeleteProduct(page, code);

  await openTrash(page);
  await permanentlyDeleteFromTrash(page, code);

  await expect(trashRow(page, code)).toHaveCount(0);
});

test('Audit Log contains product CREATE UPDATE STOCK_IN DELETE RESTORE and PERMANENT_DELETE actions', async ({ page }) => {
  const stockCode = await createProduct(page, { name: uniqueName('AuditStock'), price: '10' });
  await stockInProduct(page, stockCode, '1', 'AUD-1');
  await updateProduct(page, stockCode, { price: '15' });

  await page.getByTestId('nav-audit').click();
  await page.getByTestId('audit-entity-code').fill(stockCode);
  await page.getByTestId('audit-search-submit').click();

  await expect(auditRow(page, 'CREATE', stockCode)).toBeVisible();
  await expect(auditRow(page, 'UPDATE', stockCode)).toBeVisible();
  await expect(auditRow(page, 'STOCK_IN', stockCode)).toBeVisible();

  const lifecycleCode = await createProduct(page, { name: uniqueName('AuditLifecycle'), price: '10' });
  await softDeleteProduct(page, lifecycleCode);
  await openTrash(page);
  await restoreProductFromTrash(page, lifecycleCode);
  await closeTrash(page);
  await softDeleteProduct(page, lifecycleCode);
  await openTrash(page);
  await permanentlyDeleteFromTrash(page, lifecycleCode);
  await expect(trashRow(page, lifecycleCode)).toHaveCount(0);
  await closeTrash(page);

  await page.getByTestId('nav-audit').click();
  await page.getByTestId('audit-entity-code').fill(lifecycleCode);
  await page.getByTestId('audit-search-submit').click();

  await expect(auditRow(page, 'DELETE', lifecycleCode)).toBeVisible();
  await expect(auditRow(page, 'RESTORE', lifecycleCode)).toBeVisible();
  await expect(auditRow(page, 'PERMANENT_DELETE', lifecycleCode)).toBeVisible();
});

test('Full lifecycle for zero-stock Product covers create search update soft delete trash restore permanent delete', async ({ page }) => {
  const code = await createProduct(page, { name: uniqueName('Lifecycle'), price: '21' });

  await searchProduct(page, code);
  await expect(productRow(page, code)).toBeVisible();

  await updateProduct(page, code, { price: '31' });

  await searchProduct(page, code);
  await expectStock(page, code, '0');
  await softDeleteProduct(page, code);
  await openTrash(page);
  await expect(trashRow(page, code)).toBeVisible();
  await restoreProductFromTrash(page, code);
  await closeTrash(page);
  await searchProduct(page, code);
  await expect(productRow(page, code)).toBeVisible();

  await softDeleteProduct(page, code);
  await openTrash(page);
  await permanentlyDeleteFromTrash(page, code);
  await expect(trashRow(page, code)).toHaveCount(0);
});

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByTestId('login-username').fill(adminUsername);
  await page.getByTestId('login-password').fill(adminPassword);
  await page.getByTestId('login-submit').click();
  await expect(page).toHaveURL(/\/products$/);
}

async function createProduct(
  page: Page,
  values: { name: string; price?: string; unit?: string }
): Promise<string> {
  await page.goto('/products');
  await page.getByTestId('add-product').click();
  await page.getByTestId('product-category').selectOption({ label: categoryLabel });
  await page.getByTestId('product-name').fill(values.name);
  await page.getByTestId('product-unit').fill(values.unit ?? 'kg');
  await page.getByTestId('product-price').fill(values.price ?? '10');
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/create') && response.request().method() === 'POST' && response.ok()),
    page.getByTestId('product-submit').click()
  ]);
  await expect(page).toHaveURL(/\/products$/);
  await expect(page.locator('.alert.success')).toBeVisible();

  const row = page.locator('tbody tr', { hasText: values.name }).first();
  await expect(row).toBeVisible();
  return (await row.locator('td').first().innerText()).trim();
}

async function updateProduct(
  page: Page,
  code: string,
  values: { name?: string; price: string }
): Promise<void> {
  await searchProduct(page, code);
  await productRow(page, code).getByTestId(`edit-product-${code}`).click();
  await expect(page.getByTestId('product-code')).toHaveValue(code);
  if (values.name) {
    await page.getByTestId('product-name').fill(values.name);
  }
  await page.getByTestId('product-price').fill(values.price);

  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/update') && response.request().method() === 'POST' && response.ok()),
    page.getByTestId('product-submit').click()
  ]);
  await expect(page).toHaveURL(/\/products$/);
  await expect(page.locator('.alert.success')).toBeVisible();
}

async function stockInProduct(page: Page, code: string, quantity: string, referenceCode: string): Promise<void> {
  await searchProduct(page, code);
  await productRow(page, code).getByTestId(`stock-in-product-${code}`).click();
  await page.getByTestId('stock-in-quantity').fill(quantity);
  await page.getByTestId('stock-in-reference').fill(referenceCode);
  await page.getByTestId('stock-in-note').fill(`E2E stock-in ${quantity}`);

  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/inventory/stock-in') && response.request().method() === 'POST' && response.ok()),
    page.getByTestId('stock-in-submit').click()
  ]);
  await expect(page.locator('.modal-backdrop')).toHaveCount(0);
}

async function openHistory(page: Page, code: string): Promise<void> {
  await searchProduct(page, code);
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/history') && response.request().method() === 'POST' && response.ok()),
    productRow(page, code).getByTestId(`history-product-${code}`).click()
  ]);
}

async function searchProduct(page: Page, keyword: string): Promise<void> {
  await page.goto('/products');
  await page.getByTestId('product-search-keyword').fill(keyword);
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/search') && response.request().method() === 'POST' && response.ok()),
    page.getByTestId('product-search-submit').click()
  ]);
  await expect(page.locator('.loading')).toHaveCount(0);
}

async function softDeleteProduct(page: Page, code: string): Promise<void> {
  await searchProduct(page, code);
  await productRow(page, code).getByTestId(`delete-product-${code}`).click();
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/delete') && response.request().method() === 'POST' && response.ok()),
    page.getByTestId('confirm-accept').click()
  ]);
  await expect(productRow(page, code)).toHaveCount(0);
}

async function openTrash(page: Page): Promise<void> {
  await page.getByTestId('open-trash').click();
  await expect(page.locator('.trash-modal')).toBeVisible();
}

async function restoreProductFromTrash(page: Page, code: string): Promise<void> {
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/restore') && response.request().method() === 'POST' && response.ok()),
    trashRow(page, code).getByTestId(`restore-product-${code}`).click()
  ]);
  await expect(trashRow(page, code)).toHaveCount(0);
}

async function permanentlyDeleteFromTrash(page: Page, code: string): Promise<void> {
  await trashRow(page, code).getByTestId(`permanent-delete-product-${code}`).click();
  await Promise.all([
    page.waitForResponse(response => response.url().endsWith('/api/products/delete-permanent') && response.request().method() === 'POST' && response.ok()),
    page.getByTestId('confirm-accept').click()
  ]);
}

async function closeTrash(page: Page): Promise<void> {
  await page.locator('.trash-modal .modal-header .btn.small').click();
  await expect(page.locator('.trash-modal')).toHaveCount(0);
}

async function expectStock(page: Page, code: string, expected: string): Promise<void> {
  await expect(productRow(page, code).locator('td').nth(5)).toContainText(expected);
}

function productRow(page: Page, code: string) {
  return page.getByTestId(`product-row-${code}`);
}

function trashRow(page: Page, code: string) {
  return page.getByTestId(`trash-row-${code}`);
}

function auditRow(page: Page, action: string, code: string) {
  return page.getByTestId(`audit-row-${action}-${code}`).first();
}

function uniqueName(prefix: string): string {
  return `${prefix} ${Date.now()} ${Math.random().toString(16).slice(2, 8)}`;
}
