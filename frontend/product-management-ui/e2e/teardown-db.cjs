const { dropDatabaseIfExists } = require('./db-common.cjs');

async function teardown() {
  await dropDatabaseIfExists();
}

module.exports = teardown;

if (require.main === module) {
  teardown().catch(error => {
    console.error(error);
    process.exit(1);
  });
}
