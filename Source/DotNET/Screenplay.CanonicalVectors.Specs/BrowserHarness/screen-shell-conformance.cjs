// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Conformance gate for the application shell (spec 13.1, Cratis/Screenplay#766): the published `screen-shell-navigation-runnable` vector is run on a live
// Stage with the public `cratis run`, and a browser asserts what a person sees and does - the shell's topbar and sidebar and where the items lead,
// module-level inherit / extend / override swapping the sidebar, a toolbar button that opens the State Change slice's task dialog and posts to the registered
// route, a deep link restoring the module and feature, no sample data in what is served. The shell it draws for each module is written as a tree
// (`stage-tree.json`); the Studio side (`check-studio-production-shell-preview.mjs` of Cratis/Studio, run against production Studio) writes the tree
// its Preview draws, and when it is given (SCREENPLAY_STUDIO_TREE) the two are compared: the same shell in Studio preview and Stage.
//
//   node screen-shell-conformance.cjs --self-test
//   SCREENPLAY_SHELL_VECTOR=<folder holding application.play> SCREENPLAY_SHELL_RESULT=<result.json> [SCREENPLAY_STUDIO_TREE=<studio-tree.json>] node screen-shell-conformance.cjs
//
//   SCREENPLAY_SHELL_PORT        Stage port (default 19480); the workbench uses this plus 16000.
//   SCREENPLAY_STAGE_TAG         pin a Stage image instead of the CLI default.
//
// Exit codes: 0 every assertion passed, 1 an assertion failed, 2 the run could not start.
const { spawn, execFileSync } = require('node:child_process');
const { mkdirSync, readFileSync, writeFileSync } = require('node:fs');
const { dirname, join, resolve } = require('node:path');


const sampleDataMarker = 'Sample customer that must never ship';

// What Stage must show for the vector, module by module: the application topbar, the sidebar the module's region mode yields (Home inherits the application's,
// Sales overrides it, Support extends it), the page and the toolbar buttons of that page.
const expectedShell = {
    Home: { topbar: ['Home', 'Sales', 'Support'], sidebar: ['Overview'], active: { topbar: 'Home', sidebar: 'Overview' }, title: 'Welcome', toolbar: [] },
    Sales: { topbar: ['Home', 'Sales', 'Support'], sidebar: ['Orders', 'Customers'], active: { topbar: 'Sales', sidebar: 'Orders' }, title: 'Orders', toolbar: ['New order'] },
    Support: { topbar: ['Home', 'Sales', 'Support'], sidebar: ['Overview', 'Tickets', 'Knowledge base'], active: { topbar: 'Support', sidebar: 'Tickets' }, title: 'Tickets', toolbar: [] },
};

function compareShells(stage, studio, name) {
    const failures = [];
    const labels = items => (items ?? []).map(item => item.label);
    const active = items => (items ?? []).filter(item => item.active).map(item => item.label);
    for (const [field, getter] of [['topbar items', shell => labels(shell.topbar)], ['topbar active', shell => active(shell.topbar)], ['sidebar items', shell => labels(shell.sidebar)], ['sidebar active', shell => active(shell.sidebar)], ['page title', shell => shell.title], ['toolbar buttons', shell => shell.toolbar]]) {
        const left = JSON.stringify(getter(stage));
        const right = JSON.stringify(getter(studio));
        if (left !== right) failures.push(`${name}: ${field} differ - Stage ${left}, Studio ${right}`);
    }
    return failures;
}

if (process.argv.includes('--self-test')) {
    let passed = 0;
    const total = 6;
    const same = { topbar: [{ label: 'A', active: true }], sidebar: [{ label: 'B', active: true }], title: ['T'], toolbar: ['X'] };
    const ok = (name, condition) => { if (condition) passed++; else console.error(`self-test failed: ${name}`); };
    ok('identical shells match', compareShells(same, structuredClone(same), 'm').length === 0);
    ok('a missing topbar item is reported', compareShells(same, { ...same, topbar: [] }, 'm').length === 2);
    ok('a different active sidebar item is reported', compareShells(same, { ...same, sidebar: [{ label: 'B', active: false }] }, 'm').length === 1);
    ok('a different title is reported', compareShells(same, { ...same, title: ['U'] }, 'm').length === 1);
    ok('a different toolbar is reported', compareShells(same, { ...same, toolbar: [] }, 'm').length === 1);
    ok('the expected shells name the three modules', Object.keys(expectedShell).join() === 'Home,Sales,Support');
    console.log(`shell conformance self-test: ${passed} of ${total} checks passed`);
    process.exit(passed === total ? 0 : 1);
}

const { chromium } = require('playwright');

