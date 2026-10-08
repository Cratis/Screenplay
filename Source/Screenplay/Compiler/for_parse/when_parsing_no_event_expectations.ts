// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { a_parsed_document } from './given/a_parsed_document';

const parse = (...steps: string[]) => a_parsed_document('module Work', '  feature Recording', '    slice StateChange Nothing', '      specification NothingHappens', ...steps);

describe('when parsing no event expectations', () => {
    it('should retain an explicit assertion without inventing an event type', () => {
        const result = parse('        when DoNothing', '        then no events');
        result.diagnostics.should.be.empty;
        const specification = result.value.modules[0].features[0].slices[0].specifications[0];
        (specification.thenNoEvents === true).should.be.true;
        specification.thenEvents.should.be.empty;
        expect(toSyntaxJson(specification)).toHaveProperty('thenNoEvents', true);
    });

    it('should omit a false assertion from syntax JSON to preserve existing bytes', () => {
        const specification = parse('        when DoNothing').value.modules[0].features[0].slices[0].specifications[0];
        expect(toSyntaxJson(specification)).not.toHaveProperty('thenNoEvents');
    });

    for (const steps of [
        ['when append Happened', 'then no events'],
        ['when DoNothing', 'then no events', 'then Happened'],
        ['when DoNothing', 'then Happened', 'then no events'],
        ['when DoNothing', 'then no events', 'then events in any order'],
        ['when DoNothing', 'then no events', 'then error'],
        ['when DoNothing', 'then denied', 'then no events'],
        ['when DoNothing', 'then no events', 'then no events'],
        ['when DoNothing', 'then no events exactly'],
        ['when DoNothing', 'then no events', '  value = 1'],
    ]) {
        it(`should reject ${steps.join(' / ')}`, () => {
            parse(...steps.map(step => `        ${step}`)).diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidNoEventsExpectation).should.be.true;
        });
    }
});
