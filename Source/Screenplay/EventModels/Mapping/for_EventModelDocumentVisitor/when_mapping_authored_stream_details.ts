// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const fixture = readFileSync(new URL('../../../../../Documentation/screenplay/fixtures/source-streams.play', import.meta.url), 'utf8');
const board = (source: string) => toEventModelDocument(parse(source).value, 'Banking').collections[0].modules[0].features[0].slices[0];

describe('when mapping authored stream details', () => {
    it('should show authored classification and readable expressions in existing command details only', () => {
        expect(parse(fixture).diagnostics).toEqual([]);
        const slice = board(fixture);
        expect(slice.command?.logicDescription).toContain('Authored stream: Account.Transactions');
        expect(slice.command?.logicDescription).toContain('Stream id: month');
        expect(slice.command?.logicDescription).toContain('not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302)');
        expect(slice.command?.logicDescription).not.toContain('PathExpressionSyntax');
        expect(slice.events.map(event => event.name)).toEqual(['Deposited']);
        expect(slice.specifications).toEqual([]);
    });
    it('should describe unkeyed authored streams without fabricating a key', () => {
        const unkeyed = fixture.replace('    streamId Month\n', '').replace('          streamId = month\n', '');
        expect(parse(unkeyed).diagnostics).toEqual([]);
        const details = board(unkeyed).command!.logicDescription;
        expect(details).toContain('Authored stream: Account.Transactions');
        expect(details).not.toContain('Stream id:');
    });
    it('should describe conflicting route candidates without selecting an effective route', () => {
        const duplicated = fixture.replace('        produces event Deposited', '        stream Account.Transactions\n          streamId = month\n        produces event Deposited');
        expect(board(duplicated).command?.logicDescription).toContain('Conflicting authored stream routes: no effective route selected');
        expect(board(duplicated).command?.logicDescription).not.toContain('Authored stream:');
    });
    it('should display literals as safe authored text without execution formatting', () => {
        const details = board(fixture.replace('concept Month : Int', 'concept Month : String').replace('streamId = month', 'streamId = "<Monthly & Annual>"')).command!.logicDescription;
        expect(details).toContain('Stream id: "&lt;Monthly &amp; Annual&gt;"');
        expect(details).not.toContain('LiteralExpressionSyntax');
        expect(details).not.toContain('<Monthly');
    });
    it('should not turn a viable property interpretation into a selected route', () => {
        const ambiguous = 'import Account.Transactions\ntype Transactions\n  value String\n' + fixture.replace('          streamId = month\n', '');
        expect(board(ambiguous).command?.logicDescription).toContain('no route selected (PLAY0505)');
        expect(board(ambiguous).command?.logicDescription).not.toContain('Authored stream:');
    });
});
