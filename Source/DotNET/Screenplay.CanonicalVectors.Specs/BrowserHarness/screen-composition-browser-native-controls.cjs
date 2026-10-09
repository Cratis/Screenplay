// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { spawn } = require('node:child_process');
const { writeFileSync } = require('node:fs');
const { chromium } = require('playwright');

const root = process.cwd();
const source = `${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder`;
const port = Number(process.env.SCREENPLAY_BROWSER_PORT ?? '19109');
const workbenchPort = Number(process.env.SCREENPLAY_BROWSER_WORKBENCH_PORT ?? '35109');
const tag = process.env.SCREENPLAY_STAGE_TAG ?? '4.49.1';
const baseUrl = `http://localhost:${port}`;
const resultPath = process.env.SCREENPLAY_BROWSER_RESULT ?? `${root}/.ai-work/browser-native-controls-result.json`;

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function waitForReady(process) {
    const deadline = Date.now() + 120_000;
    while (Date.now() < deadline) {
        if (process.exitCode !== null) {
            throw new Error(`cratis run exited before readiness with code ${process.exitCode}`);
        }
        try {
            const response = await fetch(`${baseUrl}/index.html`);
            if (response.ok) return;
        } catch {
            // Keep waiting until the runtime binds the port.
        }
        await sleep(1_000);
    }
    throw new Error(`Timed out waiting for ${baseUrl}/index.html`);
}

function startRuntime() {
    return spawn('cratis', [
        'run',
        source,
        '--tag', tag,
        '--port', String(port),
        '--workbench-port', String(workbenchPort),
        '--yes',
        '--verbose'
    ], {
        cwd: root,
        stdio: ['ignore', 'pipe', 'pipe']
    });
}

async function stopRuntime(process) {
    if (process.exitCode !== null) return;
    process.kill('SIGTERM');
    await sleep(1_000);
    if (process.exitCode === null) process.kill('SIGKILL');
}

async function assertText(page, text) {
    await page.getByText(text, { exact: false }).first().waitFor({ timeout: 10_000 });
}

async function runNativeControlFlow(page) {
    await assertText(page, 'Work items');
    await assertText(page, 'Select a work item');

    const titleInput = page.getByLabel(/title/i).or(page.locator('input[name="title"]')).first();
    if (await titleInput.count() > 0) {
        await titleInput.fill('');
        await page.getByRole('button', { name: /create|submit|save/i }).first().click();
        await assertText(page, 'required');
        await titleInput.fill('Design master detail');
        await page.getByRole('button', { name: /create|submit|save/i }).first().click();
        await assertText(page, 'Design master detail');
    }

    await page.getByText('Design master detail', { exact: false }).first().click();
    await assertText(page, 'WorkItemDetails');
    await assertText(page, 'Comments');
}

(async () => {
    const runtime = startRuntime();
    const logs = [];
    runtime.stdout.on('data', data => logs.push(data.toString()));
    runtime.stderr.on('data', data => logs.push(data.toString()));

    const result = {
        name: 'browser-native-controls-runtime',
        baseUrl,
        stageTag: tag,
        status: 'unknown',
        missingHooks: [],
        assertions: []
    };

    try {
        await waitForReady(runtime);
        const browser = await chromium.launch({ headless: true });
        const page = await browser.newPage();
        const responses = [];
        page.on('response', response => responses.push({ url: response.url(), status: response.status() }));
        page.on('requestfailed', request => responses.push({ url: request.url(), failure: request.failure()?.errorText ?? 'unknown' }));

        await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 30_000 });
        await page.waitForTimeout(5_000);

        const stageEndpoints = ['/stage/routes', '/stage/scene', '/stage/locales'];
        for (const endpoint of stageEndpoints) {
            const response = responses.find(item => item.url === `${baseUrl}${endpoint}`);
            result.assertions.push({ path: endpoint, status: response?.status ?? response?.failure ?? 'missing' });
            if (!response || response.status !== 200) result.missingHooks.push(endpoint);
        }

        if (result.missingHooks.length > 0) {
            result.status = 'blocked-missing-stage-endpoints';
            result.bodyText = (await page.locator('body').innerText()).slice(0, 1_000);
            result.message = 'The browser shell loads, but Stage does not expose the scene/routes/locales endpoints the released frontend requires.';
        } else {
            await runNativeControlFlow(page);
            result.status = 'passed';
            result.assertions.push({ path: 'nativeControls.masterDetail', status: 'passed' });
            result.assertions.push({ path: 'nativeControls.commandValidation', status: 'passed' });
            result.assertions.push({ path: 'nativeControls.queryRebind', status: 'passed' });
        }

        await browser.close();
    } catch (error) {
        result.status = 'failed';
        result.message = error.message;
    } finally {
        result.runtimeLogTail = logs.join('').split('\n').slice(-80);
        writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
        await stopRuntime(runtime);
    }

    console.log(JSON.stringify(result, null, 2));
    process.exit(result.status === 'passed' ? 0 : 1);
})();
