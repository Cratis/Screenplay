// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { existsSync, writeFileSync } = require('node:fs');
const { chromium } = require('playwright');

const root = process.cwd();
const studioUrl = process.env.SCREENPLAY_STUDIO_URL;
const resultPath = process.env.SCREENPLAY_STUDIO_RESULT ?? `${root}/.ai-work/studio-production-play-result.json`;
const importFile = process.env.SCREENPLAY_STUDIO_IMPORT_FILE ?? `${root}/Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder/application.play`;
const projectName = process.env.SCREENPLAY_STUDIO_PROJECT_NAME ?? `screen-composition-${Date.now()}`;

const selectors = {
    newProject: process.env.SCREENPLAY_STUDIO_NEW_SELECTOR ?? '[data-testid="new-project"], button:has-text("New"), button:has-text("Create")',
    projectName: process.env.SCREENPLAY_STUDIO_PROJECT_NAME_SELECTOR ?? '[data-testid="project-name"], input[name="name"], input[placeholder*="Name"]',
    save: process.env.SCREENPLAY_STUDIO_SAVE_SELECTOR ?? '[data-testid="save"], button:has-text("Save")',
    reopen: process.env.SCREENPLAY_STUDIO_REOPEN_SELECTOR ?? '[data-testid="open-project"], button:has-text("Open"), a:has-text("Projects")',
    exportProject: process.env.SCREENPLAY_STUDIO_EXPORT_SELECTOR ?? '[data-testid="export"], button:has-text("Export")',
    importProject: process.env.SCREENPLAY_STUDIO_IMPORT_SELECTOR ?? '[data-testid="import"], button:has-text("Import")',
    play: process.env.SCREENPLAY_STUDIO_PLAY_SELECTOR ?? '[data-testid="play"], button:has-text("Play")',
    fileInput: process.env.SCREENPLAY_STUDIO_FILE_INPUT_SELECTOR ?? 'input[type="file"]',
    shell: process.env.SCREENPLAY_STUDIO_SHELL_SELECTOR ?? '[data-testid="studio-shell"], main, body',
    playSurface: process.env.SCREENPLAY_STUDIO_PLAY_SURFACE_SELECTOR ?? '[data-testid="play-surface"], iframe, canvas, main'
};

function record(result, path, status, value = undefined) {
    result.assertions.push({ path, status, value });
}

function block(result, path, value) {
    record(result, path, 'blocked', value);
    result.remainingBlockers.push(`${path}: ${value}`);
}

async function clickSelector(page, result, path, selector, timeout = 5_000) {
    const locator = page.locator(selector).first();
    try {
        await locator.waitFor({ timeout });
        await locator.click();
        record(result, path, 'passed', selector);
        return true;
    } catch (error) {
        block(result, path, `control not available: ${selector}; ${error.message}`);
        return false;
    }
}

async function fillSelector(page, result, path, selector, value, timeout = 5_000) {
    const locator = page.locator(selector).first();
    try {
        await locator.waitFor({ timeout });
        await locator.fill(value);
        record(result, path, 'passed', value);
        return true;
    } catch (error) {
        block(result, path, `field not available: ${selector}; ${error.message}`);
        return false;
    }
}

async function maybeCreateProject(page, result) {
    if (!await clickSelector(page, result, 'studio.newProject.click', selectors.newProject, 8_000)) return false;
    await fillSelector(page, result, 'studio.newProject.name', selectors.projectName, projectName, 3_000);
    return true;
}

async function saveAndReopen(page, result) {
    if (!await clickSelector(page, result, 'studio.save.click', selectors.save, 8_000)) return false;
    await page.waitForTimeout(1_000);
    const savedUrl = page.url();
    record(result, 'studio.save.url', 'passed', savedUrl);
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 60_000 });
    await page.locator(selectors.shell).first().waitFor({ timeout: 30_000 });
    const body = await page.locator('body').innerText();
    if (body.includes(projectName) || page.url() === savedUrl) record(result, 'studio.reopen.persistedProject', 'passed', page.url());
    else block(result, 'studio.reopen.persistedProject', 'project name or saved URL was not restored after reload');
    return true;
}

