// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { mergeDocuments } from '../../Files/PlayFolderMerge';
import { InvalidSyntaxJson } from '../../Syntax/StrictSyntaxJson';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const inheritedOptions = Object.create({ numericMode: 'legacy' }) as object;
const hiddenOptions = Object.defineProperty({ numericMode: 'legacy' }, 'unexpected', { value: true });
const symbolOptions = { numericMode: 'legacy', [Symbol('unexpected')]: true };

describe('when validating neutral import-barrel options', () => {
    it.each([null, undefined, 'legacy', 1, { numericMode: 'unknown' }, { numericMode: 1 }, { numericMode: 'legacy', unexpected: true }, { numericMode: 'exact', unexpected: true }, inheritedOptions, hiddenOptions, symbolOptions])('should retain malformed physical-root options %j in all orders', options => {
        const barrel = parse('import "child.play"\n', 'barrel.play');
        Object.assign(barrel.value, { sourceOptions: options });
        expect(() => toSyntaxJson(barrel.value)).toThrow(InvalidSyntaxJson);
        const declaration = parse('numbers exact\nconcept A : Decimal\n', 'child.play');
        for (const documents of [[barrel], [barrel, declaration], [declaration, barrel]]) {
            const merged = mergeDocuments(documents);
            merged.success.should.equal(false);
            merged.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0513').should.equal(true);
            merged.value.sourceOptions!.numericMode.should.not.equal('legacy');
            expect(() => toSyntaxJson(merged.value)).toThrow(InvalidSyntaxJson);
        }
    });
    it('should distinguish neutral unmarked barrels from explicit Exact assertions', () => {
        mergeDocuments([parse('import "child.play"\n'), parse('numbers exact\nconcept A : Decimal\n')]).success.should.equal(true);
        mergeDocuments([parse('numbers exact\nimport "child.play"\n'), parse('concept A : Decimal\n')]).success.should.equal(false);
        const old = parse('import "child.play"\n');
        delete (old.value as unknown as { sourceOptions?: unknown }).sourceOptions;
        mergeDocuments([old, parse('numbers exact\nconcept A : Decimal\n')]).success.should.equal(true);
    });
});
