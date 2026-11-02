// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing structured values in every position', () => {
    it('should read a composition set block and a structured item value', () => {
        const result = a_parsed_document(
            'instance Browse',
            '  set "shell:header".actions',
            '    item "export"',
            '  set shellHeader.filters none',
            '  items shellHeader.actions',
            '    item "export:csv"',
            '      tags',
            '        item "a"');
        expect(result.diagnostics).toEqual([]);
        const contributions = result.value.instanceContributions![0].contributions;
        expect(contributions[0].value).toMatchObject({ kind: 'ListExpressionSyntax', items: [{ value: 'export' }] });
        expect(contributions[1].value).toMatchObject({ kind: 'ListExpressionSyntax', items: [] });
        expect(contributions[2].items[0].values[0]).toMatchObject({ field: 'tags', value: { kind: 'ListExpressionSyntax' } });
    });

    it('should read structured inline operation inputs', () => {
        const result = parse('type Contact\n  recipient String\nsystem Mailer\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        produces operation Send\n          uses Mailer\n          contact Contact\n            recipient = "Ada"\n          tags String[] none\n');
        expect(result.diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
        const produces = result.value.modules[0].features[0].slices[0].commands[0].produces[0];
        expect(produces.mappings.map(mapping => mapping.source.kind)).toEqual(['ObjectExpressionSyntax', 'ListExpressionSyntax']);
    });

    it('should read structured inline event values', () => {
        const result = parse('type Line\n  sku String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        produces event Placed\n          lines Line[]\n            item\n              sku = "A-1"\n          tags String[] none\n');
        expect(result.diagnostics.filter(diagnostic => diagnostic.severity === 'error')).toEqual([]);
        const produces = result.value.modules[0].features[0].slices[0].commands[0].produces[0];
        expect(produces.mappings.map(mapping => mapping.source.kind)).toEqual(['ListExpressionSyntax', 'ListExpressionSyntax']);
        expect(produces.inlineEvent!.properties.map(property => property.name)).toEqual(['lines', 'tags']);
    });
});
