// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { DependencyGraph } from '@cratis/screenplay-compiler';
import { boardIdOf } from '../boardIdOf';
import { dependencyOverlayFor } from '../dependencyOverlayFor';
import type { DependencyOverlay } from '../DependencyOverlay';
import { sampleApplication } from '../for_DependencyMap/given/a_sample';

describe('when mapping TimeTracking overlays', () => {
    const application = sampleApplication('TimeTracking');
    const graph = DependencyGraph.for(application);
    const idsIn = (module: string) => graph.nodes.filter(node => node.kind === 'slice' && node.scope[0] === module).map(node => boardIdOf(node)!);
    let overlay: DependencyOverlay;
    beforeAll(() => { overlay = dependencyOverlayFor(application); });
    const payroll = () => overlay.scopes.find(scope => scope.id === boardIdOf(graph.nodes.find(node => node.kind === 'module' && node.scope[0] === 'Payroll')!))!;

    it('should have one scope per module feature and slice', () => overlay.scopes.map(scope => scope.id).sort().should.deep.equal(graph.nodes.filter(node => node.kind !== 'context' && node.kind !== 'application').map(node => boardIdOf(node)!).sort()));
    it('should only mention slices features and modules', () => { const known = new Set(overlay.scopes.map(scope => scope.id)); overlay.scopes.every(scope => [...scope.selected, ...scope.dependencies, ...scope.dependants, ...scope.ancestors, ...scope.links.flatMap(link => [link.from, link.to])].every(id => known.has(id))).should.be.true; });
    it('should depend from Payroll on slices of Timesheets', () => idsIn('Timesheets').some(id => payroll().dependencies.includes(id)).should.be.true);
    it('should depend from Payroll on slices of Engagements', () => idsIn('Engagements').some(id => payroll().dependencies.includes(id)).should.be.true);
    it('should select the slices of Payroll', () => idsIn('Payroll').every(id => payroll().selected.includes(id)).should.be.true);
    it('should draw a crossing link from Payroll', () => payroll().links.some(link => link.crossing && idsIn('Payroll').includes(link.from)).should.be.true);
    it('should not repeat links', () => new Set(payroll().links.map(link => `${link.from}>${link.to}`)).size.should.equal(payroll().links.length));
    it('should keep the module holding a dependency out of the dimmed', () => overlay.scopes.every(scope => scope.ancestors.every(id => !scope.selected.includes(id) && !scope.dependencies.includes(id) && !scope.dependants.includes(id))).should.be.true);
    it('should survive JSON transport', () => JSON.parse(JSON.stringify(overlay)).should.deep.equal(overlay));
});
