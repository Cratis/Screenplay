// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { writeFileSync } = require('node:fs');
const { chromium } = require('playwright');

const studioUrl = process.env.SCREENPLAY_STUDIO_URL;
const resultPath = process.env.SCREENPLAY_STUDIO_RESULT ?? `${process.cwd()}/.ai-work/studio-production-play-result.json`;

function record(result, path, status, value = undefined) {
    result.assertions.push({ path, status, value });
}

async function run() {
    const result = {
        name: 'studio-production-save-export-import-play',
        studioUrl: studioUrl ?? null,
        status: 'unknown',
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
    await page.waitForTimeout(3_000);

    record(result, 'studio.shell.loaded', 'passed', page.url());
    record(result, 'studio.play.deepLinks', 'pending', 'production selectors and route contract must be supplied by Studio owner');
    record(result, 'studio.saveExportImport', 'pending', 'production workspace import/export route contract must be supplied by Studio owner');
    result.status = 'blocked-missing-studio-automation-contract';
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
