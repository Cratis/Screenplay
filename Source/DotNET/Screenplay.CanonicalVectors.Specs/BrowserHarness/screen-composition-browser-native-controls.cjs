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
const nextTag = process.env.SCREENPLAY_NEXT_STAGE_TAG ?? '4.49.4';
const baseUrl = `http://localhost:${port}`;
const resultPath = process.env.SCREENPLAY_BROWSER_RESULT ?? `${root}/.ai-work/browser-native-controls-result.json`;

const workItemA = {
    id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    title: 'Design master detail',
    commentId: '11111111-1111-1111-1111-111111111111',
    comment: 'Needs compact layout'
};
const workItemB = {
    id: '22222222-2222-2222-2222-222222222222',
    title: 'Implement native browser forms',
    commentId: '22222222-2222-2222-2222-222222222223',
    comment: 'Validate selected work item only'
};
const createdWorkItem = {
    id: '33333333-3333-3333-3333-333333333333',
    title: 'Browser-created work item'
};
const nativeComment = {
    id: '44444444-4444-4444-4444-444444444444',
    text: 'Added through native browser form'
};

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

function block(result, path, reason) {
    record(result, path, 'blocked', reason);
    result.remainingBlockers.push(`${path}: ${reason}`);
}

async function expectText(page, result, path, text) {
    try {
        await page.getByText(text, { exact: false }).first().waitFor({ timeout: 10_000 });
        record(result, path, 'passed', text);
        return true;
    } catch {
        block(result, path, `missing text: ${text}`);
        return false;
    }
}

async function expectNoText(page, result, path, text) {
    try {
        await page.getByText(text, { exact: false }).first().waitFor({ state: 'detached', timeout: 2_000 });
        record(result, path, 'passed', `absent: ${text}`);
        return true;
    } catch {
        block(result, path, `unexpected text remained: ${text}`);
        return false;
    }
}

async function describeControls(page) {
    return page.evaluate(() => Array.from(document.querySelectorAll('input, textarea, select, button')).map((element, index) => ({
        index,
        tag: element.tagName.toLowerCase(),
        type: element.getAttribute('type'),
        name: element.getAttribute('name'),
        ariaLabel: element.getAttribute('aria-label'),
        placeholder: element.getAttribute('placeholder'),
        text: element.textContent?.trim(),
        value: element.value,
        disabled: element.disabled
    })));
}

async function closeDialog(page, result, path) {
    const dialogs = page.getByRole('dialog');
    if (await dialogs.count() === 0) return true;

    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);

    if (await dialogs.count() === 0) {
        record(result, path, 'passed');
        return true;
    }

    const closeInsideDialog = dialogs.first().getByRole('button', { name: /cancel|close|dismiss/i }).first();
    if (await closeInsideDialog.count() > 0) {
        try {
            await closeInsideDialog.click({ timeout: 2_000 });
            await page.waitForTimeout(500);
        } catch (error) {
            record(result, `${path}.closeButton`, 'blocked', error.message);
        }
    }

    if (await dialogs.count() === 0) {
        record(result, path, 'passed');
        return true;
    }

    block(result, path, 'dialog remained open');
    return false;
}

async function fillField(page, result, labelOrName, value) {
    const byLabel = page.getByLabel(new RegExp(labelOrName, 'i')).first();
    if (await byLabel.count() > 0) {
        await byLabel.fill(value);
        record(result, `browser.native.field.${labelOrName}`, 'passed', 'label');
        return true;
    }

    const byName = page.locator(`input[name="${labelOrName}"], textarea[name="${labelOrName}"]`).first();
    if (await byName.count() > 0) {
        await byName.fill(value);
        record(result, `browser.native.field.${labelOrName}`, 'passed', 'name');
        return true;
    }

    const byPlaceholder = page.getByPlaceholder(new RegExp(labelOrName, 'i')).first();
    if (await byPlaceholder.count() > 0) {
        await byPlaceholder.fill(value);
        record(result, `browser.native.field.${labelOrName}`, 'passed', 'placeholder');
        return true;
    }

    record(result, `browser.native.controls.${labelOrName}`, 'info', JSON.stringify(await describeControls(page)));
    block(result, `browser.native.field.${labelOrName}`, 'field missing');
    return false;
}

