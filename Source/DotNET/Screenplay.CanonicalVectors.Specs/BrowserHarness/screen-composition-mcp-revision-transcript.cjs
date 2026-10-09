// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { writeFileSync } = require('node:fs');

const resultPath = process.env.SCREENPLAY_MCP_RESULT ?? `${process.cwd()}/.ai-work/mcp-revision-transcript-result.json`;
const endpoint = process.env.SCREENPLAY_MCP_ENDPOINT;
const workspace = process.env.SCREENPLAY_MCP_WORKSPACE ?? 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1/source/folder';

const result = {
    name: 'mcp-folder-revision-edit-transcript',
    workspace,
    endpoint: endpoint ?? null,
    status: 'unknown',
    assertions: []
};

function record(path, status, value = undefined) {
    result.assertions.push({ path, status, value });
}

if (!endpoint) {
    result.status = 'blocked-missing-mcp-endpoint';
    result.message = 'Folder-source MCP revision editing requires a Studio-compatible MCP bridge endpoint or transcript runner. The typed-source Screenplay MCP spec remains the only executed MCP transcript.';
    record('mcp.openWorkspace', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
    record('mcp.proposeEdit', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
    record('mcp.applyProposal', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
    record('mcp.expectedRevision', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
    record('mcp.droppedComments', 'pending', 'SCREENPLAY_MCP_ENDPOINT not set');
} else {
    result.status = 'blocked-unimplemented-mcp-client';
    result.message = 'Endpoint was supplied, but this repository does not yet include a generic MCP client transcript runner. Wire this script to the Studio-compatible MCP bridge before claiming folder-source MCP pass.';
    record('mcp.endpoint.supplied', 'passed', endpoint);
    record('mcp.openWorkspace', 'pending', 'MCP transcript client not wired');
    record('mcp.proposeEdit', 'pending', 'MCP transcript client not wired');
    record('mcp.applyProposal', 'pending', 'MCP transcript client not wired');
}

writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
process.exit(result.status === 'passed' ? 0 : 1);
