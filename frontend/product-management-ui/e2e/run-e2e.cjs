const { spawn, spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const e2eDatabase = process.env.PRODUCTMANAGEMENT_E2E_DATABASE ?? 'ProductManagement_E2E_Test';
const connectionString =
  process.env.PRODUCTMANAGEMENT_E2E_CONNECTION
  ?? `Server=localhost,1433;Database=${e2eDatabase};User Id=sa;Password=123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True`;

const children = [];
const localAppData = path.resolve(__dirname, '.localappdata');
fs.mkdirSync(localAppData, { recursive: true });

function run(command, args, useShell = false, timeout = 0) {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    shell: useShell,
    timeout
  });

  if (result.error) {
    console.error(result.error);
  }

  return result;
}

const node = process.execPath;
const playwrightCli = path.resolve(__dirname, '..', 'node_modules', '@playwright', 'test', 'cli.js');
const angularCli = path.resolve(__dirname, '..', 'node_modules', '@angular', 'cli', 'bin', 'ng.js');

function start(command, args, options = {}) {
  const child = spawn(command, args, {
    stdio: options.stdio ?? 'inherit',
    shell: options.shell ?? false,
    env: { ...process.env, ...options.env }
  });
  children.push(child);
  child.on('exit', code => {
    if (code !== null && code !== 0) {
      console.error(`${command} exited with code ${code}`);
    }
  });
  return child;
}

async function waitForUrl(url, timeoutMs) {
  const startTime = Date.now();
  while (Date.now() - startTime < timeoutMs) {
    try {
      const response = await fetch(url);
      if (response.ok) return;
    } catch {
      // Server is still starting.
    }

    await new Promise(resolve => setTimeout(resolve, 500));
  }

  throw new Error(`Timed out waiting for ${url}`);
}

function killChildren() {
  for (const child of children.reverse()) {
    if (!child.pid || child.exitCode !== null) continue;

    child.kill();
    if (process.platform === 'win32') {
      spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore', timeout: 5_000 });
    } else {
      child.kill('SIGTERM');
    }
  }
}

const setup = run(node, ['e2e/setup-db.cjs']);
if (setup.status !== 0) {
  process.exit(setup.status ?? 1);
}

let testStatus = 1;
let teardownStatus = 1;

(async () => {
  try {
    start('dotnet', [
      'run',
      '--no-restore',
      '--no-launch-profile',
      '--project',
      '../../backend/ProductManagement.Api/ProductManagement.Api.csproj',
      '--urls',
      'http://localhost:5188'
    ], {
      stdio: 'ignore',
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        LOCALAPPDATA: localAppData,
        Logging__LogLevel__Default: 'Warning',
        Logging__LogLevel__Microsoft: 'Warning',
        'Logging__LogLevel__Microsoft.AspNetCore.DataProtection': 'None',
        'Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Transaction': 'None',
        ConnectionStrings__DefaultConnection: connectionString,
        AdminSeed__Username: 'admin',
        AdminSeed__Password: 'ChangeMeAdmin123',
        AdminSeed__FullName: 'E2E Admin',
        AdminSeed__Email: 'admin.e2e@example.local',
        StaffSeed__Password: '',
        Jwt__Issuer: 'ProductManagement.Api',
        Jwt__Audience: 'ProductManagement.Ui',
        Jwt__Secret: 'ProductManagement_E2E_Secret_Key_1234567890',
        Jwt__ExpirationMinutes: '120'
      }
    });
    await waitForUrl('http://localhost:5188/swagger/index.html', 120_000);

    start(node, [angularCli, 'serve', '--host', 'localhost', '--port', '4200'], { stdio: 'ignore' });
    await waitForUrl('http://localhost:4200', 120_000);

    const tests = run(node, [playwrightCli, 'test']);
    testStatus = tests.status ?? 1;
  } catch (error) {
    console.error(error);
    testStatus = 1;
  } finally {
    killChildren();
    const teardown = run(node, ['e2e/teardown-db.cjs'], false, 30_000);
    teardownStatus = teardown.status ?? 1;
  }

  if (testStatus !== 0) {
    process.exit(testStatus);
  }

  process.exit(teardownStatus);
})();
