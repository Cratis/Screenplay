// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { CommandReadMode } from '../../Syntax/CommandReadMode';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

describe('when parsing command reads', () => {
    for (const source of ['command reads protected', 'command  reads  protected // decision consistency']) {
        it(`should parse the explicit root mode in ${source}`, () => {
            const parsed = parse(source);
            parsed.success.should.be.true;
            parsed.value.commandReadMode!.should.equal(CommandReadMode.Protected);
            (toSyntaxJson(parsed.value) as Record<string, unknown>).commandReadMode!.should.equal('Protected');
        });
    }

    for (const source of ['command reads protected\ncommand reads protected', 'command reads protected\ncommand reads legacy', 'command reads unknown', 'command reads protected extra', 'command reads']) {
        it(`should refuse duplicate conflicting or incomplete modes in ${source}`, () => {
            const parsed = parse(source);
            parsed.success.should.be.false;
            parsed.diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.InvalidCommandReadsDeclaration);
        });
    }

    for (const scope of [['M'], ['M', 'F']]) {
        it(`should refuse a preamble placed in ${scope.join('.')}`, () => {
            const parsed = parse('command reads protected', 'fragment.play', scope);
            parsed.success.should.be.false;
            parsed.diagnostics.map(diagnostic => diagnostic.code).should.contain(DiagnosticCodes.InvalidCommandReadsDeclaration);
            ('commandReadMode' in parsed.value).should.be.false;
        });
    }

    for (const name of ['Submit', 'reads']) {
        it(`should not mistake the command ${name} for the preamble`, () => {
            const parsed = parse(`module M\n  feature F\n    slice StateChange S\n      command ${name}\n        value String`);
            parsed.success.should.be.true;
            ('commandReadMode' in parsed.value).should.be.false;
            parsed.value.modules[0].features[0].slices[0].commands[0].name.should.equal(name);
            ('commandReadMode' in (toSyntaxJson(parsed.value) as Record<string, unknown>)).should.be.false;
        });
    }
});
