// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { designContrast, parseDesignColor } from '../DesignColor';
import { categoriesForPart } from '../DesignVocabulary';

describe('when reading design colors', () => {
    it.each<[string, number[] | undefined]>([
        ['#fff', [255, 255, 255]],
        ['#0F8', [0, 255, 136]],
        ['#1234', [17, 34, 51]],
        ['#102030', [16, 32, 48]],
        ['#10203040', [16, 32, 48]],
        ['  #abcdef  ', [171, 205, 239]],
        ['rgb(1, 2, 3)', [1, 2, 3]],
        ['rgba( 10 ,20 ,30 , 0.5 )', [10, 20, 30]],
        ['rgb(255, 255, 255)', [255, 255, 255]],
        ['rgb(256, 0, 0)', undefined],
        ['rgb(0, 300, 0)', undefined],
        ['rgb(0, 0, 999)', undefined],
        ['#12', undefined],
        ['#ggg', undefined],
        ['red', undefined],
        ['', undefined],
    ])('should read %s', (value, expected) => {
        const color = parseDesignColor(value);
        (color === undefined ? 'none' : [color.red, color.green, color.blue].join(',')).should.equal(expected === undefined ? 'none' : expected.join(','));
    });

    it('should measure the contrast of black on white as 21', () => designContrast('#000', '#fff')!.should.be.closeTo(21, 0.001));
    it('should measure it the same whichever color is the foreground', () => designContrast('#fff', '#000')!.should.be.closeTo(21, 0.001));
    it('should measure the contrast of a color on itself as 1', () => designContrast('#4F46E5', 'rgb(79, 70, 229)')!.should.be.closeTo(1, 0.001));
    it('should measure a mid gray on white', () => designContrast('#777777', '#ffffff')!.should.be.closeTo(4.48, 0.01));
    it('should not measure a color it cannot read', () => {
        (designContrast('red', '#fff') === undefined).should.be.true;
        (designContrast('#fff', 'blue') === undefined).should.be.true;
    });
});

describe('when looking up the categories a design part takes', () => {
    it.each<[string, string[]]>([['text', ['color']], ['border', ['color', 'border']], ['height', ['size', 'space']], ['font', ['font', 'type']], ['glow', []], ['constructor', []], ['toString', []]])('should give %s %j', (part, expected) => {
        [...categoriesForPart(part)].should.deep.equal(expected);
    });
});
