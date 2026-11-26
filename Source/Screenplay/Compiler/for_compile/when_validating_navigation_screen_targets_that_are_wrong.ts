// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

const codesOf = (lines: string[]): string[] => parse(lines.join('\n')).diagnostics.map(diagnostic => diagnostic.code);

const navigation = (item: string, ...feature: string[]): string[] => [
    'layout Shell',
    '  outlet sidebar of navigation',
    'module Shop',
    '  outlet sidebar extends',
    `    ${item}`,
    '  feature Projects',
    ...feature
];

const cases: [string, string[], string[], string[]][] = [
    ['an ambiguous screen', navigation('item p title "P" target Shop.Projects screen Projects',
        '    slice StateView A', '      screen Projects', '        fits page', '    slice StateView B', '      screen Projects', '        fits page'), [DiagnosticCodes.AmbiguousNavigationScreen], []],
    ['a screen that does not fit a page', navigation('item p title "P" target Shop.Projects screen Projects',
        '    slice StateView A', '      screen Projects', '        fits drawer'), [DiagnosticCodes.NonPageNavigationScreen], []],
    ['a screen a slice adopts', navigation('item p title "P" target Shop.Projects screen Projects',
        '    screen Projects', '      fits page', '    slice StateView A', '      uses screen Projects'), [], [DiagnosticCodes.UnknownNavigationScreen, DiagnosticCodes.AmbiguousNavigationScreen]],
    ['a screen a slice adopts that is already claimed', navigation('item p title "P" target Shop.Projects screen Projects',
        '    slice StateView A', '      screen Projects', '        fits page', '    slice StateView B', '      uses screen Projects'), [], [DiagnosticCodes.UnknownNavigationScreen]],
    ['a screen a slice adopts that nothing declares', navigation('item p title "P" target Shop.Projects screen Projects',
        '    slice StateView A', '      uses screen Elsewhere'), [DiagnosticCodes.UnknownNavigationScreen], []],
    ['a screen a slice adopts by a qualified name', navigation('item p title "P" target Shop.Projects screen Projects',
        '    screen Projects', '      fits page', '    slice StateView A', '      uses screen Projects.Projects'), [], []],
    ['a target in a feature that does not exist', navigation('item p title "P" target Shop.Nowhere screen Projects'), [], [DiagnosticCodes.UnknownNavigationScreen]]
];

describe('when validating navigation screen targets that are wrong', () => {
    for (const [description, lines, expected, absent] of cases) {
        it(`should judge ${description}`, () => {
            const codes = codesOf(lines);
            for (const code of expected) codes.should.include(code);
            for (const code of absent) codes.should.not.include(code);
        });
    }
});
