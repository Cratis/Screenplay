// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { expandSpecificationExamples } from '../SpecificationCommandExamples';
import { ApplicationSyntax } from '../../Syntax/Structure';

const vectors = JSON.parse(readFileSync(join(__dirname, '../../Conformance/diagnostics.json'), 'utf8')) as { cases: { name: string; source: string[] }[] };
const vector = vectors.cases.find(vector => vector.name === 'Routed given and append inherit an example destination and producer type')!;

describe('when validating example backed routes', () => {
    let application: ApplicationSyntax;
    beforeEach(() => { application = parse(vector.source.join('\n')).value; });
    it('should accept the inherited destination and producer fallback', () => {
        parse(vector.source.join('\n')).diagnostics.should.deep.equal([]);
    });
    it('should preserve the authored example reference and absent destination', () => {
        const step = application.modules[0].features[0].slices[0].specifications[0].given[0];
        step.eventType.should.equal('Prior');
        (step.for === null).should.equal(true);
    });
    it('should qualify the effective event and inherit its destination', () => {
        const step = expandSpecificationExamples(application).modules[0].features[0].slices[0].specifications[0].given[0];
        step.eventType.should.equal('M.F.S.Happened');
        step.for!.should.deep.equal(application.modules[0].features[0].slices[0].examples![0].for);
    });
});
