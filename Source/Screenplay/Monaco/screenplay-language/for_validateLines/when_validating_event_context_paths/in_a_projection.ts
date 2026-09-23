// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { diagnosticCodes } from '../../diagnostic-codes';
import { eventContextPaths } from '../../event-context';
import { ValidationIssue, validateLines } from '../../validation';

function validate(...mappings: string[]): ValidationIssue[] {
    return validateLines([
        'projection Statistics => StatisticsReadModel',
        '  from Happened',
        ...mappings.map((mapping) => `    ${mapping}`),
    ]);
}

describe('when validating every event context path the catalog lists', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validate(
            ...eventContextPaths.map((path, index) => `value${index} = $eventContext.${path.path}`),
            ...eventContextPaths.map((path) => `count countBy.$eventContext.${path.path}`),
            'week = $eventContext.occurred.Week()',
            'type = $eventContext.EventType.Id',
        );
    });

    it('should report nothing', () => {
        issues.should.be.empty;
    });
});

describe('when validating an unknown event context member', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validate('cause = $eventContext.causationId');
    });

    it('should warn with the compiler code', () => {
        issues.map((issue) => [issue.code, issue.severity]).should.deep.equal([[diagnosticCodes.unknownEventContextMember, 'warning']]);
    });

    it('should squiggle the whole path', () => {
        const [issue] = issues;
        [issue.startColumn, issue.endColumn].should.deep.equal([13, 13 + '$eventContext.causationId'.length]);
    });
});

describe('when validating an unknown event context sub-path', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validate('week = $eventContext.occurred.week');
    });

    it('should warn with the compiler code', () => {
        issues.map((issue) => [issue.code, issue.severity]).should.deep.equal([[diagnosticCodes.unknownEventContextPath, 'warning']]);
    });
});

describe('when validating a path below an event context collection', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validate('count byTag.$eventContext.tags.value');
    });

    it('should report an error with the compiler code', () => {
        issues.map((issue) => [issue.code, issue.severity]).should.deep.equal([[diagnosticCodes.eventContextPathBelowCollection, 'error']]);
    });
});

describe('when validating a dynamic key naming no event context member', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validate('count byNothing.$eventContext');
    });

    it('should report an error with the compiler code', () => {
        issues.map((issue) => [issue.code, issue.severity]).should.deep.equal([[diagnosticCodes.missingEventContextPath, 'error']]);
    });
});

describe('when validating a dynamic key naming another source', () => {
    let issues: ValidationIssue[];

    beforeEach(() => {
        issues = validate('count byUser.$causedBy.subject');
    });

    it('should warn with the compiler code', () => {
        issues.map((issue) => [issue.code, issue.severity]).should.deep.equal([[diagnosticCodes.unresolvedDynamicKeySource, 'warning']]);
    });
});
