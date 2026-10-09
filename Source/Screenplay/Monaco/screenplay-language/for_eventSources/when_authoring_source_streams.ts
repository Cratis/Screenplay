// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { topLevelItems } from '../completion-items';
import { analyzeEventSources, eventSourceCompletions, eventSourceHover, eventSourceIdentifier, eventSourceReferenceAt } from '../event-source-authoring';
import { responseAnalysis } from '../response-analysis';
import { responseTokens } from '../response-tokens';
import { mergeSymbols, scanDocument } from '../symbols';
import { validateLines } from '../validation';

const declarations = 'concept AccountId : Uuid\nconcept Month : Int\neventsource Account\n  identifier AccountId\n  stream Transactions\n    streamId Month\n';
const command = 'command Deposit\n  accountId AccountId identifier\n  month Month\n  other String\n  stream Account.Transactions\n    streamId = month';
const source = declarations + command;
const symbols = (other = declarations, path = 'current.play') => ({ ...mergeSymbols(), authoringPath: path, authoringDocuments: [{ path: 'declarations.play', source: other }] });

function complete(source: string, marker: string, application = mergeSymbols()) {
    const lines = source.split('\n');
    const line = lines.findIndex(line => line.includes(marker));
    return eventSourceCompletions(lines, line, lines[line], { ...mergeSymbols(scanDocument(lines), application), authoringPath: application.authoringPath, authoringPlacement: application.authoringPlacement });
}

