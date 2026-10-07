// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { dependencyMapFor } from '../dependencyMapFor';
import type { DependencyMap } from '../DependencyMap';
import { sampleApplication } from './given/a_sample';

describe('when mapping TimeTracking dependencies', () => {
    let map: DependencyMap;
    beforeAll(() => { map = dependencyMapFor(sampleApplication('TimeTracking')); });
    it('should show Payroll depending on Timesheets', () => map.edges.some(edge => edge.source === 'module:Payroll' && edge.target === 'module:Timesheets').should.be.true);
    it('should show Timesheets depending on Engagements', () => map.edges.some(edge => edge.source === 'module:Timesheets' && edge.target === 'module:Engagements').should.be.true);
    it('should list all the slices behind Payroll to Timesheets', () => {
        const edge = map.edges.find(edge => edge.source === 'module:Payroll' && edge.target === 'module:Timesheets')!;
        edge.evidence.length.should.equal(edge.references);
        edge.evidence.map(id => map.evidence[id]).some(item => item.consumer.startsWith('Payroll.') && item.producer.startsWith('Timesheets.') && item.name.length > 0 && item.location.path!.endsWith('.play')).should.be.true;
    });
    it('should order modules Engagements Timesheets Payroll', () => map.modules.should.deep.equal(['module:Engagements', 'module:Timesheets', 'module:Payroll']));
    it('should survive JSON transport', () => JSON.parse(JSON.stringify(map)).should.deep.equal(map));
});
