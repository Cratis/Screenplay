// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';
import { validateSpecificationStreams } from '../SpecificationStreamValidator';

for (const keyword of ['given', 'when append']) {
    describe(`when validating typed no stream on ${keyword}`, () => {
        let context: ParserContext;
        beforeEach(() => {
            const application = parse(`module M\n  feature F\n    slice StateView S\n      event E\n      specification X\n        ${keyword} E`).value!;
            const specification = application.modules[0].features[0].slices[0].specifications[0];
            const occurrence = keyword === 'given' ? specification.given[0] : specification.whenAppended!;
            Object.assign(occurrence, { noStream: { kind: 'SpecificationNoStreamSyntax', location: occurrence.location } });
            context = new ParserContext(new LineReader([]));
            validateSpecificationStreams(application, context);
        });
        it('should refuse an unparseable history marker', () => context.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0547']));
    });
}
