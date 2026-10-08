// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

describe('when declaring dependencies', () => {
    const result = parse('module Payroll\n  depends on Timesheets\n  feature Handover\n    depends on Timesheets.Approval\n    depends on Runs\n  feature Runs\nmodule Timesheets\n  feature Approval\n');

    it('should accept module and feature targets', () => result.diagnostics.filter(diagnostic => diagnostic.severity !== 'information').should.deep.equal([]));
    it('should report unused declarations as information', () => result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0553', 'PLAY0553', 'PLAY0553']));
    it('should keep the authored targets', () => JSON.stringify(toSyntaxJson(result.value)).should.contain('"dependsOn":[{"kind":"DependsOnSyntax","target":"Timesheets"}]'));
});
