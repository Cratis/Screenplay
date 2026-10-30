// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { SliceAnnotationDocument } from '../../Document/EventModelDocument';
import { SliceScope } from '../SliceScope';
import { timelineAnnotations } from '../timelineAnnotations';

const compilation = parse('module M\n  feature A\n    slice StateView ViewA\n      event EA\n      projection PA\n        from EB\n  feature B\n    slice StateView ViewB\n      event EB\n      projection PB\n        from EA');

describe('when mapping a cycle group', () => {
    let annotations: ReadonlyMap<string, SliceAnnotationDocument[]>;

    beforeEach(() => {
        annotations = timelineAnnotations(compilation.value, compilation.diagnostics);
    });

    it('should mark the earliest backward consumer once for the group', () => {
        [...annotations.keys()].should.deep.equal([SliceScope.module('M').feature('A').slice('ViewA').id]);
        [...annotations.values()].flat().should.deep.equal([{
            severity: 'information', code: 'PLAY0517', message: compilation.diagnostics[0].message,
        }]);
    });

    it('should not mark internal edges separately', () => {
        [...annotations.values()].flat().filter(annotation => annotation.code === 'PLAY0516').should.be.empty;
    });
});
