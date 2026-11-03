// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { parseFolder } from '../PlayApplicationAssembly';

describe('when merging state machines and workflows from several files', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let plain: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parseFolder([
            { path: 'a.play', source: 'statemachine First\n  follows Invoice\n  state Start initial\n' },
            { path: 'b.play', source: 'workflow Chase\n  for Invoice\n  trigger manual\n' },
            { path: 'c.play', source: 'statemachine Second\n  follows Invoice\n  state Start initial\n' },
        ]);
        plain = parseFolder([{ path: 'a.play', source: 'module M\n  feature F\n    slice StateChange S' }]);
    });

    it('should keep the state machines of every file in file order', () => {
        result.value.stateMachines!.map(machine => machine.name).should.deep.equal(['First', 'Second']);
    });

    it('should keep the workflows of every file', () => {
        result.value.workflows!.map(workflow => workflow.name).should.deep.equal(['Chase']);
    });

    it('should count a file holding only a state machine as a declaration', () => {
        parseFolder([{ path: 'only.play', source: 'statemachine Only\n  follows Invoice\n  state Start initial\n' }]).value.stateMachines!.length.should.equal(1);
    });

    it('should omit both collections when no file declares one', () => {
        [plain.value.stateMachines, plain.value.workflows].should.deep.equal([undefined, undefined]);
    });
});
