// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { hoverContent, operationCompletions, responseTokens, scanDocument } from '@cratis/screenplay-language';
import { registerDefinitions } from '../Definitions';
import { ApplicationIndex } from '../ApplicationIndex';
import { WorkspaceApplication } from '../WorkspaceApplication';
import { reset, state } from '../vscode.stub';

function definitions(source: string, application?: WorkspaceApplication) {
    const uri = vscode.Uri.file('/workspace/current.play');
    const eventDefinitions = vi.fn(() => [new vscode.Location(uri, new vscode.Position(50, 0))]);
    const index = { fileOf: () => application ? { path: 'current.play', application } : undefined, eventDefinitions } as unknown as ApplicationIndex;
    registerDefinitions({ subscriptions: [] } as unknown as vscode.ExtensionContext, index);
    const provider = state.definitionProviders.at(-1)!.provider as vscode.DefinitionProvider;
    const lines = source.split('\n');
    const at = (line: number, column: number) => {
        const document = {
            uri,
            getText: (range?: vscode.Range) => range ? lines[line].slice(range.start.character, range.end.character) : source,
            getWordRangeAtPosition: () => {
                const match = [...lines[line].matchAll(/\w+/g)].find(match => column >= match.index && column < match.index + match[0].length);
                return match ? new vscode.Range(line, match.index, line, match.index + match[0].length) : undefined;
            }
        } as unknown as vscode.TextDocument;
        return provider.provideDefinition(document, new vscode.Position(line, column), {} as vscode.CancellationToken) as unknown as { uri: { fsPath: string }; range: { line: number; character: number } }[];
    };
    return { at, eventDefinitions };
}

describe('when navigating operation targets', () => {
    beforeEach(() => { reset(); state.workspaceFolder = '/workspace'; });
    it('should preserve standalone and inline event definition fallback', () => {
        for (const production of ['produces Recorded', 'produces S.Recorded', 'produces event Recorded']) {
            const source = `module M\n  feature F\n    slice StateChange S\n      event ${production.includes('event') ? 'Other' : 'Recorded'}\n      command C\n        ${production}`;
            const application = new WorkspaceApplication();
            application.set('current.play', source);
            const navigation = definitions(source, application);
            expect(navigation.at(5, source.split('\n')[5].indexOf('Recorded'))).toHaveLength(1);
            expect(navigation.eventDefinitions).toHaveBeenCalledWith(vscode.Uri.file('/workspace/current.play'), 'Recorded');
        }
    });
    it('should navigate standalone system uses and inline operation identifiers but not comment copies', () => {
        const source = 'system Mailer\noperation Send\n  uses Mailer // Mailer\ncommand C\n  produces Send // Send\n  produces operation Other\n    uses Mailer';
        const navigation = definitions(source);
        expect(navigation.at(2, 7)[0].range.line).toBe(0);
        expect(navigation.at(4, 11)[0].range.line).toBe(1);
        expect(navigation.at(5, 22)[0].range.line).toBe(5);
        expect(navigation.at(2, 17)).toEqual([]);
        expect(navigation.at(4, 19)).toEqual([]);
        expect(responseTokens(source.split('\n')).every(token => token.column + token.length <= source.split('\n')[token.line].length)).toBe(true);
    });
    it('should not bind another document’s uses span to a keyword-named current property', () => {
        const application = new WorkspaceApplication();
        application.set('intent.play', 'system Mailer\noperation Send\n  uses Mailer');
        const source = 'command C\n  name String\n  uses Mailer';
        application.set('current.play', source);
        const navigation = definitions(source, application);
        expect(navigation.at(2, 8)[0].range.line).toBe(50);
        expect(navigation.eventDefinitions).toHaveBeenCalled();
    });
    it('should navigate qualified conditional targets on the target line to real placed files', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module M\n  feature F\n    import "intent.play"\n    import "current.play"');
        application.set('intent.play', 'system Mailer\nslice StateChange Shared\n  operation Send\n    uses Mailer');
        const source = 'slice StateChange Here\n  command C\n    accepted Bool\n    produces when accepted == true\n      Shared.Send // Send';
        application.set('current.play', source);
        const navigation = definitions(source, application);
        expect(navigation.at(4, 14)[0]).toMatchObject({ uri: { fsPath: '/workspace/intent.play' }, range: { line: 2, character: 2 } });
        expect(navigation.at(4, 21)).toEqual([]);
        expect(hoverContent(source.split('\n'), 4, 'Send', 14, 18, application.symbolsExcept('current.play'))).toContain('M.F.Shared.Send');
    });
    it('should refuse operation/event collisions without falling back to an event', () => {
        const source = 'system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      event Send\n    slice StateChange C\n      command C\n        produces Send';
        const application = new WorkspaceApplication();
        application.set('current.play', source);
        const navigation = definitions(source, application);
        expect(navigation.at(10, 18)).toEqual([]);
        expect(navigation.eventDefinitions).not.toHaveBeenCalled();
    });
    it.each(['𐐀', '\uD801', '\uDC00', 'ß', '\u0301', '\u0661', '\u203F'])('should navigate only compiler-supported source stream declarations %s', suffix => {
        const application = new WorkspaceApplication();
        application.set('sources.play', `eventsource Account${suffix}\n  stream Transactions${suffix}`);
        const source = `command C\n  stream Account${suffix}.Transactions${suffix}`;
        const navigation = definitions(source, application);
        // There is no legacy event declaration to fall back to in this source-only fixture.
        navigation.eventDefinitions.mockReturnValue([]);
        const result = navigation.at(1, source.split('\n')[1].indexOf('Transactions'));
        expect(result).toHaveLength(/[\uD800-\uDFFF]/.test(suffix) ? 0 : 1);
        if (result.length) expect(result[0]).toMatchObject({ uri: { fsPath: '/workspace/sources.play' }, range: { start: { line: 1, character: 9 } } });
    });
    it.each([false, true])('should navigate a surviving stream only when physical fences are closed %s', closed => {
        const application = new WorkspaceApplication();
        application.set('sources.play', 'eventsource Account\n  stream Transactions');
        application.set('other.play', 'module Broken\n  description\n    ```text\neventsource Account\n  stream Other' + (closed ? '\n    ```' : ''));
        expect(definitions('command C\n  stream Account.Transactions', application).at(1, 18)).toHaveLength(closed ? 1 : 0);
    });
    it('should retain completion and source hover across unindented and indented comment-only lines', () => {
        for (const comment of ['// note', '  // note', '    // note', '        // note']) {
            const source = `system Mailer\noperation Send\n  uses Mailer\n  recipient String\ncommand C\n  text String\n  produces Send\n${comment}\n    recipient = text`;
            const lines = source.split('\n');
            const symbols = scanDocument(lines);
            expect(operationCompletions(lines, 8, '    recipient = te', symbols)?.map(entry => entry.label)).toEqual(['text']);
            expect(hoverContent(lines, 8, 'text', 17, 21, symbols)).toContain('Command source');
        }
    });
});
