// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';

const source = readFileSync(new URL('../../../Compiler/Conformance/specification-streams.play', import.meta.url), 'utf8').replace('numbers exact\n', '');

describe('when mapping specification routes', () => {
    it('should show authored routing separately from payload values on every event role', () => {
        const document = toEventModelDocument(parse(source).value, 'Banking');
        const specification = document.collections[0].modules[0].features[0].slices[0].specifications[0];
        specification.given[0].name.should.contain('stream Account.Transactions');
        specification.given[0].name.should.contain('streamId = "p-1:2026-10"');
        specification.given[0].name.should.contain('for "other"');
        specification.given[0].values.should.deep.equal({ stream: 5 });
        specification.when!.name.should.contain('stream Account.Profile');
        specification.when!.values.should.deep.equal({ streamId: 6 });
        specification.thenEvents[0].name.should.contain('stream Account.Partitioned');
        specification.thenEvents[0].name.should.contain('streamId = 202610');
        specification.thenEvents[1].name.should.contain('no stream');
        specification.thenEvents[2].name.should.equal('Recorded');
        specification.given[0].name.should.contain('PLAY0268');
    });
    it('should display opaque text ids as safe text rather than fabricated payload properties', () => {
        const document = toEventModelDocument(parse(source.replace('p-1:2026-10', '<month & year>')).value, 'Banking');
        const event = document.collections[0].modules[0].features[0].slices[0].specifications[0].given[0];
        event.name.should.contain('&lt;month &amp; year&gt;');
        event.values.should.deep.equal({ stream: 5 });
    });
});
