// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import type { CancellationToken, editor, languages, Range } from 'monaco-editor';
import { createCodeActionProvider } from '../code-actions';

const design = 'design system D\n  modes light\n  default theme T\n  tokens\n    color\n      surface\n    radius\n      control\n\ntheme T\n  implements D\n  light\n    color.surface = "#FFFFFF"\n';
const shell = 'ui profile P\n  appearance\n    color scheme light\n    corners round\n';
const token = { isCancellationRequested: false } as CancellationToken;
const range = { startLineNumber: 4, startColumn: 1, endLineNumber: 4, endColumn: 18 } as Range;
const context = { markers: [{ ...range, code: 'PLAY0929', message: 'deprecated', severity: 4 }], trigger: 1, only: 'quickfix' } as languages.CodeActionContext;

function model(path: string, text: string): editor.ITextModel {
    return {
        uri: { toString: () => `file:///${path}` },
        getValue: vi.fn(() => text),
        getVersionId: () => 1,
        getPositionAt: (offset: number) => {
            const lines = text.slice(0, offset).split('\n');
            return { lineNumber: lines.length, column: lines.at(-1)!.length + 1 };
        },
    } as unknown as editor.ITextModel;
}

describe('when moving the deprecated appearance members in Monaco', () => {
    it('should offer the repair for a single model that holds the design system, theme and profile', async () => {
        const text = `${design}\n${shell}`;
        const lineRange = { ...range, startLineNumber: 18, endLineNumber: 18 } as Range;
        const result = await createCodeActionProvider().provideCodeActions(model('a.play', text), lineRange, { ...context, markers: [{ ...context.markers[0], ...lineRange }] }, token);
        expect(result?.actions.map(action => action.title)).toEqual(["Move the appearance values into theme 'T'"]);
    });

    it('should edit the model that holds the theme and pin every analyzed model', async () => {
        const profile = model('shell.play', shell);
        const themeModel = model('design.play', design);
        const sibling = model('sibling.play', 'trigger Tick');
        const documents = new Map([['shell.play', profile], ['design.play', themeModel], ['sibling.play', sibling]]);
        const result = await createCodeActionProvider([], () => documents).provideCodeActions(profile, range, context, token);
        expect(result?.actions).toHaveLength(1);
        const resources = result!.actions[0].edit!.edits.map(edit => 'resource' in edit ? String(edit.resource) : '');
        expect(new Set(resources)).toEqual(new Set(['file:///shell.play', 'file:///design.play', 'file:///sibling.play']));
    });
});
