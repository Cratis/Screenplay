// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

const codesOf = (lines: string[]): string[] => parse(lines.join('\n')).diagnostics.map(diagnostic => diagnostic.code);

const hooked = (button: string, fits = 'pane'): string[] => [
    'layout Shell',
    '  main page',
    '  detail pane',
    '  outlet actions of actions',
    'module M',
    '  feature F',
    '    outlet actions extends',
    `      ${button}`,
    '    slice StateChange Add',
    '      command Add',
    '        name String',
    '      screen AddForm',
    `        fits ${fits}`
];

const catalog = (...extra: string[]): string[] => [
    'module Catalog',
    '  feature Products',
    '    slice StateView ProductCatalog',
    '      readmodel Product',
    '        sku String key',
    '      query Products => Product[]',
    '      screen ProductCatalog',
    '        table Product',
    '        template ListPage',
    '          detail',
    '            show ProductDetails by sku from Product.selection',
    ...extra
];

const cases: [string, string[], string][] = [
    ['a hook opening a slot the layout does not declare', hooked('button b title "B" slice Add opens in slot nowhere'), DiagnosticCodes.HookSlotNotDeclared],
    ['a hook opening a slot for a role the screen does not fit', hooked('button b title "B" slice Add opens in slot main'), DiagnosticCodes.HookSlotRoleNotFitted],
    ['a hook opening a slot for a role the screen fits', hooked('button b title "B" slice Add opens in slot detail'), ''],
    ['a hook opening a slot a screen already fills', [
        'layout Shell', '  main page', '  detail pane', '  outlet actions of actions',
        'module M', '  feature F',
        '    slice StateChange Add', '      command Add', '        name String', '      screen AddForm', '        fits pane',
        '    slice StateView V', '      screen V', '        template T', '          detail', '            title "x"',
        '        outlet actions extends', '          button b title "B" slice Add opens in slot detail'], DiagnosticCodes.HookSlotAlreadyFilled],
    ['a hook of a slice with nothing to open or ask for', [...hooked('button b title "B" slice Noop'), '    slice StateChange Noop', '      command Noop'], ''],
    ['a hook in a screen of a template nothing declares', [
        'layout Shell', '  main page', '  outlet actions of actions',
        'module M', '  feature F',
        '    slice StateChange Add', '      command Add', '        name String', '      screen AddForm', '        fits pane',
        '    slice StateView V', '      screen V', '        template Missing',
        '        outlet actions extends', '          button b title "B" slice Add opens in slot detail'], ''],
    ['a show standing outside a template slot', [...catalog().slice(0, 7), '        show ProductDetails by sku from products.selection'], DiagnosticCodes.InvalidScreenShow],
    ['a hook in an outlet no layout offers', [
        'module M', '  feature F', '    outlet nowhere extends', '      button b title "B" slice Add opens in slot detail',
        '    slice StateChange Add', '      command Add', '        name String', '      screen AddForm', '        fits pane'], DiagnosticCodes.UnknownOutlet],
    ['a screen template declared inside a feature', ['module M', '  feature F', '    screen template T', '      body page'], ''],
    ['a show of a screen the document does not declare', catalog(), DiagnosticCodes.InvalidScreenShow],
    ['a show from an element the screen does not have', [...catalog().slice(0, 7), ...catalog().slice(8), '    slice StateView ProductDetails', '      screen ProductDetails'], DiagnosticCodes.InvalidScreenShow],
    ['a show by a key the shown screen is not keyed by', catalog('    slice StateView ProductDetails', '      readmodel Detail', '        id String key', '      query ById => Detail', '      screen ProductDetails', '        data Detail via query ById by other'), DiagnosticCodes.InvalidScreenShow]
];

describe('when validating hooks and shown screens that are wrong', () => {
    for (const [description, lines, code] of cases) {
        it(code === '' ? `should accept ${description}` : `should report ${code} for ${description}`, () => {
            const codes = codesOf(lines);
            if (code === '') codes.should.not.include.members([DiagnosticCodes.HookSlotNotDeclared, DiagnosticCodes.HookSlotRoleNotFitted]);
            else codes.should.include(code);
        });
    }
});
