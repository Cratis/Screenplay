// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const deprecations = (...lines: string[]) => parse(lines.join('\n') + '\n').diagnostics
    .filter(diagnostic => diagnostic.code === 'PLAY0397')
    .map(diagnostic => diagnostic.message);
const machine = (...entries: string[]) => ['statemachine L', '  follows A', '  state S initial', ...entries];
const workflow = (...lines: string[]) => ['workflow W', '  for R', ...lines];

describe('when reading the deprecated lifecycle verbs', () => {
    it('should warn for entering send and name entering invokes', () => expect(deprecations(...machine('    entering send X'))).toEqual(["'entering send' is deprecated - use 'entering invokes'."]));
    it('should warn for entering start and name entering starts', () => expect(deprecations(...machine('    entering start X'))).toEqual(["'entering start' is deprecated - use 'entering starts'."]));
    it('should not warn for entering invokes or entering starts', () => expect(deprecations(...machine('    entering invokes X', '    entering starts Y'))).toEqual([]));
    it('should keep the entry kinds across old and new forms', () => {
        const result = parse(machine('    entering send X', '    entering invokes Y', '    entering start Z', '    entering starts W').join('\n') + '\n').value.stateMachines![0];
        expect(result.states[0].entering.map(entry => entry.entryKind)).toEqual(['Send', 'Send', 'Start', 'Start']);
    });
    it('should warn for action sends', () => expect(deprecations(...workflow('  action A sends C'))).toEqual(["'action ... sends' is deprecated - use 'action <Name> invokes <Command>'."]));
    it('should not warn for action invokes', () => expect(deprecations(...workflow('  action A invokes C'))).toEqual([]));
    it('should warn for then sends', () => expect(deprecations(...workflow('  specification S', '    then sends C'))).toEqual(["'then sends' is deprecated - use 'then invokes <Command>'."]));
    it('should not warn for then invokes', () => expect(deprecations(...workflow('  specification S', '    then invokes C'))).toEqual([]));
});
