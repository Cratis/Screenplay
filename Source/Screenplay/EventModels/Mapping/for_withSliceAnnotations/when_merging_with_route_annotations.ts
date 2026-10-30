// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { EventModelDocument, SliceAnnotationDocument } from '../../Document/EventModelDocument';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { withSliceAnnotations } from '../withSliceAnnotations';

const source = 'module M\n  feature F\n    slice StateChange Plain\n    feature Child\n      slice StateChange Routed\n        command C\n        event E\n        specification Routed\n          when C\n          then E\n            stream A.S';
const extra: SliceAnnotationDocument = { severity: 'information', code: 'PLAY0516', message: 'Later producer' };

describe('when merging with route annotations', () => {
    let original: EventModelDocument;
    let baseline: EventModelDocument;
    let result: EventModelDocument;
    let routes: SliceAnnotationDocument[];

    beforeEach(() => {
        original = toEventModelDocument(parse(source).value, 'Work');
        baseline = structuredClone(original);
        const routed = original.collections[0].modules[0].features[0].subFeatures[0].slices[0];
        routes = routed.annotations!;
        result = withSliceAnnotations(original, new Map([[routed.id, [extra]], ['unknown', [extra]]]));
    });

    it('should preserve route annotations first and append the finding in a sub feature', () => {
        routes.should.not.be.empty;
        result.collections[0].modules[0].features[0].subFeatures[0].slices[0].annotations!.should.deep.equal([...routes, extra]);
    });

    it('should leave the input unchanged', () => {
        result.should.not.equal(original);
        original.should.deep.equal(baseline);
    });

    it('should leave annotations absent on a slice without findings', () => {
        (result.collections[0].modules[0].features[0].slices[0].annotations === undefined).should.be.true;
    });

    it('should ignore an unknown slice identity', () => {
        withSliceAnnotations(original, new Map([['unknown', [extra]]])).should.deep.equal(original);
    });
});
