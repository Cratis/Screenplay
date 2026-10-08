// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DeclaredDependencyTargets, DependencyContainer } from '../DeclaredDependencyTargets';

const containers: DependencyContainer[] = [
    { name: 'Payroll', scope: [] },
    { name: 'Handover', scope: ['Payroll'] },
    { name: 'Nested', scope: ['Payroll', 'Handover'] },
    { name: 'Other', scope: ['Payroll', 'Handover'] },
    { name: 'Runs', scope: ['Payroll'] },
    { name: 'Timesheets', scope: [] },
    { name: 'Approval', scope: ['Timesheets'] },
    { name: 'Reporting', scope: ['Timesheets'] },
];
const candidates = (owner: string, targets = containers) => DeclaredDependencyTargets.candidates(owner.split('.'), targets);

describe('when enumerating candidate dependency targets', () => {
    it('should offer sibling features at the nearest tier', () => {
        expect(candidates('Payroll.Handover.Nested').find(candidate => candidate.reference === 'Other')?.tier).toBe(0);
    });
    it('should offer the enclosing feature siblings at the next tier', () => {
        expect(candidates('Payroll.Handover.Nested').find(candidate => candidate.reference === 'Runs')?.tier).toBe(1);
    });
    it('should offer root modules after features', () => {
        expect(candidates('Payroll.Handover').find(candidate => candidate.reference === 'Timesheets')?.tier).toBe(1);
    });
    it('should qualify features in other modules', () => {
        expect(candidates('Payroll.Handover').find(candidate => candidate.reference === 'Timesheets.Approval')?.tier).toBe('qualified');
    });
    it('should exclude self ancestors and descendants', () => {
        expect(candidates('Payroll.Handover').map(candidate => candidate.reference)).toEqual(['Runs', 'Timesheets', 'Timesheets.Approval', 'Timesheets.Reporting']);
    });
    it('should omit a root module shadowed by a sibling feature', () => {
        const targets = [...containers, { name: 'Runs', scope: [] }];
        expect(candidates('Payroll.Handover', targets).filter(candidate => candidate.container.name === 'Runs')).toHaveLength(1);
    });
    it('should lengthen an ambiguous qualification', () => {
        const targets = [...containers, { name: 'Shared', scope: ['A', 'Group'] }, { name: 'Shared', scope: ['B', 'Group'] }];
        expect(candidates('Payroll.Handover', targets).map(candidate => candidate.reference)).toContain('A.Group.Shared');
        expect(candidates('Payroll.Handover', targets).map(candidate => candidate.reference)).not.toContain('Group.Shared');
    });
});
