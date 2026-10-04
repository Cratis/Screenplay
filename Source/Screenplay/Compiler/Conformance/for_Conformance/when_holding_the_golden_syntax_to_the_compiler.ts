// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const variable = 'SCREENPLAY_REGENERATE_SYNTAX_VECTORS';
const conformance = resolve(__dirname, '..');
const repository = resolve(conformance, '../../../..');

interface ConformanceDocument {
    readonly name: string;
    readonly path: string;
}

// The golden syntax is what this compiler writes for each document of the conformance corpus. The C#
// compiler is held to the same files, so the two can only drift apart by failing one side or the other.
// Run the specs with SCREENPLAY_REGENERATE_SYNTAX_VECTORS=1 to rewrite them, then review the diff.
// Decode only the new additive omissions in old vectors; never omit actual new members.
// The source-stream fixture is deliberately excluded so every member is required there.
function withSourceStreamDefaults(value: unknown): unknown {
    if (Array.isArray(value)) return value.map(withSourceStreamDefaults);
    if (typeof value !== 'object' || value === null) return value;
    const node = value as Record<string, unknown>;
    if (node.kind === 'ApplicationSyntax' && !('eventSources' in node)) node.eventSources = [];
    if (node.kind === 'CommandSyntax' && !('stream' in node)) node.stream = null;
    if (node.kind === 'CommandSyntax' && !('streamCandidates' in node)) node.streamCandidates = [];
    return Object.fromEntries(['kind', ...Object.keys(node).filter(key => key !== 'kind').sort()].map(key => [key, withSourceStreamDefaults(node[key])]));
}

describe('when holding the golden syntax to the compiler', () => {
    let documents: ConformanceDocument[];
    let mismatched: string[];

    beforeAll(() => {
        documents = (JSON.parse(readFileSync(join(conformance, 'manifest.json'), 'utf8')) as { documents: ConformanceDocument[] }).documents;
        mismatched = [];
        for (const document of documents) {
            const golden = join(conformance, `${document.name}.syntax.json`);
            const rendered = `${JSON.stringify(toSyntaxJson(parse(readFileSync(join(repository, document.path), 'utf8')).value), null, 2)}\n`;
            if (process.env[variable] === '1') {
                writeFileSync(golden, rendered);
            } else if ((document.name.startsWith('source-stream') ? readFileSync(golden, 'utf8').replaceAll('\r\n', '\n') : `${JSON.stringify(withSourceStreamDefaults(JSON.parse(readFileSync(golden, 'utf8'))), null, 2)}\n`) !== rendered) {
                mismatched.push(document.name);
            }
        }
    });

    it('should hold documents', () => {
        documents.length.should.be.greaterThan(0);
    });

    it('should not be regenerating', () => {
        (process.env[variable] ?? '').should.not.equal('1', `Rewrote the golden syntax - review the diff and rerun without ${variable}`);
    });

    it('should match the golden syntax of every document', () => {
        mismatched.should.deep.equal([]);
    });
});
