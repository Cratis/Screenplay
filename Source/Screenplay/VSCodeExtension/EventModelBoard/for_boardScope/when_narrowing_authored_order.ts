// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileEventModelApplication, toEventModelDocument } from '@cratis/screenplay-event-models';
import { narrowTo, scopeOf } from '../boardScope';

describe('when narrowing an authored-order board', () => {
    it('should still narrow to a file the application root does not import', () => {
        const files = [
            { path: 'application.play', source: 'import "known.play"' },
            { path: 'known.play', source: 'module Known\n  feature F\n    slice StateChange Known' },
            { path: 'scratch.play', source: 'module Scratch\n  feature F\n    slice StateChange Scratch' },
        ];
        const compilation = compileEventModelApplication(files);
        const scope = scopeOf('scratch.play', new Map(files.map(file => [file.path, file.source])), compilation);
        const narrowed = narrowTo(compilation.value, scope);
        (narrowed !== undefined).should.be.true;
        toEventModelDocument(narrowed!, 'Board').collections[0].modules[0].features[0].slices.map(slice => slice.name).should.deep.equal(['Scratch']);
    });

    it('should retain import order for the visible subset without changing identities', () => {
        const compilation = compileEventModelApplication([
            { path: 'application.play', source: 'module M\n  feature F\n    import "z.play"\n    import "hidden.play"\n    import "a.play"' },
            { path: 'a.play', source: 'slice StateChange A' },
            { path: 'z.play', source: 'slice StateChange Z' },
            { path: 'hidden.play', source: 'slice StateChange Hidden' },
        ]);
        const narrowed = narrowTo(compilation.value, { files: new Set(['a.play', 'z.play']), containers: [] })!;
        const whole = toEventModelDocument(compilation.value, 'Board').collections[0].modules[0].features[0].slices;
        const shown = toEventModelDocument(narrowed, 'Board').collections[0].modules[0].features[0].slices;
        shown.map(slice => slice.name).should.deep.equal(['Z', 'A']);
        shown.map(slice => slice.id).should.deep.equal(whole.filter(slice => slice.name !== 'Hidden').map(slice => slice.id));
    });
});
