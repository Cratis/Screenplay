// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { dependencyMapFor } from '../dependencyMapFor';
import { layoutDependencyMap } from '../layoutDependencyMap';
import { sampleApplication } from '../for_DependencyMap/given/a_sample';

describe('when laying out dependency layers', () => {
    const map = dependencyMapFor(sampleApplication('TimeTracking'));
    const layout = layoutDependencyMap(map);
    it('should put producers to the left', () => layout.nodes.find(node => node.key === 'module:Engagements')!.x.should.be.lessThan(layout.nodes.find(node => node.key === 'module:Timesheets')!.x));
    it('should be deterministic', () => layoutDependencyMap(JSON.parse(JSON.stringify(map))).should.deep.equal(layout));
    it('should not overlap nodes', () => {
        layout.nodes.some((left, index) => layout.nodes.slice(index + 1).some(right => left.x < right.x + right.width && right.x < left.x + left.width && left.y < right.y + right.height && right.y < left.y + left.height)).should.be.false;
    });
    it('should put contexts beyond every module', () => {
        const invoicing = layoutDependencyMap(dependencyMapFor(sampleApplication('Invoicing')));
        const modules = invoicing.nodes.filter(node => node.kind === 'module');
        invoicing.nodes.filter(node => node.kind === 'context').every(node => modules.every(module => module.x < node.x)).should.be.true;
    });
});
