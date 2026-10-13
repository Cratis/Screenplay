// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';
import { parsePlacedDocuments } from '../Files/PlayApplicationAssembly';

const commands = 'module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        returns\n          receipt = value\n      command D\n        value String\n        returns\n          receipt = value\n      screen Entry\n        on submit\n';

describe('when validating response continuations', () => {
    it.each(['notify error receipt', 'confirm receipt', 'set selected to receipt', 'navigate to Entry\n                with id from receipt'])('should reject %s only in a failure value operand', action => {
        const result = parse(commands + '          execute C\n            on failure\n              ' + action);
        expect(result.diagnostics).toMatchObject([{ code: 'PLAY0685', severity: 'error', message: "'receipt' is returned by 'C' only on success" }]);
    });
    it('should restore enclosing failure after nested success', () => {
        const result = parse(commands + '          execute C\n            on failure\n              execute D\n                on success\n                  notify info receipt\n              notify error receipt');
        expect(result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.location.line])).toEqual([['PLAY0685', 19]]);
    });
    it('should keep outer success available in inner failure', () => {
        expect(parse(commands + '          execute C\n            on success\n              execute D\n                on failure\n                  notify error receipt').diagnostics).toEqual([]);
    });
    it.each(['notify error "receipt"', 'confirm $strings.receipt', 'set receipt to item.receipt', 'navigate to Entry\n                with id from receipt + other'])('should keep opaque operand %s unchanged', action => {
        expect(parse(commands + '          execute C\n            on failure\n              ' + action).diagnostics).toEqual([]);
    });
    it('should resolve responses after placed documents are assembled', () => {
        const result = parsePlacedDocuments([
            { path: 'command.play', source: 'slice StateChange S\n  command C\n    value String\n    returns value', placement: ['M', 'F'] },
            { path: 'screen.play', source: 'slice StateView V\n  screen Entry\n    on submit\n      execute C\n        on failure\n          notify error value', placement: ['M', 'F'] },
        ]);
        expect(result.diagnostics).toMatchObject([{ code: 'PLAY0685', location: { path: 'screen.play', line: 6 } }]);
    });
    it('should skip fenced bodies and form descriptions', () => {
        const result = parseForAuthoring(commands + '          execute C\n            on success\n              notify info receipt\n  form Input for C\n    description\n      ```text\n      execute C\n        on failure\n          notify error receipt\n      ```');
        expect(result.diagnostics).toEqual([]);
        expect(result.continuationValues.some(use => use.failure.length > 0)).toBe(false);
    });
});