async function clickSubmit(page, result, path) {
    const submit = page.getByRole('button', { name: /create|rename|add|submit|save/i }).filter({ hasNotText: /close|cancel/i }).last();
    if (await submit.count() === 0) {
        block(result, path, 'submit control missing');
        return false;
    }

    await submit.click();
    return true;
}

async function recordNativeButton(page, result, label, path) {
    const button = page.getByRole('button', { name: new RegExp(label, 'i') }).first();
    if (await button.count() === 0) {
        block(result, path, 'control missing');
        return { state: 'missing' };
    }

    const disabled = await button.isDisabled();
    const title = await button.getAttribute('title');
    record(result, path, disabled ? 'blocked-disabled' : 'passed', title ?? 'enabled');
    if (disabled) {
        result.remainingBlockers.push(`${label}: ${title ?? 'disabled'}`);
        return { state: 'disabled', title };
    }

    return { state: 'enabled', button };
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

function commandRequests(network, commandName) {
    return network.requests.filter(request => request.method === 'POST' && request.url.toLowerCase().includes(commandName.toLowerCase()));
}

function requestContainsWorkItem(request, workItem) {
    const body = typeof request.postData === 'string' ? request.postData : '';
    return request.url.includes(workItem.id) || body.includes(workItem.id) || body.includes(workItem.title);
}

async function sectionText(page, heading) {
    const body = await page.locator('body').innerText();
    const start = body.indexOf(heading);
    if (start < 0) return '';
    const tail = body.slice(start);
    const next = tail.slice(heading.length).search(/\n[A-Z][A-Za-z ]+\n/);
    return next < 0 ? tail : tail.slice(0, heading.length + next);
}

async function seedData(result) {
    for (const item of [workItemA, workItemB]) {
        await postJson('/api/workspaces/tracking/create-work-item/create-work-item', { workItemId: item.id, title: item.title });
        record(result, `browser.seed.CreateWorkItem.${item.id}`, 'passed', item.title);
        await postJson('/api/workspaces/tracking/add-comment/add-comment', { commentId: item.commentId, workItemId: item.id, text: item.comment });
        record(result, `browser.seed.AddComment.${item.commentId}`, 'passed', item.comment);
    }
}

function collectSceneIds(value, ids = []) {
    if (Array.isArray(value)) {
        for (const item of value) collectSceneIds(item, ids);
        return ids;
    }

    if (value && typeof value === 'object') {
        if (typeof value.id === 'string') ids.push(value.id);
        if (typeof value.stableId === 'string') ids.push(value.stableId);
        for (const child of Object.values(value)) collectSceneIds(child, ids);
    }

    return ids;
}

async function assertSceneIdentifierFidelity(result) {
    const response = await fetch(`${baseUrl}/stage/scene`);
    if (!response.ok) {
        block(result, 'browser.scene.identifierFidelity.fetch', `/stage/scene returned ${response.status}`);
        return;
    }

    const scene = await response.json();
    const ids = [...new Set(collectSceneIds(scene))];
    const punctuationIds = ids.filter(id => id.includes('.') || id.includes(':'));
    const dottedIds = ids.filter(id => id.includes('.'));
    record(result, 'browser.scene.identifierFidelity.ids', 'info', punctuationIds.join(','));

    if (punctuationIds.length > 0) record(result, 'browser.scene.identifierFidelity.punctuation', 'passed', punctuationIds.join(','));
    else block(result, 'browser.scene.identifierFidelity.punctuation', 'no punctuation-bearing stable ids found in /stage/scene');

    if (dottedIds.length > 0) record(result, 'browser.scene.identifierFidelity.dotted', 'passed', dottedIds.join(','));
    else record(result, 'browser.scene.identifierFidelity.dotted', 'pending', 'rerun after Stage/CLI vector includes dotted stable ids');
}

async function assertTwoItemSelectionLifecycle(page, result, network) {
    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(3_000);
    await expectText(page, result, 'browser.navigation.WorkItemList', 'WorkItemList');
    await expectText(page, result, 'browser.query.AllWorkItems.titleA', workItemA.title);
    await expectText(page, result, 'browser.query.AllWorkItems.titleB', workItemB.title);

    const beforeBRequests = network.requests.length;
    await page.getByText(workItemB.title, { exact: false }).first().click();
    await expectText(page, result, 'browser.masterDetail.selection', 'Clear selection');
    await expectText(page, result, 'browser.query.WorkItemDetails.titleB', workItemB.title);
    await expectText(page, result, 'browser.query.CommentsForWorkItem.commentB', workItemB.comment);
    await expectNoText(page, result, 'browser.query.CommentsForWorkItem.commentAAbsent', workItemA.comment);

    const bScopedRequests = network.requests.slice(beforeBRequests).filter(request => request.url.includes('/api/') && requestContainsWorkItem(request, workItemB));
    if (bScopedRequests.length > 0) record(result, 'browser.queryArgs.selectedB', 'passed', bScopedRequests.map(request => request.url).join(','));
    else block(result, 'browser.queryArgs.selectedB', 'no API request carried selected row B identity');

    const directLink = page.url();
    if (directLink.includes(workItemB.id)) record(result, 'browser.deepLink.selectedB', 'passed', directLink);
    else block(result, 'browser.deepLink.selectedB', `selected URL does not carry B identity: ${directLink}`);

    await assertStaleSelectionDiscard(page, result, network);

    await page.getByText('Clear selection', { exact: true }).click();
    await page.waitForTimeout(1_000);
    if (await page.getByText('Clear selection', { exact: true }).count() === 0) record(result, 'browser.queryRebind.clearSelection.button', 'passed');
    else block(result, 'browser.queryRebind.clearSelection.button', 'clear selection button remained');

    await expectNoText(page, result, 'browser.queryRebind.clearSelection.commentBAbsent', workItemB.comment);
    const clearUrl = page.url();
    if (!clearUrl.includes(workItemA.id) && !clearUrl.includes(workItemB.id)) record(result, 'browser.queryRebind.clearSelection.route', 'passed', clearUrl);
    else block(result, 'browser.queryRebind.clearSelection.route', `route still carries selected identity: ${clearUrl}`);
}

async function assertStaleSelectionDiscard(page, result, network) {
    let delayedA = 0;
    await page.route('**/api/**', async route => {
        const request = route.request();
        const postData = request.postData() ?? '';
        if (request.url().includes(workItemA.id) || postData.includes(workItemA.id)) {
            delayedA++;
            await sleep(1_500);
        }
        await route.continue();
    });

    await page.getByText(workItemA.title, { exact: false }).first().click();
    await sleep(50);
    await page.getByText(workItemB.title, { exact: false }).first().click();
    await page.waitForTimeout(2_500);
    await page.unroute('**/api/**');

    if (delayedA > 0) record(result, 'browser.queryRebind.staleDelayedA.request', 'passed', String(delayedA));
    else block(result, 'browser.queryRebind.staleDelayedA.request', 'no delayed A query was observed');

    const body = await page.locator('body').innerText();
    if (body.includes(workItemB.title) && body.includes(workItemB.comment) && !body.includes(workItemA.comment)) record(result, 'browser.queryRebind.staleDelayedA.discard', 'passed');
    else block(result, 'browser.queryRebind.staleDelayedA.discard', 'delayed A response was not proven discarded');

    const staleRequests = network.requests.filter(request => requestContainsWorkItem(request, workItemA) || requestContainsWorkItem(request, workItemB));
    record(result, 'browser.queryRebind.staleDelayedA.observedRequests', 'info', staleRequests.map(request => `${request.method} ${request.url}`).join('\n'));
}

async function assertInvalidThenValidCreate(page, result, network) {
    const button = await recordNativeButton(page, result, 'CreateWorkItem', 'browser.native.CreateWorkItem.control');
    if (button.state !== 'enabled') {
        record(result, 'browser.native.CreateWorkItem.invalidSubmit.zeroRequests', 'pending', 'button disabled');
        record(result, 'browser.native.CreateWorkItem.validSubmit.payload', 'pending', 'button disabled');
        record(result, 'browser.native.CreateWorkItem.validSubmit.projectionReopen', 'pending', 'button disabled');
        return;
    }

    await button.button.click();
    await page.waitForTimeout(500);
    const beforeInvalid = commandRequests(network, 'create-work-item').length;
    await clickSubmit(page, result, 'browser.native.CreateWorkItem.invalidSubmit.submitControl');
    await page.waitForTimeout(1_000);
    const afterInvalid = commandRequests(network, 'create-work-item').length;
    if (afterInvalid === beforeInvalid) record(result, 'browser.native.CreateWorkItem.invalidSubmit.zeroRequests', 'passed');
    else block(result, 'browser.native.CreateWorkItem.invalidSubmit.zeroRequests', `expected zero command requests, saw ${afterInvalid - beforeInvalid}`);
    await expectText(page, result, 'browser.native.CreateWorkItem.invalidSubmit.validation', 'required');

    const fieldsFilled = [
        await fillField(page, result, 'workItemId', createdWorkItem.id),
        await fillField(page, result, 'title', createdWorkItem.title)
    ].every(Boolean);
    if (!fieldsFilled) {
        await closeDialog(page, result, 'browser.native.CreateWorkItem.dialog.closeAfterMissingFields');
        return;
    }

    const beforeValid = commandRequests(network, 'create-work-item').length;
    await clickSubmit(page, result, 'browser.native.CreateWorkItem.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'create-work-item').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], createdWorkItem)) record(result, 'browser.native.CreateWorkItem.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
    else block(result, 'browser.native.CreateWorkItem.validSubmit.payload', `expected one canonical create payload, saw ${validRequests.length}`);

    await page.goto(`${baseUrl}/#/WorkItemList`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    await expectText(page, result, 'browser.native.CreateWorkItem.validSubmit.projectionReopen', createdWorkItem.title);
}

async function assertRenameDialog(page, result, network) {
    await page.getByText(workItemB.title, { exact: false }).first().click();
    const button = await recordNativeButton(page, result, 'Rename', 'browser.native.RenameWorkItem.control');
    if (button.state !== 'enabled') {
        record(result, 'browser.native.RenameWorkItem.validSubmit.payload', 'pending', 'button disabled');
        record(result, 'browser.dialog.deepLink.lifecycle', 'pending', 'native command actions disabled');
        return;
    }

    const selectedUrl = page.url();
    await button.button.click();
    await page.waitForTimeout(500);
    if (page.url() !== selectedUrl || await page.getByRole('dialog').count() > 0) record(result, 'browser.dialog.open', 'passed', page.url());
    else block(result, 'browser.dialog.open', 'rename did not open a dialog or deep-link destination');

    const renamedTitle = 'Renamed selected B in browser';
    const fieldsFilled = await fillField(page, result, 'title', renamedTitle);
    if (!fieldsFilled) {
        await closeDialog(page, result, 'browser.dialog.closeAfterMissingRenameFields');
        return;
    }

    const beforeValid = commandRequests(network, 'rename-work-item').length;
    await clickSubmit(page, result, 'browser.native.RenameWorkItem.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'rename-work-item').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], { id: workItemB.id, title: renamedTitle })) record(result, 'browser.native.RenameWorkItem.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
    else block(result, 'browser.native.RenameWorkItem.validSubmit.payload', `expected one rename payload for selected B, saw ${validRequests.length}`);

    await expectText(page, result, 'browser.native.RenameWorkItem.validSubmit.projection', renamedTitle);
    await closeDialog(page, result, 'browser.dialog.close');
}

async function assertAddComment(page, result, network) {
    await page.getByText(workItemB.title, { exact: false }).first().click();
    const button = await recordNativeButton(page, result, 'AddComment', 'browser.native.AddComment.control');
    if (button.state !== 'enabled') {
        record(result, 'browser.native.AddComment.validSubmit.payload', 'pending', 'button disabled');
        return;
    }

    await button.button.click();
    await page.waitForTimeout(500);
    const fieldsFilled = [
        await fillField(page, result, 'commentId', nativeComment.id),
        await fillField(page, result, 'text', nativeComment.text)
    ].every(Boolean);
    if (!fieldsFilled) {
        await closeDialog(page, result, 'browser.native.AddComment.dialog.closeAfterMissingFields');
        return;
    }

    const beforeValid = commandRequests(network, 'add-comment').length;
    await clickSubmit(page, result, 'browser.native.AddComment.validSubmit.submitControl');
    await page.waitForTimeout(3_000);
    const validRequests = commandRequests(network, 'add-comment').slice(beforeValid);
    if (validRequests.length === 1 && requestContainsWorkItem(validRequests[0], { id: workItemB.id, title: nativeComment.text })) record(result, 'browser.native.AddComment.validSubmit.payload', 'passed', validRequests[0].postData ?? validRequests[0].url);
    else block(result, 'browser.native.AddComment.validSubmit.payload', `expected one add-comment payload for selected B, saw ${validRequests.length}`);

    await expectText(page, result, 'browser.native.AddComment.validSubmit.projection', nativeComment.text);
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    await expectText(page, result, 'browser.native.AddComment.validSubmit.reopen', nativeComment.text);
}

async function assertNavigationAndAssets(page, result) {
    await expectText(page, result, 'browser.navigation.themeLight.label', 'Scene Default Light');
    await expectText(page, result, 'browser.navigation.themeDark.label', 'Scene Default Dark');
    await expectText(page, result, 'browser.navigation.menu.CommentThread', 'CommentThread');

    const renderState = await page.evaluate(() => ({
        styleSheetCount: document.styleSheets.length,
        svgCount: document.querySelectorAll('svg').length,
        imageCount: document.querySelectorAll('img').length,
        buttonCount: document.querySelectorAll('button').length,
        bodyBackground: getComputedStyle(document.body).backgroundColor,
        bodyColor: getComputedStyle(document.body).color,
        assetLinks: Array.from(document.querySelectorAll('link[href], script[src]')).map(element => element.getAttribute('href') ?? element.getAttribute('src')).filter(Boolean)
    }));
    if (renderState.styleSheetCount > 0 && renderState.buttonCount > 0) record(result, 'browser.package.renderHost', 'passed', JSON.stringify(renderState));
    else block(result, 'browser.package.renderHost', JSON.stringify(renderState));

    const dark = page.getByText('Scene Default Dark', { exact: false }).first();
    if (await dark.count() > 0) {
        const before = renderState.bodyBackground;
        await dark.click();
        await page.waitForTimeout(500);
        const after = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
        if (after !== before) record(result, 'browser.theme.dark.appliedStyle', 'passed', `${before} -> ${after}`);
        else block(result, 'browser.theme.dark.appliedStyle', `body background did not change from ${before}`);
    }
}

async function runBrowserFlow(result) {
    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage();
    const responses = [];
    const network = { requests: [], responses };
    page.on('request', request => network.requests.push({ url: request.url(), method: request.method(), postData: request.postData() }));
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

    await assertSceneIdentifierFidelity(result);
    await seedData(result);
    await assertTwoItemSelectionLifecycle(page, result, network);
    await assertInvalidThenValidCreate(page, result, network);
    await page.goto(`${baseUrl}/#/WorkItemDetails`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForTimeout(2_000);
    await assertRenameDialog(page, result, network);
    await assertAddComment(page, result, network);
    await assertNavigationAndAssets(page, result);

    result.network = responses.filter(response => response.url.includes('/api/') || response.url.includes('/stage/'));
    result.commandRequests = network.requests.filter(request => request.method === 'POST' && request.url.includes('/api/'));
    result.status = result.remainingBlockers.length > 0 ? 'partial-acceptance-blocked' : 'passed';
    await browser.close();
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
        nextStageTag: nextTag,
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
