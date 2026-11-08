// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const corpus = resolve(__dirname, '../../../DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1');
const read = (path: string): string => readFileSync(resolve(corpus, path), 'utf8');
const summarize = (source: string): string[] => parse(source).diagnostics.map(diagnostic => `${diagnostic.code} ${diagnostic.severity} ${diagnostic.message}`).sort();

// What the C# canonical corpus expects of the design system rejection vector (ScreenCompositionCorpus, "design-system-invalid"), as code and message.
const designSystemInvalid: readonly (readonly [string, string])[] = [
    ['PLAY0927', 'The default theme \'Missing\' of design system \'Contoso\' is not declared'],
    ['PLAY0919', 'The token \'color.primary\' is already declared'],
    ['PLAY0924', 'The part \'background\' of \'Button\' takes a color token, not \'space.sm\''],
    ['PLAY0922', '\'background = color.nothing\' on \'Button\' names no token of the design system'],
    ['PLAY0916', 'Unknown part \'glow\' on component \'Chip\' - expected one of text, background, border, radius, height, padding, shadow, font'],
    ['PLAY0917', 'The design system \'Second\' is a second one - an application has exactly one design system, \'Contoso\''],
    ['PLAY0921', 'Theme \'Nordic\' gives \'color.text\' no value in the mode \'dark\''],
    ['PLAY0920', 'Theme \'Nordic\' assigns \'space.xx\', which design system \'Contoso\' does not declare'],
    ['PLAY0923', 'Theme \'Nordic\' resolves \'color.primary\' through itself (color.primary -> color.surface -> color.primary)'],
    ['PLAY0922', '\'color.surface = palette.nope\' cannot be resolved: palette entry \'palette.nope\' is not declared in theme \'Nordic\''],
    ['PLAY0918', 'Theme \'Nordic\' has a block for the mode \'dim\', which design system \'Contoso\' does not declare (modes: light, dark)'],
    ['PLAY0928', 'In theme \'Faint\' (light), Button secondary: text #EEEEEE on background #FFFFFF has contrast 1.16, below the required 4.5'],
    ['PLAY0928', 'In theme \'Faint\' (dark), Button secondary: text #EEEEEE on background #FFFFFF has contrast 1.16, below the required 4.5'],
    ['PLAY0925', 'Theme \'Stranger\' implements \'Nothing\', but the application\'s design system is \'Contoso\''],
    ['PLAY0921', 'Theme \'Broken\' gives \'color.surface\' no value in the mode \'light\''],
    ['PLAY0921', 'Theme \'Broken\' gives \'color.text\' no value in the mode \'light\''],
    ['PLAY0921', 'Theme \'Broken\' gives \'color.primary\' no value in the mode \'light\''],
    ['PLAY0921', 'Theme \'Broken\' gives \'color.surface\' no value in the mode \'dark\''],
    ['PLAY0921', 'Theme \'Broken\' gives \'color.text\' no value in the mode \'dark\''],
    ['PLAY0921', 'Theme \'Broken\' gives \'color.primary\' no value in the mode \'dark\''],
    ['PLAY0930', 'Theme \'Broken\' already declares which design system it implements'],
    ['PLAY0930', 'Invalid palette line \'odd line\' - expected \'<name> = "<value>"\' or \'<name> = <reference>\''],
    ['PLAY0926', 'The ui profile \'Desktop\' selects theme \'Plain\', which does not implement the design system \'Contoso\'']
];

describe('when parsing the design system corpus vectors', () => {
    it('should read the positive design system without a diagnostic', () => {
        expect(parse(read('positive/DesignSystem.play.txt')).diagnostics).toEqual([]);
    });

    it('should report the same diagnostics for the invalid design system as the C# corpus expects', () => {
        const actual = parse(read('negative/DesignSystemInvalid.play.txt')).diagnostics.map(diagnostic => `${diagnostic.code} ${diagnostic.message}`).sort();
        expect(actual).toEqual(designSystemInvalid.map(([code, message]) => `${code} ${message}`).sort());
    });

    it('should report each deprecated appearance member of the deprecated appearance vector as a warning', () => {
        const diagnostics = parse(read('positive/UiProfileAppearanceDeprecated.play.txt')).diagnostics;
        expect(diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location.line])).toEqual([['PLAY0929', 'warning', 13], ['PLAY0929', 'warning', 14], ['PLAY0929', 'warning', 15]]);
    });

    it('should read the ui profile root vector without any appearance warning now that it sets no accent', () => {
        expect(summarize(read('positive/UiProfileRoot.play.txt'))).toEqual([]);
    });

    it('should warn about the deprecated members of the invalid ui profile root vector', () => {
        expect(parse(read('negative/UiProfileRootInvalid.play.txt')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0929');
    });
});