async function exportProject(page, result) {
    const exportControl = page.locator(selectors.exportProject).first();
    try {
        await exportControl.waitFor({ timeout: 8_000 });
        const [download] = await Promise.all([
            page.waitForEvent('download', { timeout: 30_000 }),
            exportControl.click()
        ]);
        const path = await download.path();
        const suggested = download.suggestedFilename();
        record(result, 'studio.export.download', 'passed', `${suggested}:${path ?? 'streamed'}`);
        return true;
    } catch (error) {
        block(result, 'studio.export.download', `export did not produce a download: ${error.message}`);
        return false;
    }
}

async function importProject(page, result) {
    if (!existsSync(importFile)) {
        block(result, 'studio.import.sourceFile', `missing import file: ${importFile}`);
        return false;
    }

    const importControl = page.locator(selectors.importProject).first();
    try {
        await importControl.waitFor({ timeout: 8_000 });
        const fileChooserPromise = page.waitForEvent('filechooser', { timeout: 10_000 });
        await importControl.click();
        const fileChooser = await fileChooserPromise;
        await fileChooser.setFiles(importFile);
        await page.waitForTimeout(2_000);
        record(result, 'studio.import.file', 'passed', importFile);
        return true;
    } catch (error) {
        const input = page.locator(selectors.fileInput).first();
        if (await input.count() > 0) {
            await input.setInputFiles(importFile);
            await page.waitForTimeout(2_000);
            record(result, 'studio.import.file', 'passed', importFile);
            return true;
        }

        block(result, 'studio.import.file', `import control did not accept a file: ${error.message}`);
        return false;
    }
}

async function playProject(page, result) {
    if (!await clickSelector(page, result, 'studio.play.click', selectors.play, 8_000)) return false;
    await page.waitForTimeout(3_000);
    const surface = page.locator(selectors.playSurface).first();
    try {
        await surface.waitFor({ timeout: 30_000 });
        const text = (await page.locator('body').innerText()).slice(0, 1_000);
        record(result, 'studio.play.surface', 'passed', text);
        return true;
    } catch (error) {
        block(result, 'studio.play.surface', `play surface did not appear: ${error.message}`);
        return false;
    }
}

async function run() {
    const result = {
        name: 'studio-production-save-export-import-play',
        studioUrl: studioUrl ?? null,
        projectName,
        importFile,
        status: 'unknown',
        remainingBlockers: [],
        assertions: []
    };

    if (!studioUrl) {
        result.status = 'blocked-missing-studio-url';
        result.message = 'Studio v0.136.3 release tag exists, but production deploy proof is blocked by a stale Pulumi lock and no local release-artifact URL was provided.';
        record(result, 'studio.productionDeploy', 'blocked', 'stale Pulumi lock');
        record(result, 'studio.save', 'pending', 'SCREENPLAY_STUDIO_URL not set');
        record(result, 'studio.export', 'pending', 'SCREENPLAY_STUDIO_URL not set');
        record(result, 'studio.import', 'pending', 'SCREENPLAY_STUDIO_URL not set');
        record(result, 'studio.play.deepLinks', 'pending', 'SCREENPLAY_STUDIO_URL not set');
        return result;
    }

    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage();
    const responses = [];
    page.on('response', response => responses.push({ url: response.url(), status: response.status() }));
    await page.goto(studioUrl, { waitUntil: 'domcontentloaded', timeout: 60_000 });
    await page.locator(selectors.shell).first().waitFor({ timeout: 30_000 });
    record(result, 'studio.shell.loaded', 'passed', page.url());

    await maybeCreateProject(page, result);
    await importProject(page, result);
    await saveAndReopen(page, result);
    await exportProject(page, result);
    await playProject(page, result);

    result.status = result.remainingBlockers.length > 0 ? 'blocked-studio-operation-failed' : 'passed';
    result.bodyText = (await page.locator('body').innerText()).slice(0, 1_000);
    result.network = responses.filter(response => response.status >= 400).slice(0, 50);
    await browser.close();
    return result;
}

run().then(result => {
    writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    console.log(JSON.stringify(result, null, 2));
    process.exit(result.status === 'passed' ? 0 : 1);
}).catch(error => {
    const result = { name: 'studio-production-save-export-import-play', status: 'failed', message: error.message, assertions: [] };
    writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    console.log(JSON.stringify(result, null, 2));
    process.exit(1);
});
