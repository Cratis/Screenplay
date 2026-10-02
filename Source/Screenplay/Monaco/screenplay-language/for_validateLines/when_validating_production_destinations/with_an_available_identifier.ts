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

    it.each(['Uuid?', 'Uuid[]', 'Uuid[]?'])('should not advise for a %s identifier', (type) => {
        destinations(source.map(line => line.replace('projectId Uuid identifier', `projectId ${type} identifier`))).should.have.lengthOf(0);
    });

    it.each([
        ['command Register // explanation', 'projectId Uuid identifier'],
        ['command Register', 'projectId Uuid identifier // explanation'],
        ['command Register // explanation', 'projectId Uuid identifier // explanation'],
    ])('should discover identifiers despite trailing comments: %s; %s', (command, property) => {
        destinations(source.map(line => line.replace('command Register', command).replace('projectId Uuid identifier', property)))
            .map(issue => issue.line).should.deep.equal([5]);
    });

    it('should not discover an identifier inside a fence', () => {
        destinations([
            ...source.slice(0, 4),
            '        produces ProjectRegistered',
            '        validate',
            '          ```csharp',
            '          ghostId Uuid identifier',
            '          ```',
            '      event ProjectRegistered',
        ]).should.have.lengthOf(0);
    });

    it('should not count fenced identifiers beside a real identifier', () => {
        destinations([
            ...source.slice(0, 5),
            '        produces ProjectRegistered',
            '        validate',
            '          ```csharp',
            '          ghostId Uuid identifier',
            '          ```',
            '      event ProjectRegistered',
        ]).map(issue => issue.line).should.deep.equal([5]);
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
