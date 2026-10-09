// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { spawn } = require('node:child_process');
const { writeFileSync } = require('node:fs');
const { chromium } = require('playwright');

const root = process.cwd();
const source = `${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder`;
const port = Number(process.env.SCREENPLAY_BROWSER_PORT ?? '19109');
const workbenchPort = Number(process.env.SCREENPLAY_BROWSER_WORKBENCH_PORT ?? '35109');
const tag = process.env.SCREENPLAY_STAGE_TAG ?? '4.49.2';
const baseUrl = `http://localhost:${port}`;
const resultPath = process.env.SCREENPLAY_BROWSER_RESULT ?? `${root}/.ai-work/browser-native-controls-result.json`;

const workItemId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
const commentId = '11111111-1111-1111-1111-111111111111';

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function waitForReady(process) {
    const deadline = Date.now() + 120_000;
    while (Date.now() < deadline) {
        if (process.exitCode !== null) throw new Error(`cratis run exited before readiness with code ${process.exitCode}`);
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
    ], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
}

async function stopRuntime(process) {
    if (process.exitCode !== null) return;
    process.kill('SIGTERM');
    await sleep(1_000);
    if (process.exitCode === null) process.kill('SIGKILL');
}

async function postJson(path, body) {
    const response = await fetch(`${baseUrl}${path}`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!response.ok) throw new Error(`${path} returned ${response.status}`);
    return response.json();
}

function record(result, path, status, value = undefined) {
    result.assertions.push({ path, status, value });
}

async function expectText(page, result, path, text) {
    await page.getByText(text, { exact: false }).first().waitFor({ timeout: 10_000 });
    record(result, path, 'passed', text);
}

async function recordNativeButton(page, result, label, path) {
    const button = page.getByRole('button', { name: new RegExp(label, 'i') }).first();
    if (await button.count() === 0) {
        record(result, path, 'missing');
        result.remainingBlockers.push(`${label}: missing`);
        return;
    }
    const disabled = await button.isDisabled();
    const title = await button.getAttribute('title');
    record(result, path, disabled ? 'blocked-disabled' : 'passed', title ?? '');
    if (disabled) result.remainingBlockers.push(`${label}: ${title ?? 'disabled'}`);
}

async function endpointStatus(responses, endpoint) {
    const deadline = Date.now() + 15_000;
    while (Date.now() < deadline) {
        const response = responses.find(candidate => candidate.url === `${baseUrl}${endpoint}`);
        if (response) return response.status ?? response.failure ?? 'missing';
        await sleep(100);
    }
    return 'missing';
}

async function runBrowserFlow(result) {
    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage();
    const responses = [];
    page.on('response', response => responses.push({ url: response.url(), status: response.status() }));
    page.on('requestfailed', request => responses.push({ url: request.url(), failure: request.failure()?.errorText ?? 'unknown' }));

    await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    const endpoints = ['/stage/routes', '/stage/scene', '/stage/locales'];
    for (const endpoint of endpoints) {
        const status = await endpointStatus(responses, endpoint);
        record(result, `browser.endpoint.${endpoint}`, status === 200 ? 'passed' : 'failed', String(status));
        if (status !== 200) result.missingHooks.push(endpoint);
    }

    if (result.missingHooks.length > 0) {
        result.status = 'blocked-missing-stage-endpoints';
        result.bodyText = (await page.locator('body').innerText()).slice(0, 1_000);
        result.message = 'The browser shell loads, but Stage does not expose the scene/routes/locales endpoints the released frontend requires.';
        await browser.close();
        return;
    }

    await postJson('/api/workspaces/tracking/create-work-item/create-work-item', { workItemId, title: 'Design master detail' });
    record(result, 'browser.seed.CreateWorkItem', 'passed', workItemId);
    await postJson('/api/workspaces/tracking/add-comment/add-comment', { commentId, workItemId, text: 'Needs compact layout' });
    record(result, 'browser.seed.AddComment', 'passed', commentId);

    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(3_000);
    await expectText(page, result, 'browser.navigation.WorkItemList', 'WorkItemList');
    await expectText(page, result, 'browser.query.AllWorkItems.title', 'Design master detail');
    await expectText(page, result, 'browser.query.AllWorkItems.status', 'open');
    await recordNativeButton(page, result, 'CreateWorkItem', 'browser.native.CreateWorkItem');
    await page.getByText('Design master detail', { exact: false }).first().click();
    await expectText(page, result, 'browser.masterDetail.selection', 'Clear selection');
    await page.getByText('Clear selection', { exact: true }).click();
    await page.waitForTimeout(500);
    if (await page.getByText('Clear selection', { exact: true }).count() === 0) record(result, 'browser.queryRebind.clearSelection', 'passed');

    await page.goto(`${baseUrl}/#/WorkItemDetails`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(3_000);
    await expectText(page, result, 'browser.navigation.WorkItemDetails', 'WorkItemDetails');
    await expectText(page, result, 'browser.query.WorkItemDetails.title', 'Design master detail');
    await expectText(page, result, 'browser.query.CommentsForWorkItem.text', 'Needs compact layout');

    await expectText(page, result, 'browser.navigation.themeLight', 'Scene Default Light');
    await expectText(page, result, 'browser.navigation.themeDark', 'Scene Default Dark');
    await expectText(page, result, 'browser.navigation.menu.CommentThread', 'CommentThread');

    await recordNativeButton(page, result, 'Rename', 'browser.native.RenameWorkItem');
    await recordNativeButton(page, result, 'AddComment', 'browser.native.AddComment');

    result.network = responses.filter(response => response.url.includes('/api/') || response.url.includes('/stage/'));
    await browser.close();

    result.status = result.remainingBlockers.length > 0 ? 'partial-native-actions-disabled' : 'passed';
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
        remainingBlockers: [],
        assertions: []
    };

    try {
        await waitForReady(runtime);
        await runBrowserFlow(result);
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
