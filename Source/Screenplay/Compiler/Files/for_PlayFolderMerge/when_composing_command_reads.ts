// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { CommandReadMode } from '../../Syntax/CommandReadMode';
import { mergeDocuments } from '../PlayFolderMerge';

describe('when composing command reads', () => {
    it('should inherit the root mode without redeclaring it in fragments', () => {
        const merged = mergeDocuments([
            parse('command reads protected', 'application.play'),
            parse('module M\n  feature F\n    slice StateChange S\n      command Submit\n        value String', 'module.play'),
        ]);
        merged.success.should.be.true;
        merged.value.commandReadMode!.should.equal(CommandReadMode.Protected);
    });

    for (const second of ['command reads protected', 'command reads legacy']) {
        it(`should refuse a duplicate or conflicting ${second} across files`, () => {
            const merged = mergeDocuments([
                parse('command reads protected', 'application.play'),
                parse(second, 'other.play'),
            ]);
            merged.success.should.be.false;
            merged.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidCommandReadsDeclaration && diagnostic.location.path === 'other.play').should.be.true;
        });
    }

    it('should refuse a preamble in a placed fragment', () => {
        const merged = mergeDocuments([
            parse('module M', 'application.play'),
            parse('command reads protected', 'fragment.play', ['M']),
        ]);
        merged.success.should.be.false;
        merged.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidCommandReadsDeclaration && diagnostic.location.path === 'fragment.play').should.be.true;
    });
});
