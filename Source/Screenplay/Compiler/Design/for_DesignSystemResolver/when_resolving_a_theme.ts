// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { resolveDesignTheme } from '../DesignSystemResolver';
import { DesignThemeEvaluation } from '../DesignThemeEvaluation';
import { DesignAssignmentSyntax } from '../../Syntax/DesignSystemSyntax';

const source = [
    'design system D', '  modes light, dark', '  default theme T',
    '  tokens', '    color', '      text', '      surface', '    space', '      md',
    '  components', '    Button', '      parts text, background', '      variant primary', '        text = color.text', '        background = color.surface',
    '          ',
    'theme T', '  implements D',
    '  palette', '    ink = "#111111"', '    paper = palette.ink', '    cyclic = palette.cyclic',
    '  values', '    space.md = "8px"', '    color.text = palette.paper', '    color.surface = "#FFFFFF"',
    '  dark', '    color.surface = palette.ink',
    'theme Other', '  implements E', '  values', '    color.text = "#000000"',
].join('\n');

const at = { line: 1, column: 1 };
const assignment = (name: string, value: string, isLiteral: boolean): DesignAssignmentSyntax => ({ kind: 'DesignAssignmentSyntax', name, value, isLiteral, location: at });

describe('when resolving a theme', () => {
    const application = parse(source).value;
    const theme = application.themes![0];

    it('should resolve every value in a mode, following the palette', () => {
        const resolved = resolveDesignTheme(application, theme, 'light')!;
        resolved.problems.should.deep.equal([]);
        Object.fromEntries(resolved.tokens).should.deep.equal({ 'space.md': '8px', 'color.text': '#111111', 'color.surface': '#FFFFFF' });
        [...resolved.components[0].variants[0].parts].should.deep.equal([['text', '#111111'], ['background', '#FFFFFF']]);
    });

    it('should let a mode block override the theme values', () => {
        resolveDesignTheme(application, theme, 'dark')!.tokens.get('color.surface')!.should.equal('#111111');
    });

    it('should resolve without a mode', () => {
        const resolved = resolveDesignTheme(application, theme, null)!;
        [resolved.mode, resolved.tokens.get('color.surface')].should.deep.equal([null, '#FFFFFF']);
    });

    it('should carry what could not be resolved as problems', () => {
        const unresolved = parse(source.replace('    color.surface = "#FFFFFF"', '    color.surface = color.missing')).value;
        resolveDesignTheme(unresolved, unresolved.themes![0], 'light')!.problems.should.deep.equal(["'color.surface = color.missing' cannot be resolved: 'color.missing' is not a token of design system 'D'"]);
    });

    it('should not resolve a mode the design system does not declare', () => (resolveDesignTheme(application, theme, 'sepia') === null).should.be.true);
    it('should not resolve a theme that implements another design system', () => (resolveDesignTheme(application, application.themes![1], 'light') === null).should.be.true);
    it('should not resolve against an application without a design system', () => (resolveDesignTheme(parse('theme T\n  implements D').value, theme, 'light') === null).should.be.true);
});

describe('when evaluating a theme that assigns a name twice', () => {
    const application = parse(source).value;
    const designSystem = application.designSystems![0];
    const theme = { ...application.themes![0], palette: [assignment('ink', '#111111', true), assignment('ink', '#999999', true)],
        values: [assignment('color.text', 'palette.ink', false), assignment('color.text', '"#999999"', true), assignment('color.surface', 'color.text', false)] };
    const evaluation = new DesignThemeEvaluation(designSystem, theme, 'light');
    evaluation.evaluate();

    it('should keep the first assignment of each name', () => {
        Object.fromEntries(evaluation.tokens).should.deep.equal({ 'color.text': '#111111', 'color.surface': '#111111' });
    });

    it('should answer whether a token has a value', () => {
        [evaluation.hasValue('color.text'), evaluation.hasValue('space.md')].should.deep.equal([true, false]);
    });
});

describe('when evaluating references that cannot be followed', () => {
    const application = parse(source).value;
    const designSystem = application.designSystems![0];
    const evaluate = (palette: DesignAssignmentSyntax[], values: DesignAssignmentSyntax[]): string[] => {
        const evaluation = new DesignThemeEvaluation(designSystem, { ...application.themes![0], palette, values, modeValues: [] }, null);
        evaluation.evaluate();
        return evaluation.problems.map(problem => `${problem.code} ${problem.message}`);
    };

    it('should report a palette entry the theme does not declare', () => {
        evaluate([], [assignment('color.text', 'palette.gone', false)]).should.deep.equal(["PLAY0922 'color.text = palette.gone' cannot be resolved: palette entry 'palette.gone' is not declared in theme 'T'"]);
    });

    it('should report a token the design system does not declare', () => {
        evaluate([], [assignment('color.text', 'color.gone', false)]).should.deep.equal(["PLAY0922 'color.text = color.gone' cannot be resolved: 'color.gone' is not a token of design system 'D'"]);
    });

    it('should report a token the theme gives no value', () => {
        evaluate([], [assignment('color.text', 'color.surface', false)]).should.deep.equal(["PLAY0922 'color.text = color.surface' cannot be resolved: token 'color.surface' has no value in theme 'T'"]);
    });

    it('should report a palette entry that refers to itself, once', () => {
        const problems = evaluate([assignment('loop', 'palette.loop', false)], [assignment('color.text', 'palette.loop', false), assignment('color.surface', 'palette.loop', false)]);
        problems.should.deep.equal(["PLAY0923 Theme 'T' resolves 'palette.loop' through itself (palette.loop -> palette.loop)"]);
    });

    it('should not resolve a value that already failed a second time', () => {
        evaluate([], [assignment('color.text', 'color.surface', false), assignment('space.md', 'color.text', false)]).should.have.lengthOf(1);
    });
});
