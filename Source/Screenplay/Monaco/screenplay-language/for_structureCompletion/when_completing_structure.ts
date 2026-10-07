// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { scanDocument } from '../symbols';
import { structureCompletion } from '../structure-completions';

const suggest = (source: string) => {
    const lines = source.split('\n');
    const index = lines.length - 1;
    return structureCompletion(lines, index, lines[index], '', scanDocument(lines))?.text ?? '';
};

const catalog = [
    'concept ProductId : Uuid',
    'concept Sku : String',
    'module Catalog',
    '    feature Products',
    '        slice StateChange ProductAddition',
].join('\n');

describe('when completing the structure of a command', () => {
    it('should suggest an inline event produced from the command properties', () => {
        suggest(`${catalog}\n            command AddProduct\n                productId ProductId identifier\n                sku Sku\n                `)
            .should.equal('produces event ProductAdded\n                    sku Sku = sku');
    });

    it('should suggest producing a declared event, mapping the properties both share', () => {
        suggest(`${catalog}\n            event ProductAdded\n                sku Sku\n            command AddProduct\n                sku Sku\n                `)
            .should.equal('produces ProductAdded\n                    sku = sku');
    });

    it('should suggest nothing once the command produces something', () => {
        suggest(`${catalog}\n            command AddProduct\n                sku Sku\n                produces ProductAdded\n                `).should.equal('');
    });
});

describe('when completing the structure of a specification', () => {
    it('should suggest a skeleton from the slice command and its event', () => {
        suggest(`${catalog}\n            command AddProduct\n                productId ProductId identifier\n                sku Sku\n                produces ProductAdded\n            specification AddingAProduct\n                `)
            .should.equal([
                'when AddProduct',
                '                    productId = "0f8fad5b-d9cb-469f-a165-70867728950e"',
                '                    sku = "Example"',
                '                then ProductAdded',
                '                    for "0f8fad5b-d9cb-469f-a165-70867728950e"',
                '                    sku = "Example"',
            ].join('\n'));
    });
});

describe('when completing the structure of a form', () => {
    it('should suggest a field for every command property', () => {
        suggest(`${catalog}\n            command AddProduct\n                sku Sku\n                name String\n        form AddProductForm for AddProduct\n            `)
            .should.equal('field sku\n            field name');
    });
});

describe('when nothing is obviously missing', () => {
    it('should suggest nothing after text on the line', () => {
        const lines = [`${catalog}`, '            command AddProduct', '                sku Sku', '                pro'].join('\n').split('\n');
        (structureCompletion(lines, lines.length - 1, lines[lines.length - 1], '', scanDocument(lines)) === null).should.be.true;
    });
});
