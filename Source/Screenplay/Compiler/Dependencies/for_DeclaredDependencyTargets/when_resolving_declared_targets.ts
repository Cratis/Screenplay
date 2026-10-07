// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DeclaredDependencyTargets, DependencyContainer } from '../DeclaredDependencyTargets';

const containers: DependencyContainer[] = [
    { name: 'Payroll', scope: [] }, { name: 'Runs', scope: [] }, { name: 'Timesheets', scope: [] },
    { name: 'Handover', scope: ['Payroll'] }, { name: 'Runs', scope: ['Payroll'] },
    { name: 'Approval', scope: ['Timesheets'] }, { name: 'Nested', scope: ['Payroll', 'Handover'] },
    { name: 'Cousin', scope: ['Payroll', 'Runs'] },
];

describe('when resolving declared targets', () => {
    it.each([
        ['Runs', 'Payroll.Handover', 'Payroll.Runs'],
        ['Runs', 'Payroll.Handover.Nested', 'Payroll.Runs'],
        ['Timesheets', 'Payroll.Handover', 'Timesheets'],
        ['Timesheets.Approval', 'Payroll.Handover', 'Timesheets.Approval'],
        ['Handover.Nested', 'Timesheets.Approval', 'Payroll.Handover.Nested'],
        ['Approval', 'Payroll.Handover', ''],
        ['Cousin', 'Payroll.Handover.Nested', ''],
        ['Nowhere', 'Payroll.Handover', ''],
    ])('should resolve %s from %s to %s without cousins', (target, owner, expected) => {
        const result = DeclaredDependencyTargets.resolve(target, owner.split('.'), containers);
        (result.resolved === undefined ? '' : [...result.resolved.scope, result.resolved.name].join('.')).should.equal(expected);
    });
    it('should not choose an equally near dotted match', () => {
        const result = DeclaredDependencyTargets.resolve('Group.Shared', ['Payroll'], [{ name: 'Shared', scope: ['A', 'Group'] }, { name: 'Shared', scope: ['B', 'Group'] }]);
        result.ambiguous.should.have.lengthOf(2);
        Object.hasOwn(result, 'resolved').should.be.false;
    });
});
