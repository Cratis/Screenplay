// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { GenerationSyntax } from '../../Syntax/GenerationSyntax';

describe('when parsing generation strategies', () => {
    it.each([
        ['Uuid', 'uuid'], ['Uuid', 'uuidv7'], ['String', 'uuid case lower prefix "ref_"'],
        ['String', 'ulid prefix "cus_" case upper'], ['String', 'typeid ""'], ['String', 'typeid "user"'],
        ['String', 'nanoid'], ['String', 'nanoid 16 prefix "id_" alphabet "abcdef0123456789"'],
        ['String', 'bytes 32 base64url'], ['String', 'bytes 16 hex case upper'], ['String', 'bytes 16 base32 case lower'],
        ['String', 'pattern "ORD-#{7}" checksum mod11'], ['Int', 'random -9..9'], ['String', 'rule MakeReference'], ['String', 'sequence'],
    ])('should retain %s generate %s', (primitive, strategy) => {
        const result = parse(`concept C : ${primitive}\n  generate ${strategy}`);
        result.diagnostics.should.be.empty;
        result.value.concepts[0].generation!.strategy.should.equal(strategy.split(' ')[0]);
    });

    it.each([
        ['generate rule', DiagnosticCodes.InvalidGenerationArgument],
        ['generate pattern', DiagnosticCodes.InvalidGenerationArgument],
        ['generate bytes 16 invalid', DiagnosticCodes.InvalidGenerationArgument],
        ['generate nanoid alphabet', DiagnosticCodes.InvalidGenerationArgument],
        ['generate ulid case invalid', DiagnosticCodes.InvalidGenerationArgument],
        ['generate pattern "#" checksum invalid', DiagnosticCodes.InvalidGenerationArgument],
        ['generate typeid "user_"', DiagnosticCodes.InvalidTypeIdPrefix],
        ['generate typeid "user\\n"', DiagnosticCodes.InvalidTypeIdPrefix],
        ['generate nanoid -1', DiagnosticCodes.GenerationSizeOutsideRange],
        ['generate bytes 513 hex', DiagnosticCodes.GenerationSizeOutsideRange],
        ['generate nanoid alphabet "a"', DiagnosticCodes.InvalidGenerationAlphabet],
        ['generate nanoid alphabet "a\\n"', DiagnosticCodes.InvalidGenerationAlphabet],
        ['generate ulid prefix "' + 'x'.repeat(33) + '"', DiagnosticCodes.InvalidGenerationPrefix],
        ['generate uuid unknown "x"', DiagnosticCodes.InvalidGenerationModifier],
    ])('should diagnose %s', (declaration, code) => {
        parse(`concept C : String\n  ${declaration}`).diagnostics.some(diagnostic => diagnostic.code === code).should.be.true;
    });

    it('should keep a bare generate line in an enumeration as a value', () => {
        const result = parse('concept Mode : Enum\n  generate\n  manual');
        result.diagnostics.should.be.empty;
        result.value.concepts[0].values.should.deep.equal(['generate', 'manual']);
    });

    it.each(['Bool', 'Date', 'DateTime', 'Decimal', 'Enum'])('should refuse generation on %s', primitive => {
        parse(`concept C : ${primitive}\n  generate uuid`).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidGenerationPrimitive).should.be.true;
    });

    it('should walk a generation declaration once', () => {
        const application = parse('concept C : String\n  generate nanoid').value;
        class Generations extends ScreenplaySyntaxWalker {
            readonly strategies: string[] = [];
            override visitGeneration(syntax: GenerationSyntax): void { this.strategies.push(syntax.strategy); }
        }
        const walker = new Generations();
        walker.visitApplication(application);
        walker.strategies.should.deep.equal(['nanoid']);
    });
});
