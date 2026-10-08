// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { mergeDocuments } from '../Files/PlayFolderMerge';

const source = readFileSync(new URL('../Conformance/authoring-metadata.play', import.meta.url), 'utf8');
const documentation = (text: string) => `  documentation\n    \`\`\`markdown\n    ${text}\n    \`\`\`\n`;

describe('when authoring documentation', () => {
    it('should retain metadata on every supported owner and typed documentation properties', () => {
        const result = parse(source);
        expect(result.diagnostics).toEqual([]);
        const module = result.value.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[0];
        expect(module.documentation).toContain('# Boundary');
        expect(feature.documentation).toContain('drafts');
        expect(slice.documentation).toContain('## Assumption');
        expect(slice.commands[0].documentation).toContain('delivery');
        expect(slice.readModels[0].documentation).toContain('delivery');
        expect(slice.reactions[0].documentation).toContain('issuance');
        expect(slice.specifications[0].description).toContain('Witnesses');
        expect(slice.specifications[1].description).toContain('\n');
        expect(slice.commands[0].properties.map(property => property.name)).toEqual(['documentation', 'invoiceId']);
        expect(slice.readModels[0].properties.map(property => property.name)).toEqual(['documentation']);
    });

    it('should keep the first documentation while warning only on disagreement', () => {
        const result = mergeDocuments([
            parse(`module M\n${documentation('First')}`, 'a.play'),
            parse(`module M\n${documentation('First')}`, 'b.play'),
            parse(`module M\n${documentation('Other')}`, 'c.play'),
        ]);
        expect(result.value.modules[0].documentation).toBe('First');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0559']);
        expect(result.diagnostics[0].location.path).toBe('c.play');
    });
});
