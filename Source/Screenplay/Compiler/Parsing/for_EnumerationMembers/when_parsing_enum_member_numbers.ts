// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';
import { decodeExactSyntaxJson } from '../../Syntax/StrictSyntaxJson';
import type { ApplicationSyntax } from '../../Syntax/Structure';

const event = '\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status';

describe('when parsing enum member numbers', () => {
    let result: ReturnType<typeof parse>;
    beforeEach(() => {
        result = parse('concept Status : Enum\n  description "identity"\n  draft = -2147483648\n  issued = 2147483647\n  @validate = -0\n  validate\n    not empty');
    });
    it('should keep names separate from their numbers', () => result.value.concepts[0].values.should.deep.equal(['draft', 'issued', 'validate']));
    it('should retain int32 boundaries and normalize negative zero', () => result.value.concepts[0].valueNumbers!.should.deep.equal([-2147483648, 2147483647, 0]));
    it('should retain descriptions and validation', () => {
        result.value.concepts[0].description!.should.equal('identity');
        result.value.concepts[0].validations!.should.have.lengthOf(1);
    });
    it('should keep Unicode identifier continuations', () => {
        const concept = parse('concept Status : Enum\n  café = 1\n  søk = 4').value.concepts[0];
        concept.values.should.deep.equal(['café', 'søk']);
        concept.valueNumbers!.should.deep.equal([1, 4]);
    });
    it('should not report errors for valid numbers', () => result.diagnostics.should.have.lengthOf(0));
});

describe('when rejecting invalid enum member numbers', () => {
    it.each([
        ['  draft = 0\n  issued', 'PLAY0667'],
        ['  draft\n  issued = 1', 'PLAY0667'],
        ['  draft = 4\n  issued = 4', 'PLAY0668'],
        ['  draft = 0\n  issued = -0', 'PLAY0668'],
        ['  draft = 2147483648', 'PLAY0669'],
        ['  draft = -2147483649', 'PLAY0669'],
        ...['+1', '01', '-01', '1.0', '1e0', '--1', '', 'nope'].map(literal => [`  draft = ${literal}`, 'PLAY0670']),
    ])('should report %s with %s', (body, code) => {
        parse(`concept Status : Enum\n${body}`).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([code]);
    });
});

describe('when suggesting enum member numbers', () => {
    it('should hint only once for nested and inline stored values', () => {
        const source = 'concept Status : Enum\n  draft\ntype Detail\n  status Status[] optional\ntype Envelope\n  detail Detail\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        envelope Envelope\n      command C\n        produces event Changed\n          status Status = "draft"';
        const hints = parse(source).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0671');
        hints.should.have.lengthOf(1);
        hints[0].severity.should.equal('information');
        hints[0].message.should.contain('Number every member by its current position');
    });
    it.each([
        'concept Status : Enum\n  draft = 0' + event,
        'concept Status : Enum\n  draft = 0\n  issued' + event,
        'concept Status : Enum\n  draft = nope' + event,
        'concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        status Status',
        'concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateView S\n      readmodel Row\n        status Status',
    ])('should not hint for unstored, numbered or invalid enum %s', source => {
        parse(source).diagnostics.some(diagnostic => diagnostic.code === 'PLAY0671').should.equal(false);
    });
});

describe('when transporting enum member numbers', () => {
    it('should omit absent numbers without conflating them with empty arrays', () => {
        const syntax = parse('numbers exact\nconcept Status : Enum\n  draft').value;
        const wire = JSON.stringify(toSyntaxJson(syntax));
        wire.includes('valueNumbers').should.equal(false);
        const restored = decodeExactSyntaxJson(wire) as ApplicationSyntax;
        (restored.concepts[0].valueNumbers == null).should.equal(true);
        const restoredWire = JSON.stringify(toSyntaxJson(restored));
        restoredWire.includes('valueNumbers').should.equal(false);
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(restoredWire))).should.equal(restoredWire);
    });
    it('should roundtrip populated numbers through the strict schema', () => {
        const syntax = parse('numbers exact\nconcept Status : Enum\n  draft = 1\n  issued = 4').value;
        const wire = JSON.stringify(toSyntaxJson(syntax));
        wire.should.contain('"valueNumbers":[1,4]');
        const restored = decodeExactSyntaxJson(wire) as ApplicationSyntax;
        restored.concepts[0].valueNumbers!.should.deep.equal([1, 4]);
        const restoredWire = JSON.stringify(toSyntaxJson(restored));
        JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(restoredWire))).should.equal(restoredWire);
    });
    it.each([[1], [1, 1], [1, 2147483648], [1, 1.5]])('should refuse malformed number arrays %s', (...numbers) => {
        const syntax = parse('concept Status : Enum\n  draft\n  issued').value;
        const candidate = { ...syntax, concepts: [{ ...syntax.concepts[0], valueNumbers: numbers }] };
        (() => toSyntaxJson(candidate)).should.throw();
    });
    it('should refuse numbers on non-enums', () => {
        const syntax = parse('concept Status : String').value;
        const candidate = { ...syntax, concepts: [{ ...syntax.concepts[0], valueNumbers: [] }] };
        (() => toSyntaxJson(candidate)).should.throw();
    });
});
