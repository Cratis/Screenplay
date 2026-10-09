// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { mergeDocuments } from '../../Files/PlayFolderMerge';

describe('when parsing purposes', () => {
    it('should diagnose malformed headers, fields and references', () => {
        const result = parse('purpose 42\n  unknown thing\n  retention notQuoted\nmodule M\n  purpose 42');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0596', 'PLAY0596', 'PLAY0596', 'PLAY0596']);
    });
    it('should warn on an interest not supported by its basis', () => {
        const result = parse('purpose P\n  basis contract\n  interest "Prevent fraud"');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0601']);
    });
    it('should merge references in placed module and feature files once', () => {
        const declared = parse('purpose Billing\n  basis consent', 'root.play');
        const first = parse('purpose Billing\nfeature F\n  purpose Billing', 'one.play', ['M']);
        const second = parse('purpose Billing\nslice StateChange S\n  purpose Billing', 'two.play', ['M', 'F']);
        const result = mergeDocuments([declared, first, second]);
        expect(result.value.modules[0].purposes).toHaveLength(1);
        expect(result.value.modules[0].features[0].purposes).toHaveLength(1);
        expect(result.value.modules[0].features[0].slices[0].purposes?.[0].name).toBe('Billing');
    });
    it('should retain multiple escaped transfers and recipients', () => {
        const purpose = parse('purpose P\n  transfer "A" safeguard "A\\"B"\n  recipient "One"\n  recipient "Two"').value.purposes![0];
        expect(purpose.transfers[0].safeguard).toBe('A"B');
        expect(purpose.recipients).toEqual(['One', 'Two']);
    });
});
