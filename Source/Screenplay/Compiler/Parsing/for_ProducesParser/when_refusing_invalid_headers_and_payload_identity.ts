// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        value String\n';

describe('when refusing invalid headers and payload identity', () => {
    it('should discard a malformed production and its children without consuming its sibling', () => {
        const result = parse(prefix + '        produces ???\n          ignored = value\n        produces event Kept\n          value String = value');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.InvalidProducesDeclaration);
        expect(result.value.modules[0].features[0].slices[0].commands[0].produces.map(production => production.event)).toEqual(['Kept']);
    });
    it('should refuse identifier on inline payload and retain the ordinary property', () => {
        const result = parse(prefix + '        produces event Changed\n          value String identifier = value');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.IdentifierOnEventProperty);
        const produced = result.value.modules[0].features[0].slices[0].commands[0].produces[0];
        expect(produced.inlineEvent!.properties[0]).toMatchObject({ name: 'value', isIdentifier: false });
        expect(produced.mappings[0].property).toBe('value');
    });
});
