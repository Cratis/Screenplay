// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseFolder } from '../PlayApplicationAssembly';

describe('when fragments declare dependencies', () => {
    const result = parseFolder([
        { path: 'a.play', source: 'module Payroll\n  depends on Timesheets\n  feature Handover\n    depends on Runs\n    depends on Runs\n  feature Runs\n' },
        { path: 'b.play', source: 'module Payroll\n  depends on Timesheets\n  depends on Engagements\n  feature Handover\n    depends on Payroll.Runs\n    depends on Timesheets.Approval\nmodule Timesheets\n  feature Approval\nmodule Engagements\n' },
    ]);
    it('should accumulate module targets in path order', () => result.value.modules[0].dependsOn!.map(dependency => dependency.target).should.deep.equal(['Timesheets', 'Engagements']));
    it('should deduplicate resolved aliases', () => result.value.modules[0].features[0].dependsOn!.map(dependency => dependency.target).should.deep.equal(['Runs', 'Timesheets.Approval']));
    it('should warn on each repeat', () => result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0555').map(diagnostic => `${diagnostic.code}@${diagnostic.location.path}:${diagnostic.location.line}`).should.deep.equal(['PLAY0555@a.play:5', 'PLAY0555@b.play:2', 'PLAY0555@b.play:5']));
    it('should report repeats as warnings', () => result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0555').every(diagnostic => diagnostic.severity === 'warning').should.be.true);
});