describe('when authoring source streams', () => {
    it('should describe the event routes admission without assigning a public version number', () => {
        const description = topLevelItems.find(item => item.label === 'eventsource')!.documentation;
        expect(description).toContain('event routes executable model');
        expect(description).not.toContain('not admitted');
        expect(description).not.toMatch(/ESM v\d/);
    });
    it('should retain application declarations around isolated command fragments', () => {
        const analysis = analyzeEventSources(source.split('\n'));
        expect(analysis.declarations.map(source => source.name)).toEqual(['Account']);
        expect(analysis.routes.map(route => [route.eventSource, route.stream])).toEqual([['Account', 'Transactions']]);
        expect(analysis.resolve('Account', 'Transactions').state).toBe('unique');
        expect(scanDocument(source.split('\n')).eventSources?.[0].identifier?.name).toBe('AccountId');
    });
    it('should complete exact qualified references without synthetic names', () => {
        expect(complete(command.replace('Account.Transactions', 'Account.Tr'), 'Account.Tr', symbols())?.map(entry => entry.insertText)).toEqual(['Transactions']);
        expect(complete(command.replace('Account.Transactions', 'Acc'), 'stream Acc', symbols())?.map(entry => entry.insertText)).toEqual(['Account.Transactions']);
    });
    it('should complete only proven compatible nonoptional scalar command properties', () => {
        const input = source.replace('  other String', '  other Month optional\n  many Month[]\n  foreign Unknown');
        expect(complete(input.replace('streamId = month', 'streamId = '), 'streamId = ')?.map(entry => entry.label)).toEqual(['month']);
        expect(complete(input.replace('streamId Month', 'streamId Missing').replace('streamId = month', 'streamId = '), 'streamId = ')).toEqual([]);
        expect(complete(input.replace('streamId Month', 'streamId Int').replace('streamId = month', 'streamId = '), 'streamId = ')).toEqual([]);
    });
    it('should complete nested known sources and hover the exact authored key expression', () => {
        const nested = 'type Period\n  month Month\n' + source.replace('  other String', '  period Period');
        expect(complete(nested.replace('streamId = month', 'streamId = period.'), 'streamId = period.')?.map(entry => entry.label)).toEqual(['month']);
        expect(complete(nested.replace('period Period', 'period Period optional').replace('streamId = month', 'streamId = period.'), 'streamId = period.')).toEqual([]);
        const lines = source.split('\n');
        const line = lines.findIndex(line => line.includes('streamId ='));
        const start = lines[line].lastIndexOf('month') + 1;
        expect(eventSourceHover(lines, line, start, start + 5)).toContain('Month');
        expect(eventSourceHover(lines, line, start, start + 5)).toContain('Command source for authored stream id');
        const hover = eventSourceHover(lines, line, 5, 13);
        expect(hover).toContain('Authored stream id mapping');
        expect(hover).toContain('admitted in the event routes executable model');
        expect(hover).not.toContain('Syntax-only');
        expect(hover).not.toContain('#302');
    });
    it('should use current typed source locations including every standalone location member', () => {
        const lines = command.split('\n');
        const analysis = analyzeEventSources(lines, symbols());
        expect(analysis.routes[0].referenceLocation).toEqual({ path: 'current.play', line: 5, column: 10 });
        expect(analysis.routes[0].streamId?.location.line).toBe(6);
        const reference = eventSourceReferenceAt(lines, 4, 18, 30, symbols());
        expect(reference?.target?.name).toBe('Transactions');
        expect(reference?.target?.location).toEqual({ path: 'declarations.play', line: 5, column: 3 });
        expect(eventSourceIdentifier(reference!.target!.location, 'Transactions', declarations)?.column).toBe(10);
    });
    it('should not navigate or complete a child of duplicate physical parents', () => {
        const application = symbols(declarations + 'eventsource Account\n  stream Other');
        const lines = command.split('\n');
        expect(eventSourceReferenceAt(lines, 4, 18, 30, application)?.target).toBeUndefined();
        expect(complete(command.replace('Account.Transactions', 'Account.Tr'), 'Account.Tr', application)).toEqual([]);
    });
    it('should preserve both viable interpretations and all legal property names', () => {
        const types = declarations + 'import Account.Transactions\ntype Transactions\n  value String';
        const legacy = command.replace('    streamId = month', '  eventsource String\n  from String\n  streamId String\n  identifier String');
        const analysis = responseAnalysis(legacy.split('\n'), symbols(types).authoringDocuments);
        expect(analysis.commands.get(0)?.properties.map(property => property.name)).toEqual(['accountId', 'month', 'other', 'eventsource', 'from', 'streamId', 'identifier']);
        expect(analysis.eventSources.routes).toEqual([]);
        expect(analysis.eventSources.ambiguousCandidates).toHaveLength(1);
        expect(eventSourceReferenceAt(legacy.split('\n'), 4, 18, 30, symbols(types))?.resolution.state).toBe('ambiguous');
        expect(eventSourceReferenceAt(legacy.split('\n'), 4, 18, 30, symbols(types))?.target).toBeUndefined();
        expect(complete(command.replace('Account.Transactions', 'Account.Tr'), 'Account.Tr', symbols(types))).toEqual([]);
        expect(analysis.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0505');
        expect(responseTokens(legacy.split('\n'), symbols(types)).some(token => token.line === 4 && token.type === 0)).toBe(false);
        expect(responseAnalysis(['command C', '  stream String', '  @stream Account.Transactions'], symbols().authoringDocuments).commands.get(0)?.properties.map(property => property.name)).toEqual(['stream', 'stream']);
    });
    it('should not show a bogus unknown property type for an authored cross-file route', () => {
        expect(validateLines(command.split('\n'), { application: symbols() }).filter(issue => issue.message.includes("Unknown type 'Account.Transactions'"))).toEqual([]);
    });
    it('should ignore repeated names in comments and fenced descriptions', () => {
        const lines = (source + '\n  description\n    ```text\n    stream Account.Transactions\n    ```').split('\n');
        expect(eventSourceReferenceAt(['command C', '  stream Account.Transactions // Account.Transactions'], 1, 34, 46, symbols())).toBeNull();
        expect(eventSourceCompletions(lines, lines.length - 2, lines[lines.length - 2], symbols())).toBeNull();
        expect(eventSourceHover(declarations.split('\n'), 4, 10, 22)).toContain('Stream id type: Month');
    });
    it('should keep unresolved placements out of confident navigation', () => {
        const application = { ...symbols(), authoringDocuments: [{ path: 'declarations.play', source: declarations, placement: ['M', 'F'], isPlacementResolved: false }] };
        expect(analyzeEventSources(command.split('\n'), application).declarations.map(source => source.name)).toEqual(['Account']);
        expect(analyzeEventSources(command.split('\n'), application).resolve('Account', 'Transactions').state).toBe('incomplete');
        expect(eventSourceReferenceAt(command.split('\n'), 4, 18, 30, application)?.target).toBeUndefined();
    });
    it('should disclose conflicting placements and unreadable parser extent on every route surface', () => {
        for (const unknown of [false, true]) {
            const application = { ...symbols(), authoringDocuments: [
                ...symbols().authoringDocuments,
                { path: 'other.play', source: unknown ? 'unknown block\n  eventsource Account\n    stream Transactions' : 'eventsource Account\n  stream Other', isPlacementResolved: unknown }
            ] };
            const lines = command.split('\n');
            const analysis = analyzeEventSources(lines, application);
            expect(analysis.resolve('Account', 'Transactions').state).toBe(unknown ? 'incomplete' : 'ambiguous');
            expect(analysis.resolve('Account', 'Transactions').reasons.length).toBeGreaterThan(0);
            expect(eventSourceReferenceAt(lines, 4, 18, 30, application)?.target).toBeUndefined();
            expect(eventSourceHover(lines, 4, 18, 30, application)).toContain(unknown ? 'incomplete' : 'ambiguous');
            expect(complete(command.replace('Account.Transactions', 'Account.Tr'), 'Account.Tr', application)).toEqual([]);
            expect(analysis.resolve('Foreign.Account', 'Transactions').state).not.toBe('unique');
        }
    });
    it('should retain declaration order, case sensitivity and authored UTF-16 spans with tabs and comments', () => {
        for (const documents of [symbols().authoringDocuments, [...symbols().authoringDocuments].reverse()]) {
            const lines = ['command Deposit // 🚀 Deposit', '\tmonth Month', '\tstream Account.Transactions // Account.Transactions', '\t\tstreamId = month'];
            const analysis = responseAnalysis(lines, documents);
            const route = analysis.eventSources.routes[0];
            expect(route.referenceLocation?.column).toBe(lines[2].indexOf('Account.Transactions') + 1);
            expect(eventSourceReferenceAt(lines, 2, route.referenceLocation!.column, route.referenceLocation!.column + 7, symbols())?.target?.name).toBe('Account');
            expect(analysis.eventSources.resolve('account', 'Transactions').state).toBe('notFound');
            expect(analysis.eventSources.resolve('Account', 'transactions').state).toBe('notFound');
        }
    });
    it('should not offer routes inside event or operation payloads', () => {
        for (const production of ['produces event Recorded', 'produces operation Send\n    uses Mailer']) {
            const current = 'system Mailer\ncommand C\n  ' + production + '\n    stream Account.Tr';
            expect(complete(current, 'stream Account.Tr', symbols())).toBeNull();
        }
    });
    it('should retain unknown original root extent around a normalized command fragment', () => {
        const lines = (declarations + 'unknown block\n  eventsource Account\n    stream Other\n' + command).split('\n');
        const analysis = analyzeEventSources(lines);
        expect(analysis.declarations.map(source => source.name)).toEqual(['Account']);
        expect(analysis.resolve('Account', 'Transactions').state).toBe('incomplete');
        expect(analysis.targets).toEqual([]);
    });
    it.each([false, true])('should disclose an unclosed physical fence without hiding visible declarations %s', closed => {
        const application = symbols(declarations + 'module Broken\n  description\n    ```text\neventsource Account\n  stream Other' + (closed ? '\n    ```' : ''));
        const lines = command.split('\n');
        const analysis = analyzeEventSources(lines, application);
        expect(analysis.declarations.map(source => source.name)).toEqual(['Account']);
        expect(analysis.resolve('Account', 'Transactions').state).toBe(closed ? 'unique' : 'incomplete');
        expect(eventSourceReferenceAt(lines, 4, 18, 30, application)?.target?.name).toBe(closed ? 'Transactions' : undefined);
        expect(eventSourceHover(lines, 4, 18, 30, application)).toContain(closed ? 'Account.Transactions' : 'incomplete');
        expect(complete(command.replace('Account.Transactions', 'Account.Tr'), 'Account.Tr', application)?.map(entry => entry.label)).toEqual(closed ? ['Account.Transactions'] : []);
        if (!closed) expect(responseAnalysis(application.authoringDocuments[0].source.split('\n'), [], undefined, 'declarations.play').diagnostics).toContainEqual(expect.objectContaining({ code: 'PLAY0164', location: expect.objectContaining({ path: 'declarations.play' }) }));
    });
    it.each(['𐐀', '\uD801', '\uDC00'])('should not complete or navigate unsupported declared names %s', suffix => {
        const application = symbols(declarations.replaceAll('Account', 'Account' + suffix));
        expect(complete(command.replace('Account.Transactions', 'Acc'), 'stream Acc', application)).toEqual([]);
        expect(eventSourceReferenceAt(command.replaceAll('Account', 'Account' + suffix).split('\n'), 4, 18, 30, application)?.target).toBeUndefined();
        const invalidStream = symbols(declarations.replaceAll('Transactions', 'Transactions' + suffix));
        expect(complete(command.replace('Account.Transactions', 'Account.Tr'), 'Account.Tr', invalidStream)).toEqual([]);
    });
    it.each(['ß', '\u0301', '\u0661', '\u203F'])('should complete and navigate compiler-supported BMP declarations %s', suffix => {
        const application = symbols(declarations.replaceAll('Account', 'Account' + suffix).replaceAll('Transactions', 'Transactions' + suffix));
        const qualified = `Account${suffix}.Transactions${suffix}`;
        expect(complete(command.replace('Account.Transactions', 'Acc'), 'stream Acc', application)?.map(entry => entry.label)).toEqual([qualified]);
        expect(eventSourceReferenceAt(command.replace('Account.Transactions', qualified).split('\n'), 4, 18 + suffix.length, 30 + suffix.length * 2, application)?.target?.name).toBe('Transactions' + suffix);
    });
    it('should share one cached analysis across source consumers', () => {
        const lines = source.split('\n');
        expect(analyzeEventSources(lines)).toBe(analyzeEventSources(lines));
        expect(analyzeEventSources(lines)).toBe(responseAnalysis(lines).eventSources);
    });
});
