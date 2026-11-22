// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../../ScreenplayCompiler';
import { personaDiagnostics } from '../../PersonaCompleteness';

describe('when evaluating an inherited screen gate without bindings', () => {
    let diagnostics: ReturnType<typeof personaDiagnostics>;

    beforeEach(() => {
        const compilation = parse('policy Member\n  require role "A"\npersona Person\n  policy Member\nmodule M\n  authorize Member\n  feature F\n    slice StateChange S\n      screen Empty');
        diagnostics = personaDiagnostics(compilation.value);
    });

    it('should count the screen gate as policy use without reporting denial', () => diagnostics.should.deep.equal([]));
});
