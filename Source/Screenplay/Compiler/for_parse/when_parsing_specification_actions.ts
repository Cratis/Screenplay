// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { SpecificationSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax } from '../Syntax/Structure';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing specification actions', () => {
    let result: CompilationResult<ApplicationSyntax>;
    const specification = (index: number): SpecificationSyntax => result.value.modules[0].features[0].slices[0].specifications[index];

    beforeEach(() => {
        result = a_parsed_document(
            'module Library',
            '  feature Lending',
            '    slice StateView Hours',
            '      specification AtOpening',
            '        given clock "2026-10-05T08:00:00Z"',
            '        given capture LegacyLoans',
            '          title = "Dune"',
            '        when clock "2026-10-05T09:30:00.5-05:00"',
            '      specification Triggered',
            '        when trigger NightlySync',
            '          batch = 3',
            '      specification Captured',
            '        when capture LegacyLoans',
            '          title = "Emma"',
            '      specification Performing',
            '        when query Library.OpeningHoursFor',
            '          day = "Monday"',
            '        then result exactly',
            '          opens = "08:00"',
            '        then result',
            '      specification Empty',
            '        when query OpeningHoursFor',
            '        then no result',
        );
    });

    it('should succeed', () => {
        result.diagnostics.should.be.empty;
    });

    it('should read the clocks', () => {
        [specification(0).givenClock!.instant, specification(0).whenClock!.instant].should.deep.equal(['2026-10-05T08:00:00Z', '2026-10-05T09:30:00.5-05:00']);
    });

    it('should read the earlier capture records', () => {
        specification(0).givenCaptures.map(capture => `${capture.capture}:${capture.record.map(field => field.property)}`).should.deep.equal(['LegacyLoans:title']);
    });

    it('should read the trigger with its values', () => {
        (toSyntaxJson(specification(1).whenTrigger!) as object).should.deep.equal({
            kind: 'SpecificationTriggerSyntax',
            trigger: 'NightlySync',
            values: [{ kind: 'PropertyMappingSyntax', property: 'batch', source: toSyntaxJson(specification(1).whenTrigger!.values[0].source) }],
        });
    });

    it('should read the capture record', () => {
        specification(2).whenCapture!.capture.should.equal('LegacyLoans');
    });

    it('should read the query performed with its arguments', () => {
        [specification(3).whenQuery!.query, specification(3).whenQuery!.arguments.map(argument => argument.property)].should.deep.equal(['Library.OpeningHoursFor', ['day']]);
    });

    it('should read the results in order, exactly or not', () => {
        specification(3).thenResults.map(each => `${each.exactly}:${each.properties.length}`).should.deep.equal(['true:1', 'false:0']);
    });

    it('should read that nothing is returned', () => {
        [specification(4).thenNoResult !== null, specification(3).thenNoResult === null].should.deep.equal([true, true]);
    });
});
