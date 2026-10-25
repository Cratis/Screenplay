// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { WorkspaceApplication } from '../WorkspaceApplication';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const clauses = (grammar.repository['action-subjects'].patterns as { match: string }[]).map(rule => new RegExp(rule.match));
const source = readFileSync(new URL('../../Compiler/Conformance/action-subjects/source.play', import.meta.url), 'utf8');
const relevant = (code: string) => /^PLAY081[5-9]$/.test(code);

describe('when highlighting action subjects', () => {
    for (const line of ['action Retry for item', 'action "Choose" for screen', 'action $strings.choose for item', 'item retry action Work.Items.Retry for screen']) {
        it(`should highlight the contextual clause in ${line}`, () => {
            const match = clauses.map(rule => rule.exec(line)).find(match => match !== null)!;
            expect(match).not.toBeUndefined();
            expect(match.at(-3)).toBe('for');
            expect(match.at(-1)).toBe(line.endsWith('item') ? 'item' : 'screen');
        });
    }
    for (const line of ['for String', 'item String', 'screen String', 'action "for item"', 'action Retry label "for screen"', '// action Retry for item']) {
        it(`should not create a subject clause in ${line}`, () => expect(clauses.some(rule => rule.test(line))).toBe(false));
    }
    it('should carry every action-subject diagnostic through the workspace facade', () => {
        const workspace = new WorkspaceApplication();
        workspace.set('application.play', source);
        const expected = parse(source, 'application.play').diagnostics.filter(diagnostic => relevant(diagnostic.code));
        expect(new Set(expected.map(diagnostic => diagnostic.code)).size).toBe(5);
        expect(workspace.diagnosticsFor('application.play').filter(diagnostic => relevant(diagnostic.code))).toEqual(expected);
    });
});
