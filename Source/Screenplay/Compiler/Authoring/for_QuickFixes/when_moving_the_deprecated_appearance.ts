// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { compileApplication } from '../../Files/PlayApplicationAssembly';
import { applyQuickFixEdits, findQuickFixes, isQuickFixDiagnostic, prepareQuickFixes, QuickFix } from '../QuickFixes';

const designSystem = `design system D
  modes light
  default theme T
  tokens
    color
      surface
    radius
      control
    font
      sans
`;

const theme = `theme T
  implements D
  values
    radius.control = "6px"
    font.sans      = "inter"
  light
    color.surface = "#FFFFFF"
`;

const profile = `ui profile P
  appearance
    color scheme light
    accent "#4F46E5"
    corners round
    font source-serif-4
`;

const single = `${designSystem}\n${theme}\n${profile}`;
const code = 'PLAY0929';
const lineOf = (source: string, text: string): number => source.split('\n').findIndex(line => line.trim() === text) + 1;
const apply = (source: string, fix: QuickFix): string => applyQuickFixEdits(source, fix.edits)!;
const codes = (source: string): string[] => compileApplication(new Map([['a.play', source]])).diagnostics.map(diagnostic => diagnostic.code);

describe('when moving the deprecated appearance members into the theme', () => {
    it('should treat PLAY0929 as a quick-fix diagnostic', () => {
        expect(isQuickFixDiagnostic(code)).toBe(true);
    });

    it('should offer the repair on each deprecated line with the C# title', () => {
        for (const text of ['accent "#4F46E5"', 'corners round', 'font source-serif-4']) {
            const fixes = findQuickFixes(single, { line: lineOf(single, text), diagnosticCode: code });
            expect(fixes.map(fix => fix.title)).toEqual(["Move the appearance values into theme 'T'"]);
        }
    });

    it('should not offer the repair on the color scheme line', () => {
        expect(findQuickFixes(single, { line: lineOf(single, 'color scheme light'), diagnosticCode: code })).toEqual([]);
    });

    const fixed = apply(single, findQuickFixes(single, { line: lineOf(single, 'accent "#4F46E5"'), diagnosticCode: code })[0]);

    it('should move the accent into a palette section of the theme', () => {
        expect(fixed).toContain('theme T\n  implements D\n  palette\n    accent = "#4F46E5"\n\n  values\n');
    });

    it('should leave the values the theme already sets', () => {
        expect(fixed).toContain('    radius.control = "6px"\n    font.sans      = "inter"\n');
        expect(fixed).not.toContain('12px');
    });

    it('should remove every deprecated line and keep the color scheme', () => {
        expect(fixed).toContain('ui profile P\n  appearance\n    color scheme light\n');
        expect(fixed).not.toMatch(/accent "|corners round|font source/u);
    });

    it('should leave no deprecation warning and no error', () => {
        expect(codes(fixed)).not.toContain(code);
        expect(codes(fixed).filter(value => value !== code)).toEqual(codes(single).filter(value => value !== code));
    });

    it('should select the same repair from the markers of several lines', () => {
        const requests = ['accent "#4F46E5"', 'corners round'].map(text => ({ line: lineOf(single, text), diagnosticCode: code }));
        expect(prepareQuickFixes(single)(requests)).toHaveLength(1);
    });

    it('should add the values the theme lacks, aligned with its own', () => {
        const source = `${designSystem}\n${theme.replace('    radius.control = "6px"\n    font.sans      = "inter"\n', '    space.md = "1px"\n').replace('color\n', 'color\n')}\n${profile}`.replace('    font\n      sans\n', '    font\n      sans\n    space\n      md\n');
        const result = apply(source, findQuickFixes(source, { line: lineOf(source, 'corners round'), diagnosticCode: code })[0]);
        expect(result).toContain('    space.md       = "1px"\n    radius.control = "12px"\n    font.sans      = "source-serif-4"\n');
    });

    it('should drop the appearance block when only deprecated members were in it', () => {
        const source = single.replace('    color scheme light\n', '');
        const result = apply(source, findQuickFixes(source, { line: lineOf(source, 'corners round'), diagnosticCode: code })[0]);
        expect(result).toContain('ui profile P\n');
        expect(result).not.toContain('appearance');
    });

    it('should use the design systems default theme when the profile selects none and its own theme when it does', () => {
        const other = single.replace('\nui profile P', '\ntheme U\n  implements D\n  values\n    radius.control = "1px"\n    font.sans = "x"\n\nui profile P').replace('  appearance\n', '  theme U\n  appearance\n');
        expect(findQuickFixes(other, { line: lineOf(other, 'corners round'), diagnosticCode: code })[0].title).toBe("Move the appearance values into theme 'U'");
    });

    it('should refuse when the design system does not declare the token', () => {
        const source = single.replace('    radius\n      control\n', '');
        expect(findQuickFixes(source, { line: lineOf(source, 'corners round'), diagnosticCode: code })).toEqual([]);
    });

    it('should refuse when the theme holds a different accent', () => {
        const source = single.replace('  values\n', '  palette\n    accent = "#000000"\n\n  values\n');
        expect(findQuickFixes(source, { line: lineOf(source, 'accent "#4F46E5"'), diagnosticCode: code })).toEqual([]);
    });

    it('should keep an identical accent the theme already has', () => {
        const source = single.replace('  values\n', '  palette\n    accent = "#4f46e5"\n\n  values\n');
        const result = apply(source, findQuickFixes(source, { line: lineOf(source, 'accent "#4F46E5"'), diagnosticCode: code })[0]);
        expect(result.match(/accent = /gu)).toHaveLength(1);
        expect(result).not.toContain('corners');
    });

    it('should refuse when the application has no design system', () => {
        expect(findQuickFixes(profile, { line: lineOf(profile, 'corners round'), diagnosticCode: code })).toEqual([]);
    });

    it('should use the document line ending', () => {
        const source = single.replaceAll('\n', '\r\n');
        const result = apply(source, findQuickFixes(source, { line: lineOf(source.replaceAll('\r', ''), 'corners round'), diagnosticCode: code })[0]);
        expect(result).toContain('implements D\r\n  palette\r\n    accent = "#4F46E5"\r\n\r\n  values\r\n');
        expect(result.replaceAll('\r\n', '')).not.toContain('\n');
    });

    it('should edit the theme in its own document when the application has several', () => {
        const documents = new Map([['design.play', `${designSystem}\n${theme}`], ['shell.play', profile]]);
        const fixes = findQuickFixes(profile, { path: 'shell.play', documents, line: lineOf(profile, 'font source-serif-4'), diagnosticCode: code });
        expect(fixes).toHaveLength(1);
        expect([...new Set(fixes[0].edits.map(edit => edit.path))].sort()).toEqual(['design.play', 'shell.play']);
        const edited = new Map(documents);
        for (const path of ['design.play', 'shell.play']) edited.set(path, applyQuickFixEdits(documents.get(path)!, fixes[0].edits.filter(edit => edit.path === path))!);
        expect(edited.get('design.play')).toContain('palette\n    accent = "#4F46E5"');
        expect(edited.get('shell.play')).toBe('ui profile P\n  appearance\n    color scheme light\n');
        expect(compileApplication(edited).diagnostics.map(diagnostic => diagnostic.code)).not.toContain(code);
    });

    const fixFor = (source: string, text = 'corners round', options = {}) => findQuickFixes(source, { line: lineOf(source, text), diagnosticCode: code, ...options });

    it('should not offer the repair for a buffer that differs from its document or sits in a placement', () => {
        expect(fixFor(single, 'corners round', { path: 'a.play', documents: new Map([['a.play', 'other']]) })).toEqual([]);
        expect(fixFor(single, 'corners round', { placement: ['M'] })).toEqual([]);
    });

    it('should refuse when the profile selects a theme that is not declared or the design system has no default theme', () => {
        const missing = single.replace('  appearance\n', '  theme Nope\n  appearance\n');
        expect(fixFor(missing)).toEqual([]);
        expect(fixFor(single.replace('  default theme T\n', ''))).toEqual([]);
    });

    it('should refuse corners it cannot translate and tokens the design system lacks', () => {
        expect(fixFor(single.replace('corners round', 'corners pill'))).toEqual([]);
        expect(fixFor(single.replace('corners round', 'corners constructor'))).toEqual([]);
        expect(fixFor(single.replace('    font\n      sans\n', '').replace('    font.sans      = "inter"\n', ''))).toEqual([]);
    });

    it('should refuse to rewrite a section that holds a comment', () => {
        const source = single.replace('    radius.control = "6px"\n    font.sans      = "inter"\n', '    // radius for controls\n    space.md = "1px"\n').replace('    font\n      sans\n', '    font\n      sans\n    space\n      md\n');
        expect(fixFor(source)).toEqual([]);
    });

    it('should create the palette and values sections before a mode block and at the end of a theme without one', () => {
        const noValues = single.replace('  values\n    radius.control = "6px"\n    font.sans      = "inter"\n', '');
        const result = apply(noValues, fixFor(noValues)[0]);
        expect(result).toContain('  implements D\n  palette\n    accent = "#4F46E5"\n\n  values\n    radius.control = "12px"\n    font.sans      = "source-serif-4"\n\n  light\n');
        const bare = `${designSystem}\ntheme T\n  implements D\n\n${profile}`.replace('  light\n', '');
        const barePatched = apply(bare, fixFor(bare)[0]);
        expect(barePatched).toContain('theme T\n  implements D\n\n  palette\n    accent = "#4F46E5"\n\n  values\n    radius.control = "12px"');
    });

    it('should append to a section that has a heading but no entries', () => {
        const source = single.replace('  values\n    radius.control = "6px"\n    font.sans      = "inter"\n', '  palette\n  values\n    radius.control = "6px"\n    font.sans      = "inter"\n');
        expect(apply(source, fixFor(source, 'accent "#4F46E5"')[0])).toContain('  palette\n    accent = "#4F46E5"\n  values\n');
    });
});
