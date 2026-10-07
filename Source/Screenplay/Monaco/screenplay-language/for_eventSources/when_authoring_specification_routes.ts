// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { eventSourceCompletions, eventSourceHover, eventSourceReferenceAt } from '../event-source-authoring';
import { responseTokens } from '../response-tokens';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

const declarations = 'eventsource Account\n  identifier String\n  stream Transactions\n    streamId String\n  stream Profile\n';
const specification = 'specification History\n  given Recorded\n    for "other"\n    stream Account.Transactions\n      streamId = "p-1:2026-10"\n  when append Recorded\n    for "other"\n    stream Account.Profile\n  then Recorded\n    no stream';
const lines = (declarations + specification).split('\n');
const complete = (lines: string[], line = lines.length - 1) => eventSourceCompletions(lines, line, lines[line], scanDocument(lines));

describe('when authoring specification routes', () => {
    it('should complete qualified streams in every event role', () => {
        for (const header of ['given Recorded', 'when append Recorded', 'then Recorded']) {
            const current = (declarations + `specification History\n  ${header}\n    stream Account.Tr`).split('\n');
            complete(current)!.map(entry => entry.insertText).should.deep.equal(['Transactions', 'Profile']);
        }
    });
    it('should offer stream only on facts and no stream only on expectations', () => {
        for (const header of ['given Recorded', 'when append Recorded', 'then Recorded']) {
            const current = (declarations + `specification History\n  ${header}\n    `).split('\n');
            const labels = complete(current)!.map(entry => entry.label);
            labels.should.contain('stream');
            labels.includes('no stream').should.equal(header === 'then Recorded');
        }
    });
    it('should complete only the remaining word after no', () => {
        const current = (declarations + 'specification History\n  then Recorded\n    no st').split('\n');
        complete(current)![0].insertText.should.equal('stream');
    });
    it('should offer a literal stream id below a keyed route', () => {
        const current = (declarations + 'specification History\n  given Recorded\n    stream Account.Transactions\n      ').split('\n');
        complete(current)!.map(entry => entry.label).should.deep.equal(['streamId']);
        complete(current)![0].insertText.should.contain('streamId =');
    });
    it('should not offer stream ids below unkeyed routes or duplicate route statements', () => {
        const current = (declarations + 'specification History\n  then Recorded\n    stream Account.Profile\n      ').split('\n');
        complete(current)!.should.deep.equal([]);
        current[current.length - 1] = '    ';
        complete(current)!.should.deep.equal([]);
    });
    it('should hover and navigate the exact route operand on each fact', () => {
        for (const line of [8, 12]) {
            const start = lines[line].indexOf('Account') + 1;
            eventSourceReferenceAt(lines, line, start, start + 7)!.target!.name.should.equal('Account');
            eventSourceHover(lines, line, start, start + 7)!.should.contain('Account.');
        }
        eventSourceHover(lines, 9, 7, 15)!.should.contain('literal');
        eventSourceHover(lines, 14, 5, 7)!.should.contain('unrouted');
    });
    it('should overlay route keywords without treating payload names as routes', () => {
        const tokens = responseTokens([...lines, '    stream = "payload"', '    streamId = "payload"']);
        tokens.some(token => token.line === 8 && token.column === 4 && token.type === 0).should.equal(true);
        tokens.some(token => token.line === 9 && token.column === 6 && token.type === 0).should.equal(true);
        tokens.some(token => token.line === 14 && token.column === 4 && token.type === 0).should.equal(true);
        tokens.some(token => token.line >= 15 && token.type === 0).should.equal(false);
    });
    it.each(['PLAY0547', 'PLAY0548', 'PLAY0549', 'PLAY0550', 'PLAY0551'])('should surface compiler diagnostic %s', code => {
        validateLines(lines, { compilerDiagnostics: [{ code, message: 'route diagnostic', severity: 'error', location: { path: 'current.play', line: 9, column: 5 } }] }).some(issue => issue.code === code).should.equal(true);
    });
    it('should complete imported cross-file targets without command property ambiguity', () => {
        const current = ['specification History', '  then Recorded', '    stream Account.Tr'];
        const symbols = { ...scanDocument(current), authoringDocuments: [{ path: 'sources.play', source: 'import Account.Transactions\n' + declarations }] };
        eventSourceCompletions(current, 2, current[2], symbols)!.map(entry => entry.insertText).should.deep.equal(['Transactions', 'Profile']);
    });
    it('should not claim a parser-refused unrouted spelling is a typed assertion', () => {
        const current = lines.map(line => line.replace('no stream', 'no   stream'));
        responseTokens(current).some(token => token.line === 14 && token.type === 0).should.equal(false);
        (eventSourceHover(current, 14, 10, 16) === null).should.equal(true);
    });
    it('should leave command and read-model specification contexts alone', () => {
        for (const header of ['when Deposit', 'given readmodel History', 'then readmodel History', 'then query History']) {
            const current = (declarations + `specification History\n  ${header}\n    stream Account.Tr`).split('\n');
            (complete(current) === null).should.equal(true);
        }
    });
});
