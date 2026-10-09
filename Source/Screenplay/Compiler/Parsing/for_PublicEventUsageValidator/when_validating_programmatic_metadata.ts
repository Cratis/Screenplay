// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, publicEventDiagnostics } from '../../ScreenplayCompiler';
import { EventVisibility } from '../../Syntax/EventVisibility';
import { TranslationDirection } from '../../Syntax/TranslationDirection';
import { ImportSyntax } from '../../Syntax/Declarations';
import { SliceSyntax } from '../../Syntax/Structure';

const source = 'import Outside.Arrived from "other/store"\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      event Changed\n';

for (const [condition, metadata] of [
    ['a public import without an origin', { visibility: EventVisibility.Public, origin: undefined }],
    ['a private import with an origin', { visibility: EventVisibility.Private, origin: 'other/store' }],
    ['an import with a blank origin', { origin: '\u0085\t' }],
    ['an import with a nonstring origin', { origin: 42 }],
    ['an import with an invalid visibility', { visibility: 'Partner' }],
] as const) {
    describe(`when validating programmatic metadata with ${condition}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;
        let imported: ImportSyntax;

        beforeEach(() => {
            const application = parse(source).value;
            imported = { ...application.imports[0], ...metadata } as unknown as ImportSyntax;
            diagnostics = publicEventDiagnostics({ ...application, imports: [imported] });
        });

        it('should reject the malformed import rather than silently accept it', () => {
            diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.InvalidImportDeclaration]);
        });
        it('should locate the rejection at the import declaration', () => {
            diagnostics[0].location.should.deep.equal(imported.location);
            diagnostics[0].severity.should.equal('error');
        });
    });
}

for (const [condition, metadata] of [
    ['a direction on a state view', { type: 'StateView', direction: TranslationDirection.Inbound }],
    ['an unknown translation direction', { direction: 'Sideways' }],
] as const) {
    describe(`when validating programmatic metadata with ${condition}`, () => {
        let diagnostics: ReturnType<typeof publicEventDiagnostics>;
        let slice: SliceSyntax;

        beforeEach(() => {
            const application = parse(source).value;
            const module = application.modules[0];
            const feature = module.features[0];
            slice = { ...feature.slices[0], ...metadata } as unknown as SliceSyntax;
            diagnostics = publicEventDiagnostics({ ...application, modules: [{ ...module, features: [{ ...feature, slices: [slice] }] }] });
        });

        it('should reject the malformed direction', () => {
            diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.InvalidSliceDeclaration]);
        });
        it('should locate the rejection at the slice declaration', () => {
            diagnostics[0].location.should.deep.equal(slice.location);
            diagnostics[0].severity.should.equal('error');
        });
    });
}
