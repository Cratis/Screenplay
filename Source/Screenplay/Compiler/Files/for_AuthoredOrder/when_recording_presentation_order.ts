// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes, parse, toSyntaxJson } from '../../index';
import { authoredOrderKey, authoredOrderOf, copyAuthoredOrder, recordAuthoredOrder } from '../AuthoredOrder';
import { compileApplication, parsePlacedDocuments } from '../PlayApplicationAssembly';

const rank = (application: ReturnType<typeof parse>['value'], ...scope: string[]) => authoredOrderOf(application).get(authoredOrderKey(scope));

describe('when recording presentation order', () => {
    it('should compute presentation ranks separately for ordinary application compilation', () => {
        const compilation = compileApplication(new Map([
            ['application.play', 'import "other.play"'],
            ['other.play', 'module M\n  feature F'],
        ]), ['application.play']);
        [...authoredOrderOf(compilation.value).keys()].should.deep.equal([['M'], ['M', 'F']].map(authoredOrderKey));
    });

    it('should leave merged syntax bytes, document order and duplicate diagnostics unchanged', () => {
        const compilation = compileApplication(new Map([
            ['application.play', 'import "z.play"\nimport "a.play"'],
            ['z.play', 'concept Shared : String\nmodule Z\n  feature F\n    slice StateChange S\n      event E'],
            ['a.play', 'concept Shared : Uuid\nmodule A\n  feature F\n    slice StateChange S\n      event E'],
        ]), ['application.play']);
        const baseline = parsePlacedDocuments(compilation.documents);
        recordAuthoredOrder(compilation.value, ['application.play'], compilation.documents);
        JSON.stringify(toSyntaxJson(compilation.value)).should.equal(JSON.stringify(toSyntaxJson(baseline.value)));
        compilation.diagnostics.should.deep.equal(baseline.diagnostics);
        compilation.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.RepeatedDeclarationAcrossFiles).should.be.true;
        compilation.documents.map(document => document.path).should.deep.equal(['application.play', 'z.play', 'a.play']);
        rank(compilation.value, 'Z')!.should.be.lessThan(rank(compilation.value, 'A')!);
    });

    it('should wait for the import at the deepest placement and visit a repeated import once', () => {
        const compilation = compileApplication(new Map([
            ['application.play', 'import "**/*.play"\nmodule M\n  feature F\n    slice StateChange First\n    import "steps/*.play"\n    import "steps/*.play"\n    slice StateChange Last'],
            ['steps/z.play', 'slice StateChange Z'],
            ['steps/a.play', 'slice StateChange A'],
        ]), ['application.play']);
        recordAuthoredOrder(compilation.value, ['application.play'], compilation.documents);
        const order = authoredOrderOf(compilation.value);
        [...order.keys()].should.deep.equal([['M'], ['M', 'F'], ['M', 'F', 'First'], ['M', 'F', 'A'], ['M', 'F', 'Z'], ['M', 'F', 'Last']].map(authoredOrderKey));
        compilation.documents.map(document => document.path).should.deep.equal(['application.play', 'steps/a.play', 'steps/z.play']);
    });

    it('should preserve implicit containers and nested feature declarations', () => {
        const application = parse('module M\n  feature F').value;
        recordAuthoredOrder(application, ['root.play'], [
            { path: 'root.play', source: 'import "placed.play"', placement: [] },
            { path: 'placed.play', source: 'feature Nested\n  slice StateChange S', placement: ['M', 'F'] },
        ]);
        // An import without a resolved matching placement does not visit the file.
        authoredOrderOf(application).size.should.equal(0);
        recordAuthoredOrder(application, ['placed.play'], [{ path: 'placed.play', source: 'feature Nested\n  slice StateChange S', placement: [] }]);
        authoredOrderOf(application).size.should.equal(0);
        recordAuthoredOrder(application, ['root.play'], [
            { path: 'root.play', source: 'module M\n  feature F\n    import "placed.play"', placement: [] },
            { path: 'placed.play', source: 'feature Nested\n  slice StateChange S', placement: ['M', 'F'] },
        ]);
        rank(application, 'M', 'F', 'Nested')!.should.be.lessThan(rank(application, 'M', 'F', 'Nested', 'S')!);
    });

    it('should tolerate unresolved placements, absent roots and cycles without inventing ranks', () => {
        const application = parse('').value;
        recordAuthoredOrder(application, ['missing.play', 'bad.play', 'root.play'], [
            { path: 'bad.play', source: 'module Bad', placement: [], isPlacementResolved: false },
            { path: 'root.play', source: 'module Good\nimport "again.play"', placement: [] },
            { path: 'again.play', source: 'import "root.play"', placement: [] },
        ]);
        [...authoredOrderOf(application).keys()].should.deep.equal([authoredOrderKey(['Good'])]);
    });

    it('should copy metadata to a narrowed tree but never serialize it', () => {
        const compilation = compileApplication(new Map([['root.play', 'module M\n  feature F']]), ['root.play']);
        const original = compilation.value;
        recordAuthoredOrder(original, ['root.play'], compilation.documents);
        authoredOrderOf(original).size.should.be.greaterThan(0);
        const narrowed = copyAuthoredOrder(original, { ...original });
        authoredOrderOf(narrowed).should.equal(authoredOrderOf(original));
        JSON.stringify(narrowed).should.equal(JSON.stringify(original));
        const standalone = parse('module M').value;
        authoredOrderOf(copyAuthoredOrder(standalone, { ...standalone })).size.should.equal(0);
    });
});
