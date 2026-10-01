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
            } else if (readFileSync(golden, 'utf8').replaceAll('\r\n', '\n') !== rendered) {
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
