// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { beforeAll, describe, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax, SpecificationSyntax } from '../Syntax';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

describe('when parsing producerless event source fixtures', () => {
    let source: string;
    let result: CompilationResult<ApplicationSyntax>;
    let specification: SpecificationSyntax;

    const caseSource = (type: string) => source
        .replace('specification CountingSlots', `specification CountingSlots\n        parameter day ${type}\n        case First day = "2026-11-10"`)
        .replaceAll('for "2026-11-10"', 'for case.day');

    beforeAll(() => {
        source = readFileSync(resolve(__dirname, '../Conformance/producerless-event-source.play'), 'utf8');
        result = parse(source);
        specification = result.value!.modules[0].features[0].slices[0].specifications[0];
    });

    it('should accept the shared producerless projection fixture', () => result.success.should.be.true);
    it('should retain both explicit source identities', () => {
        specification.given.map(event => [event.eventType, event.for!.kind]).should.deep.equal([
            ['DayOpened', 'LiteralExpressionSyntax'], ['SlotTaken', 'LiteralExpressionSyntax']
        ]);
    });
    it('should not invent a producing command', () => {
        result.value!.modules[0].features[0].slices[0].commands.should.have.lengthOf(0);
    });
    it('should retain an appended event identity', () => {
        const appended = parse(source.replace('given SlotTaken', 'when append SlotTaken'));
        appended.success.should.be.true;
        appended.value!.modules[0].features[0].slices[0].specifications[0].whenAppended!.for!.kind.should.equal('LiteralExpressionSyntax');
    });
    it('should accept a case parameter as the producerless event source', () => {
        parse(caseSource('String')).success.should.be.true;
    });
    it('should refuse an optional case parameter for a required projected identity', () => {
        const refused = parse(caseSource('String?'));
        refused.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.OptionalSpecificationParameterTarget);
    });
    it('should refuse an incompatible case parameter for a projected identity', () => {
        const refused = parse(caseSource('Date'));
        refused.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.IncompatibleSpecificationParameterType);
    });
    it('should check the projected identity type when a case overrides an event example', () => {
        const example = caseSource('String?')
            .replace('      specification CountingSlots', '      example SavedSlot : SlotTaken\n        for "2026-11-10"\n        dayId = "2026-11-10"\n      specification CountingSlots')
            .replace('given SlotTaken', 'given SavedSlot');
        const refused = parse(example);
        refused.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.OptionalSpecificationParameterTarget);
    });
    it('should still refuse an empty identity assertion', () => {
        const refused = parse(source.replaceAll('for "2026-11-10"', 'for'));
        refused.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.InvalidSpecificationEventSource);
    });
});
