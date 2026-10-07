// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { timelineOrderDiagnostics } from '../../Files/TimelineOrder';
import { DependencyGraph } from '../DependencyGraph';

for (const sample of ['Library', 'Invoicing', 'Commerce', 'TimeTracking']) {
    describe(`when matching timeline groups in ${sample}`, () => {
        let groups: string[][];
        let timelineGroups: string[][];
        beforeEach(() => {
            const directory = resolve(__dirname, '../../../../..', 'Samples', sample);
            const files = new Map(readdirSync(directory, { recursive: true, encoding: 'utf8' }).filter(path => path.endsWith('.play')).sort().map(path => [path, readFileSync(join(directory, path), 'utf8')]));
            const application = compileApplication(files).value;
            groups = DependencyGraph.for(application).siblingGroups(['usesFactsFrom', 'reactsTo']).map(group => group.members.map(node => node.scope.at(-1)!));
            timelineGroups = timelineOrderDiagnostics(application).filter(finding => finding.code === DiagnosticCodes.TimelineCycleGroup).map(finding => [...finding.message.matchAll(/'([^']+)'/g)].map(match => match[1]));
        });
        it('should keep sibling groups equal to timeline PLAY0517 groups', () => {
            groups.should.deep.equal(timelineGroups);
        });
    });
}
