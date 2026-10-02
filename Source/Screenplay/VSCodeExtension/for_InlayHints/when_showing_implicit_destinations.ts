// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import * as vscode from 'vscode';
import { inlayHintsProvider } from '../InlayHints';

describe('when showing implicit destinations', () => {
    it('should place the identifier hint at the end of the production header', async () => {
        const text = 'command Rename\n  projectId Uuid identifier\n  produces event Renamed\n';
        const document = { getText: () => text } as vscode.TextDocument;
        const hints = await inlayHintsProvider.provideInlayHints(document, new vscode.Range(0, 0, 3, 0), {} as vscode.CancellationToken);
        expect(hints?.map(hint => hint.label)).toEqual(['for projectId']);
        expect(hints?.[0].position.line).toBe(2);
        expect(hints?.[0].paddingLeft).toBe(true);
    });
});
