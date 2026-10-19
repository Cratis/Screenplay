// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const declarations = ['concept Status : String', 'concept Note : String', 'module M', '  feature F', '    slice StateView S', '      readmodel Item', '        status Status', '        id Uuid', '      query Details => Item', '      command Retry', '        id Uuid', '      screen Details', '        data Item via query Details'];

describe('when validating interaction subject paths that continue past a concept', () => {
    it('should warn that the path continues past a scalar concept', () => {
        const result = parse([...declarations, '        on click', '          when item.status.missing == null', '            notify info "Missing"'].join('\n'), 'screen.play');
        result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.message]).should.deep.equal([['PLAY0345', "Subject field 'item.status.missing' continues past scalar 'status'"]]);
    });

    it('should accept an action input that stops at the concept', () => {
        const result = parse([...declarations, '        on click', '          when item.status == null', '            execute Retry', '              with id from item.id'].join('\n'), 'screen.play');
        result.diagnostics.filter(diagnostic => /^PLAY034[5-8]$/.test(diagnostic.code)).should.deep.equal([]);
    });

    it('should warn that an action input continues past a scalar concept', () => {
        const result = parse([...declarations, '        on click', '          when item.id == null', '            execute Retry', '              with id from item.status.missing'].join('\n'), 'screen.play');
        result.diagnostics.map(diagnostic => diagnostic.code).should.include('PLAY0345');
    });
});
