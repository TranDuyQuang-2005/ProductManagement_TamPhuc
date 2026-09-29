const {
  connect,
  databaseName,
  dropDatabaseIfExists,
  executeBatch,
  executeScript,
  quoteName
} = require('./db-common.cjs');

(async () => {
  if (databaseName.toLowerCase() === 'productmanagementdb') {
    throw new Error('Refusing to run E2E tests against ProductManagementDB development database.');
  }

  await dropDatabaseIfExists();

  const master = await connect('master');
  try {
    await executeBatch(master, `
CREATE DATABASE ${quoteName(databaseName)};
ALTER DATABASE ${quoteName(databaseName)} SET RECOVERY SIMPLE;
`);
  } finally {
    await master.close();
  }

  const pool = await connect(databaseName);
  try {
    await executeScript(pool, 'database/02_CreateTables.sql');
    await executeScript(pool, 'database/03_CreateIndexes.sql');
    await executeBatch(pool, `
INSERT dbo.Categories
  (CategoryCode, CategoryName, CodePrefix, NextProductNumber, IsActive,
   CreatedByUserId, IsAdminProtected, CreatedAt)
VALUES
  (N'E2E-CAT', N'E2E Category', N'E2', 1, 1, NULL, 1, SYSUTCDATETIME());
`);
  } finally {
    await pool.close();
  }
})().catch(error => {
  console.error(error);
  process.exit(1);
});
