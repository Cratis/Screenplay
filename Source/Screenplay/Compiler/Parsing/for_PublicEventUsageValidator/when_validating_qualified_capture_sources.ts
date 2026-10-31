// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';

const source = (name: string): string => `concept ProductId : Uuid
module Products
  feature Catalog
    slice Translate Facts
      direction inbound
      event ProductPublished from "catalog"
        productId ProductId
      event ProductReceived
        productId ProductId
      capture ProductsFeed
        source events
          from ${name}
        key productId
        append ProductReceived
          productId = $.productId`;

for (const name of ['Wrong.ProductPublished', 'Domain.Model.Products.Catalog.Facts.ProductPublished', 'Products.ProductPublished']) {
    describe(`when validating unresolved qualified capture source ${name}`, () => {
        let result: ReturnType<typeof parse>;
        beforeEach(() => { result = parse(source(name), 'capture.play'); });

        it('should fail compilation', () => result.success.should.equal(false));
        it('should report exactly one unknown event error at the from setting', () => {
            result.diagnostics.should.deep.equal([{
                code: DiagnosticCodes.UnknownEvent,
                severity: 'error',
                arguments: {},
                message: `Unknown event '${name}' - declare its contract before classifying its usage.`,
                location: { line: 12, column: 11, path: 'capture.play' },
            }]);
        });
    });
}

for (const [name, text] of [
    ['full path', source('Products.Catalog.Facts.ProductPublished')],
    ['feature suffix', source('Catalog.Facts.ProductPublished')],
    ['slice suffix', source('Facts.ProductPublished')],
    ['bare name', source('ProductPublished')],
    ['local declaration import', 'import Products.Catalog.Facts.ProductPublished\n' + source('ProductPublished')],
    ['nested feature', source('Catalog.Nested.Facts.ProductPublished').replaceAll('\n    ', '\n      ').replace('  feature Catalog', '  feature Catalog\n    feature Nested')],
    ['repeated generations', source('Facts.ProductPublished').replace('      event ProductPublished from', '      event ProductPublished generation 1 from "catalog"\n        productId ProductId\n      event ProductPublished generation 2 from')],
    ['explicit external import', 'import Outside.ProductPublished from "catalog"\n' + source('Outside.ProductPublished')],
    ['opaque legacy import', 'import Outside.ProductPublished\n' + source('Outside.ProductPublished')],
]) {
    describe(`when validating a capture source resolved through ${name}`, () => {
        let result: ReturnType<typeof parse>;
        beforeEach(() => { result = parse(text); });
        it('should compile successfully', () => result.success.should.equal(true));
        it('should have no diagnostics', () => {
            result.diagnostics.should.be.empty;
        });
    });
}

for (const [name, text, code, severity] of [
    ['unknown bare input', source('Missing'), DiagnosticCodes.UnknownEvent, 'warning'],
    ['unrelated qualified output', source('ProductPublished') + '\n      reaction Receive\n        when ProductPublished\n          produces Wrong.ProductReceived', DiagnosticCodes.UnknownEvent, 'warning'],
    ['wrong qualifier despite real import', 'import Products.Catalog.Facts.ProductPublished\n' + source('Wrong.ProductPublished'), DiagnosticCodes.UnknownEvent, 'error'],
    ['ambiguous imports', 'import One.ProductPublished from "one"\nimport Two.ProductPublished from "two"\n' + source('ProductPublished').replace('      event ProductPublished from "catalog"\n        productId ProductId\n', ''), DiagnosticCodes.AmbiguousReference, 'warning'],
]) {
    describe(`when preserving capture source resolution for ${name}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;
        beforeEach(() => { diagnostics = publicEventDiagnostics(parse(text).value); });
        it('should report one diagnostic with the existing classification', () => {
            diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity]).should.deep.equal([[code, severity]]);
        });
    });
}
