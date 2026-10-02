// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { diagnosticCodes } from '../../diagnostic-codes';
import { ValidationIssue, validateLines } from '../../validation';

const source = [
    'module Projects',
    '  feature Registration',
    '    slice StateChange Register',
    '      command Register',
    '        projectId Uuid identifier',
    '        produces ProjectRegistered // omitted destination',
    '          name = name',
    '        produces ProjectUpdated',
    '          for projectId // explicit destination',
    '        produces when true == true',
    '          ProjectConditional',
    '      event ProjectRegistered',
    '      event ProjectUpdated',
    '      event ProjectConditional',
];

const destinations = (lines: string[]) => validateLines(lines).filter(issue => issue.code === diagnosticCodes.omittedProductionDestination);

describe('when validating production destinations with an available identifier', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = destinations(source);
    });

    it('should advise only on the plain omitted destination', () => {
        issues.map(issue => issue.line).should.deep.equal([5]);
    });

    it('should keep the advice informational', () => {
        issues[0].severity.should.equal('information');
        issues[0].message.should.contain('for projectId');
    });

    it('should not advise without an identifier', () => {
        destinations(source.map(line => line.replace(' identifier', ''))).should.have.lengthOf(0);
    });

    it('should not advise on reaction productions', () => {
        destinations(source.map(line => line.replace('command Register', 'reaction Register'))).should.have.lengthOf(0);
    });

    it('should ignore fenced code that looks like a production', () => {
        destinations([
            ...source.slice(0, 5),
            '        handler',
            '          ```csharp',
            '        produces NotScreenplay',
            '          ```',
        ]).should.have.lengthOf(0);
    });
});
