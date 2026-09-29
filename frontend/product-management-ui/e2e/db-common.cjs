const fs = require('fs');
const path = require('path');
const sql = require('mssql');

const databaseName = process.env.PRODUCTMANAGEMENT_E2E_DATABASE || 'ProductManagement_E2E_Test';
const root = path.resolve(__dirname, '..', '..', '..');
const dbConfig = {
  server: process.env.PRODUCTMANAGEMENT_E2E_SQLSERVER || 'localhost',
  port: Number(process.env.PRODUCTMANAGEMENT_E2E_SQLPORT || '1433'),
  user: process.env.PRODUCTMANAGEMENT_E2E_SQLUSER || 'sa',
  password: process.env.PRODUCTMANAGEMENT_E2E_SQLPASSWORD || '123',
  database: 'master',
  options: {
    encrypt: false,
    trustServerCertificate: true
  }
};

function quoteName(value) {
  return `[${value.replace(/]/g, ']]')}]`;
}

function escapeLiteral(value) {
  return value.replace(/'/g, "''");
}

async function connect(database = 'master') {
  return await sql.connect({ ...dbConfig, database });
}

async function executeBatch(pool, batch) {
  if (!batch.trim()) return;
  await pool.request().batch(batch);
}

async function executeScript(pool, relativePath) {
  const scriptPath = path.join(root, relativePath);
  const raw = fs
    .readFileSync(scriptPath, 'utf8')
    .replace(/USE \[ProductManagementDB\];/gi, `USE ${quoteName(databaseName)};`);

  const batches = raw.split(/^\s*GO\s*$/gim);
  for (const batch of batches) {
    await executeBatch(pool, batch);
  }
}

async function dropDatabaseIfExists() {
  const pool = await connect('master');
  try {
    await executeBatch(pool, `
IF DB_ID(N'${escapeLiteral(databaseName)}') IS NOT NULL
BEGIN
  ALTER DATABASE ${quoteName(databaseName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE ${quoteName(databaseName)};
END;
`);
  } finally {
    await pool.close();
  }
}

module.exports = {
  connect,
  databaseName,
  dropDatabaseIfExists,
  executeBatch,
  executeScript,
  quoteName
};
