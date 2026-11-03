// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import * as vscode from 'vscode';
import { EventModelBoardEditor } from '../EventModelBoardEditor';
import { defaultViewOptions } from '../ViewOptionsStore';
import { a_configuration } from '../for_ViewOptionsStore/given/a_configuration';
import { a_memento } from '../for_ViewOptionsStore/given/a_memento';
import { a_board_panel } from './given/a_board_panel';

const host = vi.hoisted(() => ({
    configuration: undefined as ReturnType<typeof a_configuration> | undefined,
    provider: undefined as EventModelBoardEditor | undefined,
    listeners: new Set<(event: vscode.ConfigurationChangeEvent) => void>(),
    warning: vi.fn(),
}));

vi.mock('vscode', async importOriginal => ({
    ...await importOriginal<typeof vscode>(),
    workspace: {
        getConfiguration: (section: string) => {
            section.should.equal('screenplay.eventModelBoard');
            return host.configuration;
        },
        onDidChangeConfiguration: (listener: (event: vscode.ConfigurationChangeEvent) => void) => {
            host.listeners.add(listener);
            return { dispose: () => host.listeners.delete(listener) };
        },
    },
    window: {
        registerCustomEditorProvider: (_type: string, provider: EventModelBoardEditor) => {
            host.provider = provider;
            return { dispose: () => {} };
        },
        showWarningMessage: host.warning,
    },
}));
vi.mock('../BoardRefresh', () => ({ BoardRefresh: class { now() {} dispose() {} } }));
vi.mock('../boardHtml', () => ({ boardHtml: () => '<html></html>', createNonce: () => 'nonce' }));

let context: vscode.ExtensionContext;
let first: ReturnType<typeof a_board_panel>;
let second: ReturnType<typeof a_board_panel>;

function configurationChanged(section: string) {
    for (const listener of host.listeners) listener({ affectsConfiguration: (candidate: string) => candidate === section } as vscode.ConfigurationChangeEvent);
}

beforeEach(() => {
    host.listeners.clear();
    host.warning.mockReset();
    host.configuration = a_configuration();
    context = { extensionUri: vscode.Uri.file('/extension'), globalState: a_memento(), subscriptions: [] } as unknown as vscode.ExtensionContext;
    EventModelBoardEditor.register(context);
    first = a_board_panel();
    second = a_board_panel();
    const document = { uri: vscode.Uri.file('/work.play') } as vscode.TextDocument;
    host.provider!.resolveCustomTextEditor(document, first.panel);
    host.provider!.resolveCustomTextEditor(document, second.panel);
});

describe('when synchronizing view options', () => {
    it('should send normalized current settings on ready without changing stored preferences', async () => {
        host.configuration!.values.set('density', 'spacious');
        host.configuration!.values.set('textSize', 'bad');
        await context.globalState.update('screenplay.eventModelBoard.viewOptions', { detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines' });
        await first.receive({ type: 'ready' });
        first.messages.should.deep.equal([{ type: 'viewOptions', options: {
            ...defaultViewOptions, detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines', density: 'spacious',
        } }]);
    });

    for (const [section, value] of [['density', 'compact'], ['textSize', 'large']]) {
        it(`should refresh every open board for external ${section} changes, including hidden panels`, () => {
            host.configuration = a_configuration({ [section]: value });
            configurationChanged(`screenplay.eventModelBoard.${section}`);
            const expected = [{ type: 'viewOptions', options: { ...defaultViewOptions, [section]: value } }];
            first.messages.should.deep.equal(expected);
            second.messages.should.deep.equal(expected);
            host.configuration!.updates.should.have.lengthOf(0);
        });
    }

    it('should ignore irrelevant configuration changes', () => {
        configurationChanged('editor.fontSize');
        first.messages.should.have.lengthOf(0);
        second.messages.should.have.lengthOf(0);
    });

    it('should save a board change and broadcast authoritative options to both boards', async () => {
        const options = { ...defaultViewOptions, detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines', density: 'spacious', textSize: 'large' } as const;
        await first.receive({ type: 'viewOptionsChanged', options });
        first.messages.should.deep.equal([{ type: 'viewOptions', options }]);
        second.messages.should.deep.equal(first.messages);
        host.configuration!.updates.should.have.lengthOf(2);
        await second.receive({ type: 'ready' });
        second.messages.at(-1)!.should.deep.equal({ type: 'viewOptions', options });
    });

    it('should retain old preferences after an external setting change and on reopen', async () => {
        await first.receive({ type: 'viewOptionsChanged', options: { ...defaultViewOptions, showProperties: true } });
        host.configuration!.values.set('textSize', 'large');
        configurationChanged('screenplay.eventModelBoard.textSize');
        second.messages.at(-1)!.should.deep.equal({ type: 'viewOptions', options: { ...defaultViewOptions, showProperties: true, textSize: 'large' } });
        const reopened = a_board_panel();
        host.provider!.resolveCustomTextEditor({ uri: vscode.Uri.file('/later.play') } as vscode.TextDocument, reopened.panel);
        await reopened.receive({ type: 'ready' });
        reopened.messages.should.deep.equal([second.messages.at(-1)]);
    });

    it('should stop sending options to a disposed panel', () => {
        first.dispose();
        configurationChanged('screenplay.eventModelBoard.density');
        first.messages.should.have.lengthOf(0);
        second.messages.should.have.lengthOf(1);
    });

    it('should dispose the configuration listener with the extension', () => {
        host.listeners.size.should.equal(1);
        for (const subscription of context.subscriptions) subscription.dispose();
        host.listeners.size.should.equal(0);
        configurationChanged('screenplay.eventModelBoard.textSize');
        second.messages.should.have.lengthOf(0);
    });

    it('should report a rejected save and restore readable authoritative options without rejecting the message handler', async () => {
        host.configuration!.update = async () => { throw new Error('Settings are read-only'); };
        await first.receive({ type: 'viewOptionsChanged', options: { ...defaultViewOptions, density: 'spacious', textSize: 'large' } });
        host.warning.mock.calls.should.have.lengthOf(1);
        first.messages.should.deep.equal([{ type: 'viewOptions', options: defaultViewOptions }]);
        second.messages.should.deep.equal(first.messages);
    });
});
