// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { mergeDocuments } from '../Files/PlayFolderMerge';

const source = readFileSync(new URL('../Conformance/authoring-metadata.play', import.meta.url), 'utf8');
const documentation = (text: string) => `  documentation\n    \`\`\`markdown\n    ${text}\n    \`\`\`\n`;

describe('when authoring documentation', () => {
    it('should retain metadata on every supported owner and typed documentation properties', () => {
        const result = parse(source);
        result.diagnostics.should.deep.equal([]);
        const module = result.value.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[0];
        module.documentation!.should.contain('# Boundary');
        feature.documentation!.should.contain('drafts');
        slice.documentation!.should.contain('## Assumption');
        slice.commands[0].documentation!.should.contain('delivery');
        slice.readModels[0].documentation!.should.contain('delivery');
        slice.reactions[0].documentation!.should.contain('issuance');
        slice.specifications[0].description!.should.contain('Witnesses');
        slice.specifications[1].description!.should.contain('\n');
        slice.commands[0].properties.map(property => property.name).should.deep.equal(['documentation', 'invoiceId']);
        slice.readModels[0].properties.map(property => property.name).should.deep.equal(['documentation']);
    });

    it('should keep the first documentation while warning only on disagreement', () => {
        const result = mergeDocuments([
            parse(`module M\n${documentation('First')}`, 'a.play'),
            parse(`module M\n${documentation('First')}`, 'b.play'),
            parse(`module M\n${documentation('Other')}`, 'c.play'),
        ]);
        result.value.modules[0].documentation!.should.equal('First');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0559']);
        result.diagnostics[0].location.path!.should.equal('c.play');
    });
});