const vector = process.env.SCREENPLAY_SHELL_VECTOR;
const resultPath = resolve(process.env.SCREENPLAY_SHELL_RESULT ?? '.ai-work/acceptance/screen-shell-conformance.json');
const studioTreePath = process.env.SCREENPLAY_STUDIO_TREE;
const port = Number(process.env.SCREENPLAY_SHELL_PORT ?? 19480);
const base = `http://127.0.0.1:${port}`;
if (!vector) {
    console.error('screen-shell-conformance: set SCREENPLAY_SHELL_VECTOR to the folder holding the vector\'s application.play');
    process.exit(2);
}

const passed = [];
const failures = [];
const check = (name, actual, expected) => {
    const same = JSON.stringify(actual) === JSON.stringify(expected);
    (same ? passed : failures).push(same ? name : `${name}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
};
const sleep = ms => new Promise(done => setTimeout(done, ms));

async function waitForReady(runtime) {
    const deadline = Date.now() + 180_000;
    while (Date.now() < deadline) {
        if (runtime.exitCode !== null) throw new Error(`cratis run exited before readiness with code ${runtime.exitCode}`);
        try { if ((await fetch(`${base}/index.html`)).ok) return; } catch { /* the runtime has not bound the port yet */ }
        await sleep(1_000);
    }
    throw new Error(`timed out waiting for ${base}`);
}

function stageContainer() {
    try {
        const listing = execFileSync('docker', ['ps', '-a', '--filter', `publish=${port}`, '--format', '{{.ID}} {{.Image}}'], { encoding: 'utf8' }).trim();
        const [id, image] = listing.split('\n')[0].split(' ');
        return id ? { id, image } : undefined;
    } catch { return undefined; }
}

(async () => {
    const tag = process.env.SCREENPLAY_STAGE_TAG ? ['--tag', process.env.SCREENPLAY_STAGE_TAG] : [];
    const runtime = spawn('cratis', ['run', vector, ...tag, '--port', String(port), '--workbench-port', String(port + 16000), '--yes'], { stdio: ['ignore', 'pipe', 'pipe'] });
    const log = [];
    runtime.stdout.on('data', chunk => log.push(String(chunk)));
    runtime.stderr.on('data', chunk => log.push(String(chunk)));
    const result = { name: 'screen-shell-conformance', cli: execFileSync('cratis', ['--version'], { encoding: 'utf8' }).trim(), status: 'running', stage: {}, tree: { modules: {} } };
    let browser;
    try {
        await waitForReady(runtime);
        const container = stageContainer();
        result.stage = { image: container?.image ?? null };
        browser = await chromium.launch();
        const page = await browser.newPage({ viewport: { width: 1500, height: 900 } });
        const errors = [];
        page.on('pageerror', error => errors.push(String(error)));
        const posts = [];
        page.on('request', request => { if (request.method() === 'POST' && request.url().includes('/api/')) posts.push({ url: new URL(request.url()).pathname, body: JSON.parse(request.postData() || '{}') }); });

        const shellOf = () => page.evaluate(() => ({
            topbar: [...document.querySelectorAll('nav[aria-label="Modules"] a, nav[aria-label="Modules"] button')].map(item => ({ label: item.innerText.trim(), active: item.hasAttribute('aria-current') })),
            sidebar: [...document.querySelectorAll('nav[aria-label="Sidebar"] a, nav[aria-label="Sidebar"] button')].map(item => ({ label: item.innerText.trim(), active: item.hasAttribute('aria-current') })),
            title: [...document.querySelectorAll('h1')].map(heading => heading.innerText.trim()),
            toolbar: [...new Set([...document.querySelectorAll('[data-scene-id$="-button"][data-scene-id*="toolbar"]')].map(button => button.innerText.trim()))],
        }));

        // (6) sample data never ships: not in what Stage serves.
        const sceneText = await (await fetch(`${base}/stage/scene`)).text();
        check('the served scene carries no sample data', sceneText.includes(sampleDataMarker), false);
        const scene = JSON.parse(sceneText);
        check('the served scene carries no sample data selection in a profile', scene.uiProfiles.every(profile => profile.sampleData === undefined || profile.sampleData === null), true);

        // (1) the shell: topbar and sidebar items, each linked to a module/feature, rendered by Stage.
        await page.goto(base);
        await page.waitForSelector('.layout-topbar');
        check('the application topbar shows the modules', (await shellOf()).topbar.map(item => item.label), ['Home', 'Sales', 'Support']);

        // (2) inherit / extend / override per module, and where the items lead.
        for (const module of ['Sales', 'Support', 'Home']) {
            await page.locator('nav[aria-label="Modules"]').getByText(module, { exact: true }).click();
            await page.waitForFunction(title => [...document.querySelectorAll('h1')].some(heading => heading.innerText.trim() === title), expectedShell[module].title);
            const shell = await shellOf();
            result.tree.modules[module] = shell;
            const expected = expectedShell[module];
            check(`${module}: the topbar items`, shell.topbar.map(item => item.label), expected.topbar);
            check(`${module}: the sidebar the module's region mode yields`, shell.sidebar.map(item => item.label), expected.sidebar);
            check(`${module}: the module is active in the topbar`, shell.topbar.filter(item => item.active).map(item => item.label), [expected.active.topbar]);
            check(`${module}: the feature is active in the sidebar`, shell.sidebar.filter(item => item.active).map(item => item.label), [expected.active.sidebar]);
            check(`${module}: the page`, shell.title, [expected.title]);
            check(`${module}: the toolbar buttons`, shell.toolbar, expected.toolbar);
        }

        // (3) a toolbar button opens the State Change slice's dialog; the submit reaches the route Stage registered.
        await page.locator('nav[aria-label="Modules"]').getByText('Sales', { exact: true }).click();
        await page.getByRole('button', { name: 'New order' }).waitFor();
        const hashOfSales = new URL(page.url()).hash;
        await page.getByRole('button', { name: 'New order' }).click();
        const dialog = page.locator('.scene-slice-dialog');
        await dialog.waitFor();
        const dialogShape = { title: (await dialog.locator('h2').innerText()).trim(), subtitle: (await dialog.locator('.scene-slice-dialog-subtitle').innerText()).trim(), fields: await dialog.locator('input').evaluateAll(inputs => inputs.map(input => [input.name, input.type])) };
        result.tree.toolbarDialog = dialogShape;
        check('the toolbar button opens its slice task dialog', [dialogShape.title, dialogShape.subtitle], ['New order', 'State Change · PlaceOrder']);
        check('the dialog fields are the command schema', dialogShape.fields, [['orderId', 'text'], ['customer', 'text'], ['quantity', 'number']]);
        await dialog.locator('input[name=orderId]').fill('11111111-1111-4111-8111-111111111111');
        await dialog.locator('input[name=customer]').fill('Fjordline AS');
        await dialog.locator('input[name=quantity]').fill('4');
        await dialog.locator('button[type=submit]').click();
        await page.locator('[role=dialog]').waitFor({ state: 'detached' });
        check('submitting posts the command to the route the Stage registered', posts, [{ url: '/api/sales/orders', body: { orderId: '11111111-1111-4111-8111-111111111111', customer: 'Fjordline AS', quantity: 4 } }]);

        // (4) deep links reflect navigation state.
        const link = page.url();
        const second = await browser.newPage({ viewport: { width: 1500, height: 900 } });
        await second.goto(link);
        await second.waitForSelector('.layout-topbar');
        check('a deep link restores the module sidebar', (await second.locator('nav[aria-label="Sidebar"] a, nav[aria-label="Sidebar"] button').allInnerTexts()).map(text => text.trim()), ['Orders', 'Customers']);
        check('a deep link restores the page', (await second.locator('h1').allInnerTexts()).map(text => text.trim()).includes('Orders'), true);
        check('the address records where the user is', hashOfSales, '#/OrderScreen');
        await page.locator('nav[aria-label="Sidebar"]').getByText('Customers').click();
        await page.waitForFunction(() => location.hash === '#/CustomerScreen');
        await page.goBack();
        await page.waitForFunction(() => location.hash === '#/OrderScreen');
        check('back returns to the previous feature', (await shellOf()).sidebar.filter(item => item.active).map(item => item.label), ['Orders']);
        check('no error was raised in the page', errors, []);

        // (7) the same shell in Studio's preview, when its tree is given.
        if (studioTreePath) {
            const studio = JSON.parse(readFileSync(studioTreePath, 'utf8'));
            for (const module of Object.keys(expectedShell)) {
                const differences = studio.modules?.[module] ? compareShells(result.tree.modules[module], studio.modules[module], module) : [`${module}: Studio recorded no preview`];
                check(`${module}: Studio's preview draws the shell Stage draws`, differences, []);
            }
            check('Studio\'s toolbar button opens the same dialog as Stage\'s', studio.toolbarDialog ?? null, result.tree.toolbarDialog);
            result.studioTree = studioTreePath;
        } else {
            failures.push('studio parity: no Studio tree was given (set SCREENPLAY_STUDIO_TREE from check-studio-production-shell-preview.mjs)');
        }
    } catch (error) {
        failures.push(`the scenario stopped: ${error.message}`);
    } finally {
        if (browser) await browser.close();
        const container = stageContainer();
        runtime.kill('SIGINT');
        await sleep(2_000);
        if (container) { try { execFileSync('docker', ['rm', '-f', container.id], { stdio: 'ignore' }); } catch { /* already gone */ } }
    }

    result.status = failures.length === 0 ? 'passed' : 'failed';
    result.passed = passed;
    result.failures = failures;
    mkdirSync(dirname(resultPath), { recursive: true });
    writeFileSync(resultPath, JSON.stringify(result, null, 2) + '\n');
    writeFileSync(join(dirname(resultPath), 'stage-tree.json'), JSON.stringify(result.tree, null, 2) + '\n');
    console.log(`${passed.length} assertions passed, ${failures.length} failed`);
    for (const failure of failures) console.log(`  FAIL ${failure}`);
    process.exit(failures.length === 0 ? 0 : 1);
})();
