// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const compile = (...source: string[]) => parse(source.join('\n'));

describe('when validating projection targets', () => {
    it('should follow composite paths and reject a path below a scalar', () => {
        const result = compile(
            'type Detail', '  value String', 'module M', '  feature F', '    slice StateView S',
            '      event E', '        value String', '      readmodel V', '        detail Detail', '        value String',
            '      projection P => V', '        from E', '          detail.value = value', '          value.missing = value');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0514']);
    });

    it('should leave imported and ambiguous shapes unresolved', () => {
        const imported = compile('import Contracts.V', 'module M', '  feature F', '    slice StateView S',
            '      event E', '      projection P => V', '        from E', '          missing = "value"');
        imported.diagnostics.should.deep.equal([]);
        const ambiguous = compile('module M', '  feature F', '    slice StateView A', '      readmodel V', '        value String',
            '    slice StateView B', '      readmodel V', '        other String', '    slice StateView C',
            '      event E', '      projection P => V', '        from E', '          missing = "value"');
        ambiguous.diagnostics.should.deep.equal([]);
    });

    it('should resolve a qualified read model', () => {
        const result = compile('module M', '  feature F', '    slice StateView Owner', '      readmodel V', '        value String',
            '    slice StateView Consumer', '      event E', '        value String',
            '      projection P => Owner.V', '        from E', '          value = value');
        result.diagnostics.should.deep.equal([]);
    });

    it('should check shared element and variant-local mappings against each variant shape', () => {
        const result = compile('type Row', '  value String', 'module M', '  feature F', '    slice StateView S',
            '      event E', '        value String', '      readmodel Active', '        rows Row[]', '        value String',
            '      projection P', '        children rows identified by value', '          from E', '            value = value',
            '        variant Active', '          enters on E', '          from E', '            value = value');
        result.diagnostics.should.deep.equal([]);
    });

    it('should not guess an external element shape', () => {
        const result = compile('module M', '  feature F', '    slice StateView S', '      event E',
            '      readmodel V', '        detail External', '      projection P => V', '        nested detail',
            '          from E', '            missing = "value"');
        result.diagnostics.should.deep.equal([]);
    });
});
