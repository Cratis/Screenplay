// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ProvideSyntax } from '../Syntax/ProvideSyntax';
import { ParserContext } from '../Parsing/ParserContext';
import { LineReader } from '../Parsing/LineReader';
import { validateProvides } from '../Parsing/ProvideValidator';

const sliceBody = [
    '      query VesselByKey => Vessel optional',
    '        by vesselId Uuid',
    '      readmodel Vessel',
    '        vesselId Uuid',
];

const model = (...provides: string[]): string => ['module Fleet', ...provides, '  feature Data', '    slice StateView Vessels', ...sliceBody, ''].join('\n');
const codes = (source: string): string[] => parse(source).diagnostics.map(diagnostic => diagnostic.code);

class Collector extends ScreenplaySyntaxWalker {
    readonly provides: ProvideSyntax[] = [];
    override visitProvide(syntax: ProvideSyntax): void {
        this.provides.push(syntax);
        super.visitProvide(syntax);
    }
}

describe('when parsing provided values', () => {
    it('should retain every source at every level', () => {
        const result = parse([
            'provide region String from "Nordics"',
            'provide roles String[] from $context.identity.roles',
            'module Fleet',
            '  provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId',
            '  provide entitlements String from host',
            '  feature Data',
            '    slice StateView Vessels',
            '      provide score Int',
            '        file Score.cs',
            '      provide limit Int',
            '        ```csharp',
            '        return 10;',
            '        ```',
            '      query VesselByKey => Vessel optional',
            '        by vesselId Uuid',
            '      readmodel Vessel',
            '        vesselId Uuid',
            ''
        ].join('\n'));
        result.diagnostics.should.deep.equal([]);
        const collector = new Collector();
        collector.visitApplication(result.value);
        collector.provides.map(provide => `${provide.name}:${provide.source.kind}`).should.deep.equal([
            'region:ValueProvideSourceSyntax',
            'roles:ValueProvideSourceSyntax',
            'vessel:QueryProvideSourceSyntax',
            'entitlements:HostProvideSourceSyntax',
            'score:FileProvideSourceSyntax',
            'limit:CodeProvideSourceSyntax'
        ]);
        result.value.modules[0].provides!.map(provide => provide.type.isOptional).should.deep.equal([true, false]);
    });

    it('should split the key on top level commas', () => {
        const result = parse('module Fleet\n  provide vessel Vessel from query VesselByKey by $context.arguments.vesselId, "x"\n');
        const source = result.value.modules[0].provides![0].source;
        source.kind.should.equal('QueryProvideSourceSyntax');
        if (source.kind === 'QueryProvideSourceSyntax') source.by.should.have.length(2);
    });

    it('should omit provides when there are none', () => {
        const result = parse('module Fleet\n  feature Data\n');
        (result.value.modules[0].provides === undefined).should.equal(true);
    });

    it('should reject a declaration that is not well formed', () => {
        codes('module Fleet\n  provide Region String from host\n').should.include('PLAY1119');
    });

    it('should reject a source that is not a value', () => {
        codes('module Fleet\n  provide region String from nonsense words\n').should.include('PLAY1120');
    });

    it('should require a source', () => {
        codes('module Fleet\n  provide region String\n').should.include('PLAY1121');
    });

    it('should reject a body beneath a stated source', () => {
        codes('module Fleet\n  provide region String from host\n    file Region.cs\n').should.include('PLAY1122');
    });

    it('should reject a value provided twice at one level', () => {
        codes('module Fleet\n  provide region String from host\n  provide region String from host\n').should.include('PLAY1123');
    });

    it('should reject a reserved name', () => {
        codes('module Fleet\n  provide tenant String from host\n').should.include('PLAY1124');
    });

    it('should require an override to keep the type', () => {
        codes('provide region String from host\nmodule Fleet\n  provide region Int from host\n').should.include('PLAY1125');
    });

    it('should accept an override that keeps the type', () => {
        codes('provide region String from host\nmodule Fleet\n  provide region String from "x"\n').should.not.include('PLAY1125');
    });

    it('should reject an unknown query', () => {
        codes(model('  provide vessel Vessel optional from query Missing by "x"')).should.include('PLAY1126');
    });

    it('should reject an observable query', () => {
        const source = model('  provide vessel Vessel from query Live by "x"').replace('      readmodel Vessel', '      query Live => observable Vessel\n        by vesselId Uuid\n      readmodel Vessel');
        codes(source).should.include('PLAY1127');
    });

    it('should reject a result type that differs from the query', () => {
        codes(model('  provide vessel Int from query VesselByKey by "x"')).should.include('PLAY1128');
    });

    it('should reject a key count that differs from the query', () => {
        codes(model('  provide vessel Vessel optional from query VesselByKey')).should.include('PLAY1128');
    });

    it('should accept a query that matches', () => {
        codes(model('  provide vessel Vessel optional from query VesselByKey by "x"')).should.deep.equal([]);
    });

    it('should reject provided values that read each other in a cycle', () => {
        codes('module Fleet\n  provide a String from $context.b\n  provide b String from $context.a\n').should.include('PLAY1129');
    });

    it('should withdraw the unknown context warning for a provided root', () => {
        codes('module Fleet\n  provide region String from host\n  provide copy String from $context.region\n').should.not.include('PLAY0153');
    });

    it('should reject a query source that names no query', () => {
        codes('module Fleet\n  provide vessel Vessel from query\n').should.include('PLAY1120');
    });

    it('should reject a body that is neither a file nor a code block', () => {
        codes('module Fleet\n  provide vessel Vessel\n    nonsense here\n').should.include('PLAY1120');
    });

    it('should split the key list around an escaped quote', () => {
        codes(model('  provide vessel Vessel optional from query VesselByKey by "a\\"b, c"')).should.not.include('PLAY1120');
    });

    it('should reject a value provided twice at the application level', () => {
        codes('provide region String from host\nprovide region String from host\n').should.include('PLAY1123');
    });

    it('should reject a duplicate that reaches the validator, as a merged folder delivers it', () => {
        const application = parse('provide region String from host\n').value;
        const provide = application.provides![0];
        const context = new ParserContext(new LineReader([]));
        validateProvides({ ...application, provides: [provide, provide] }, context);
        context.diagnostics.map(diagnostic => diagnostic.code).should.include('PLAY1123');
    });

    it('should place a provide of a placed file in its module', () => {
        const result = parse('provide region String from host\n', 'Fleet.play', ['Fleet']);
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([]);
    });

    it('should accept values that share a dependency without a cycle', () => {
        codes('module Fleet\n  provide base String from host\n  provide left String from $context.base\n  provide right String from $context.base\n  provide both Vessel optional from query Missing by $context.left, $context.right\n').should.not.include('PLAY1129');
    });

    it('should resolve a query named with its slice', () => {
        codes(model('  provide vessel Vessel optional from query Vessels.VesselByKey by "x"')).should.not.include('PLAY1126');
    });

    it('should keep a bracketed key together when splitting the key list', () => {
        codes(model('  provide vessel Vessel optional from query VesselByKey by f("a", "b"), "c"')).should.not.include('PLAY1120');
    });

    it('should report a code block that never closes', () => {
        codes('module Fleet\n  provide limit Int\n    ```csharp\n    return 10;\n').length.should.be.greaterThan(0);
    });

    it('should keep the unknown context warning for an unprovided root', () => {
        codes('module Fleet\n  provide copy String from $context.region\n').should.include('PLAY0153');
    });
});
