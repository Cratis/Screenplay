// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } = require('node:fs');
const { dirname, join, resolve } = require('node:path');
const { tmpdir } = require('node:os');

const root = process.cwd();
const resultPath = process.env.SCREENPLAY_MCP_RESULT ?? `${root}/.ai-work/mcp-revision-transcript-result.json`;
const endpoint = process.env.SCREENPLAY_MCP_ENDPOINT;
const workspace = process.env.SCREENPLAY_MCP_WORKSPACE ?? 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder';
const scratchRoot = process.env.SCREENPLAY_MCP_SCRATCH ?? join(tmpdir(), `screenplay-mcp-${process.pid}`);
const applicationRelativePath = 'application.play';

const result = {
    name: 'mcp-folder-revision-edit-transcript',
    workspace,
    scratchRoot,
    endpoint: endpoint ?? null,
    status: 'unknown',
    remainingBlockers: [],
    assertions: []
};

function record(path, status, value = undefined) {
    result.assertions.push({ path, status, value });
}

function block(path, value) {
    record(path, 'blocked', value);
    result.remainingBlockers.push(`${path}: ${value}`);
}

function parseToolResult(response) {
    if (!response) return response;
    if (response.error) throw new Error(response.error.message ?? JSON.stringify(response.error));
    const value = response.result;
    if (value?.content?.[0]?.text) {
        const text = value.content[0].text;
        try { return JSON.parse(text); } catch { return text; }
    }
    return value;
}

async function rpc(method, params = undefined) {
    const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
            'content-type': 'application/json',
            accept: 'application/json, text/event-stream'
        },
        body: JSON.stringify({ jsonrpc: '2.0', id: `${Date.now()}-${Math.random()}`, method, params })
    });
    if (!response.ok) throw new Error(`${method} returned HTTP ${response.status}: ${await response.text()}`);
    const text = await response.text();
    const jsonLine = text.split('\n').map(line => line.trim()).find(line => line.startsWith('{'));
    return JSON.parse(jsonLine ?? text);
}

async function callTool(name, args) {
    return parseToolResult(await rpc('tools/call', { name, arguments: args }));
}

function toolNamed(tools, suffix) {
    const found = tools.find(tool => tool.name === suffix || tool.name.endsWith(suffix));
    if (!found) throw new Error(`Tool ending with '${suffix}' was not advertised. Tools: ${tools.map(tool => tool.name).join(', ')}`);
    return found.name;
}

function prepareScratch() {
    const source = resolve(root, workspace);
    if (!existsSync(source)) throw new Error(`Workspace source does not exist: ${source}`);
    rmSync(scratchRoot, { recursive: true, force: true });
    mkdirSync(dirname(scratchRoot), { recursive: true });
    cpSync(source, scratchRoot, { recursive: true });
    const application = join(scratchRoot, applicationRelativePath);
    const original = readFileSync(application, 'utf8');
    const marker = '// MCP folder revision preservation marker';
    if (!original.startsWith(marker)) writeFileSync(application, `${marker}\n${original}`);
    return { source, application, marker };
}

function findDocumentId(documents, path) {
    const match = documents.find(document => document.path === path || document.path?.endsWith(`/${path}`));
    return match?.documentId ?? match?.id ?? match?.stableKey;
}

