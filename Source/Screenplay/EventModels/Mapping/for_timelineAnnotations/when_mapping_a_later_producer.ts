// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { SliceAnnotationDocument } from '../../Document/EventModelDocument';
import { SliceScope } from '../SliceScope';
import { timelineAnnotations } from '../timelineAnnotations';

const compilation = parse('module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n    slice StateChange Write\n      event E', 'story.play');

describe('when mapping a later producer', () => {
    let annotations: ReadonlyMap<string, SliceAnnotationDocument[]>;

    beforeEach(() => {
        annotations = timelineAnnotations(compilation.value, compilation.diagnostics);
    });

    it('should annotate only the consumer slice', () => {
        [...annotations.keys()].should.deep.equal([SliceScope.module('M').feature('F').slice('View').id]);
    });

    it('should preserve the reported code and message as information', () => {
        [...annotations.values()].should.deep.equal([[{
            severity: 'information', code: 'PLAY0516', message: compilation.diagnostics[0].message,
        }]]);
    });
});