async function runProtocol() {
    const scratch = prepareScratch();
    record('mcp.scratch.created', 'passed', scratchRoot);

    const initialized = parseToolResult(await rpc('initialize', {
        protocolVersion: '2024-11-05',
        capabilities: {},
        clientInfo: { name: 'screen-composition-mcp-revision-transcript', version: '1.0.0' }
    }));
    record('mcp.initialize', 'passed', JSON.stringify(initialized).slice(0, 500));
    await rpc('notifications/initialized').catch(() => undefined);

    const listed = parseToolResult(await rpc('tools/list'));
    const tools = listed.tools ?? listed ?? [];
    record('mcp.tools.count', 'passed', String(tools.length));

    const openWorkspace = toolNamed(tools, 'screenplay_open_workspace');
    const readWorkspace = toolNamed(tools, 'screenplay_read_workspace');
    const proposeSource = toolNamed(tools, 'screenplay_propose_source');
    const readProposal = toolNamed(tools, 'screenplay_read_proposal');
    const apply = toolNamed(tools, 'screenplay_apply');

    const opened = await callTool(openWorkspace, { path: scratchRoot, includeContent: false });
    const expectedRevision = opened.sourceRevision ?? opened.revision ?? opened.expectedRevision;
    const expectedCatalogRevision = opened.catalogRevision ?? opened.expectedCatalogRevision;
    if (!expectedRevision || !expectedCatalogRevision) throw new Error(`Open workspace did not return source/catalog revisions: ${JSON.stringify(opened)}`);
    record('mcp.openWorkspace', 'passed', `${expectedRevision}/${expectedCatalogRevision}`);

    const documents = await callTool(readWorkspace, { expectedRevision, expectedCatalogRevision, view: 'documents', limit: 200 });
    const documentItems = documents.documents ?? documents.items ?? documents.results ?? [];
    const documentId = findDocumentId(documentItems, applicationRelativePath);
    if (!documentId) throw new Error(`Could not find ${applicationRelativePath} in document list: ${JSON.stringify(documentItems).slice(0, 1_000)}`);
    record('mcp.documents.application', 'passed', documentId);

    const currentSource = readFileSync(scratch.application, 'utf8');
    const editedSource = currentSource.replace('notify info "Work item closed"', 'notify info "Work item closed from MCP"');
    if (editedSource === currentSource) throw new Error('Could not prepare source edit for behavior notification text');

    try {
        await callTool(proposeSource, {
            expectedRevision: 'rev1:stale-for-transcript',
            expectedCatalogRevision,
            formatting: 'PreserveTrivia',
            validation: 'Authoring',
            documents: [{ documentId, source: editedSource, operation: 'replace-document' }]
        });
        block('mcp.staleRevision.refusal', 'stale expectedRevision was accepted');
    } catch (error) {
        record('mcp.staleRevision.refusal', 'passed', error.message.slice(0, 500));
    }

    const proposal = await callTool(proposeSource, {
        expectedRevision,
        expectedCatalogRevision,
        formatting: 'PreserveTrivia',
        validation: 'Authoring',
        documents: [{ documentId, source: editedSource, operation: 'replace-document' }]
    });
    const proposalId = proposal.proposalId ?? proposal.id;
    const proposalRevision = proposal.sourceRevision ?? proposal.expectedRevision ?? expectedRevision;
    if (!proposalId) throw new Error(`Propose did not return proposal id: ${JSON.stringify(proposal)}`);
    record('mcp.proposeEdit', 'passed', proposalId);

    const droppedComments = await callTool(readProposal, { proposalId, view: 'dropped-comments', expectedSourceRevision: proposalRevision, limit: 200 });
    const dropped = droppedComments.droppedComments ?? droppedComments.items ?? [];
    if (Array.isArray(dropped) && dropped.length === 0) record('mcp.droppedComments', 'passed', '0');
    else block('mcp.droppedComments', JSON.stringify(dropped).slice(0, 1_000));

    const applied = await callTool(apply, { proposalId, expectedRevision: proposalRevision, expectedCatalogRevision, includeContent: false });
    record('mcp.applyProposal', 'passed', JSON.stringify(applied).slice(0, 500));

    const afterSource = readFileSync(scratch.application, 'utf8');
    if (afterSource.includes(scratch.marker) && afterSource.includes('Work item closed from MCP')) record('mcp.commentPreservation.afterApply', 'passed');
    else block('mcp.commentPreservation.afterApply', 'marker or edit missing after apply');

    const reopened = await callTool(openWorkspace, { path: scratchRoot, includeContent: false });
    const afterRevision = reopened.sourceRevision ?? reopened.revision ?? reopened.expectedRevision;
    if (afterRevision && afterRevision !== expectedRevision) record('mcp.expectedRevision.changed', 'passed', `${expectedRevision} -> ${afterRevision}`);
    else block('mcp.expectedRevision.changed', `revision did not change: ${afterRevision ?? 'missing'}`);
}

async function run() {
    if (!endpoint) {
        result.status = 'blocked-missing-mcp-endpoint';
        result.message = 'Folder-source MCP revision editing requires a Studio-compatible MCP bridge endpoint or transcript runner. The typed-source Screenplay MCP spec remains the only executed MCP transcript.';
        record('mcp.openWorkspace', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
        record('mcp.proposeEdit', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
        record('mcp.applyProposal', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
        record('mcp.expectedRevision', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
        record('mcp.droppedComments', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
        return;
    }

    try {
        await runProtocol();
    } catch (error) {
        block('mcp.protocol', error.message);
    }
    result.status = result.remainingBlockers.length > 0 ? 'blocked-mcp-protocol-operation-failed' : 'passed';
}

run().then(() => {
    writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    console.log(JSON.stringify(result, null, 2));
    process.exit(result.status === 'passed' ? 0 : 1);
}).catch(error => {
    result.status = 'failed';
    result.message = error.message;
    writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    console.log(JSON.stringify(result, null, 2));
    process.exit(1);
});
