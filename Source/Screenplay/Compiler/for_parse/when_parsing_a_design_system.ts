// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { mergeDocuments } from '../Files/PlayFolderMerge';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { resolveDesignTheme } from '../Design/DesignSystemResolver';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ThemeSyntax } from '../Syntax/DesignSystemSyntax';
import { ApplicationSyntax } from '../Syntax/Structure';
import { decodeExactSyntaxJson } from '../Syntax/StrictSyntaxJson';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const document = readFileSync(resolve(__dirname, '../Conformance/design-system.play'), 'utf8');

class KindCounter extends ScreenplaySyntaxWalker {
    readonly kinds = new Map<string, number>();
    override visitNode(node: SyntaxNode): void { this.kinds.set(node.kind, (this.kinds.get(node.kind) ?? 0) + 1); }
}

describe('when parsing a design system', () => {
    const result = parse(document, 'design.play');
    const application = result.value;
    const designSystem = application.designSystems![0];

    it('should read it without a diagnostic', () => result.diagnostics.should.deep.equal([]));

    it('should read the modes, the default theme and every token category', () => {
        designSystem.name.should.equal('StorefrontDesign');
        designSystem.modes.should.deep.equal(['light', 'dark']);
        designSystem.defaultTheme!.should.equal('Daylight');
        designSystem.tokens.map(category => category.category).should.deep.equal(['color', 'space', 'size', 'radius', 'border', 'shadow', 'font', 'type', 'motion', 'opacity', 'z']);
        designSystem.tokens[0].tokens.map(token => [token.name, token.description]).should.deep.equal([
            ['surface', 'page background'], ['text', 'body text'], ['primary', 'primary action'], ['on-primary', 'text on a primary action'], ['outline', null]]);
    });

    it('should read the components with their variants and states', () => {
        const button = designSystem.components[0];
        button.name.should.equal('Button');
        button.parts.should.have.lengthOf(8);
        button.variants.map(variant => variant.name).should.deep.equal(['primary', 'quiet']);
        button.variants[0].parts.map(part => [part.name, part.value, part.isLiteral]).slice(0, 2).should.deep.equal([['text', 'color.on-primary', false], ['background', 'color.primary', false]]);
        button.variants[0].states.map(state => [state.name, state.parts.length]).should.deep.equal([['hover', 1], ['disabled', 2]]);
    });

    it('should read the words and the rules', () => {
        designSystem.words!.voice.map(entry => [entry.name, entry.value, entry.isLiteral]).should.deep.equal([['tone', 'Plain and exact', true], ['address', 'You', true]]);
        designSystem.words!.naming.map(entry => entry.name).should.deep.equal(['customer']);
        designSystem.rules.map(rule => [rule.foreground, rule.background, rule.minimum]).should.deep.equal([['text', 'background', '4.5'], ['text', 'background', '3']]);
    });

    it('should read a theme with its package compatibility, design system, palette, values and mode values', () => {
        const theme = application.themes![0];
        theme.name.should.equal('Daylight');
        theme.implements!.should.equal('StorefrontDesign');
        theme.compatibleWith.should.deep.equal(['Cratis.Components', 'Cratis.Components.Charts']);
        theme.palette!.map(entry => entry.name).should.deep.equal(['indigo', 'white', 'ink', 'line']);
        theme.values!.should.have.lengthOf(10);
        theme.modeValues!.map(block => [block.mode, block.values.length]).should.deep.equal([['light', 5], ['dark', 5]]);
        theme.directiveLocations!.implements.line.should.equal(74);
    });

    it('should read a theme that implements nothing', () => {
        const theme = application.themes![1];
        [theme.name, theme.implements, theme.compatibleWith.length].should.deep.equal(['Plain', undefined, 1]);
    });

    it('should keep the profile theme out of the syntax it writes', () => {
        application.designProfiles!.map(profile => [profile.name, profile.theme]).should.deep.equal([['Web', 'Daylight']]);
        JSON.stringify(toSyntaxJson(application)).should.not.contain('designProfiles');
    });

    it('should round trip through the exact syntax JSON', () => {
        const exact = parse(`numbers exact\n${document}`).value;
        const decoded = decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(exact))) as ApplicationSyntax;
        JSON.stringify(toSyntaxJson(decoded.designSystems![0])).should.equal(JSON.stringify(toSyntaxJson(exact.designSystems![0])));
        // Decoding restores the defaults the syntax JSON omits (a theme without palette, values, mode values or implements), so drop them again to compare.
        const withoutDefaults = (theme: ThemeSyntax) => Object.fromEntries(Object.entries(toSyntaxJson(theme) as Record<string, unknown>)
            .filter(([, value]) => value !== null && !(Array.isArray(value) && value.length === 0)));
        JSON.stringify(decoded.themes!.map(withoutDefaults)).should.equal(JSON.stringify(exact.themes!.map(withoutDefaults)));
    });

    it('should walk every node of the design system and the themes', () => {
        const counter = new KindCounter();
        counter.visitApplication(application);
        const counts = Object.fromEntries([...counter.kinds].filter(([kind]) => /^(Design|Theme)/.test(kind)));
        counts.should.deep.equal({
            DesignSystemSyntax: 1, DesignTokenCategorySyntax: 11, DesignTokenSyntax: 15, DesignComponentSyntax: 2, DesignVariantSyntax: 3, DesignStateSyntax: 2,
            DesignAssignmentSyntax: 42, DesignWordsSyntax: 1, DesignContrastRuleSyntax: 2, ThemeSyntax: 2, ThemeModeValuesSyntax: 2,
        });
    });

    it('should resolve a theme in a mode', () => {
        const resolved = resolveDesignTheme(application, application.themes![0], 'dark')!;
        resolved.problems.should.deep.equal([]);
        [resolved.designSystem, resolved.theme, resolved.mode].should.deep.equal(['StorefrontDesign', 'Daylight', 'dark']);
        resolved.tokens.get('color.surface')!.should.equal('#111827');
        resolved.tokens.get('space.md')!.should.equal('12px');
        const hover = resolved.components[0].variants[0].states.get('hover')!;
        [hover.get('background'), hover.get('border')].should.deep.equal(['#4F46E5', '#FFFFFF']);
    });
});

describe('when parsing a folder with a design system', () => {
    const designFile = 'design system D\n  default theme T\n  tokens\n    color\n      text';
    const themeFile = 'theme T\n  implements D\n  values\n    color.text = "#000000"';

    it('should hold the themes of one file to the design system of another', () => {
        const result = compileApplication(new Map([['design.play', designFile], ['theme.play', themeFile]]), ['design.play', 'theme.play']);
        result.diagnostics.should.deep.equal([]);
        result.value.designSystems!.map(candidate => candidate.name).should.deep.equal(['D']);
        result.value.themes!.map(candidate => candidate.name).should.deep.equal(['T']);
    });

    it('should carry the ui profiles of every file', () => {
        const profile = 'ui profile Web\n  theme T';
        const result = compileApplication(new Map([['design.play', designFile], ['theme.play', `${themeFile}\ntheme U\n  implements D\n${profile}`]]), ['design.play', 'theme.play']);
        result.value.designProfiles!.map(candidate => [candidate.name, candidate.theme]).should.deep.equal([['Web', 'T']]);
    });

    it('should keep the first design system and reject another', () => {
        const result = mergeDocuments([parse('design system A', 'a.play'), parse('design system B', 'b.play')]);
        result.value.designSystems!.map(candidate => candidate.name).should.deep.equal(['A']);
        const rejected = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0172');
        rejected.map(diagnostic => diagnostic.location.path).should.deep.equal(['b.play']);
        rejected[0].message.should.contain('design system').and.contain("'a.play'");
    });

    it('should report a theme declared in two files once', () => {
        const result = mergeDocuments([parse('theme T', 'a.play'), parse('theme T', 'b.play')]);
        result.value.themes!.should.have.lengthOf(1);
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0173').should.have.lengthOf(1);
    });
});

describe('when reading what a ui profile says about its theme and appearance', () => {
    const facts = (lines: string[]) => parse(lines.join('\n')).value.designProfiles!;

    it('should read an unquoted and a quoted theme', () => {
        facts(['ui profile A', '  theme Nordic', 'ui profile B', '  theme "Nordic Dark"']).map(profile => profile.theme).should.deep.equal(['Nordic', 'Nordic Dark']);
    });

    it('should keep the first theme of a profile and ignore a blank or invalid one', () => {
        facts(['ui profile A', '  theme ""', '  theme "  "', '  theme two words', '  theme First', '  theme Second']).map(profile => profile.theme).should.deep.equal(['First']);
    });

    it('should read no theme from a profile that names none', () => {
        const [profile] = facts(['ui profile A', '  target platform web']);
        [profile.theme, profile.deprecatedAppearance.length].should.deep.equal([null, 0]);
    });

    it('should not read a theme line nested in another block', () => {
        facts(['ui profile A', '  packages', '    theme Nested', '  theme Outer']).map(profile => profile.theme).should.deep.equal(['Outer']);
    });

    it('should find the deprecated appearance members once each, in the first appearance block only', () => {
        const [profile] = facts(['ui profile A', '  appearance', '    color scheme dark', '    font inter', '    accent "#4F46E5"', '    accent "#000000"', '    corners round', '    font mono',
            '  appearance', '    corners sharp', '  theme T']);
        profile.deprecatedAppearance.map(entry => [entry.member, entry.location.line]).should.deep.equal([['accent', 5], ['corners', 7], ['font', 4]]);
        profile.theme!.should.equal('T');
    });

    it('should keep the members of an appearance block that is not deprecated', () => {
        facts(['ui profile A', '  appearance', '    color scheme dark', '    accent', '    font']).flatMap(profile => profile.deprecatedAppearance).should.deep.equal([]);
    });

    it('should skip a profile whose header is not valid', () => {
        parse('ui profile\n  theme T\nui profile B').value.designProfiles!.map(profile => profile.name).should.deep.equal(['B']);
    });

    it('should step over a fenced block in a profile', () => {
        facts(['ui profile A', '  sample data', '    ```json', 'theme Fenced', '    ```', '  theme Real']).map(profile => profile.theme).should.deep.equal(['Real']);
    });

    it('should report the deprecated members when the design system is checked', () => {
        const result = parse(['ui profile A', '  appearance', '    accent "#4F46E5"', '    corners round', '    font inter'].join('\n'));
        result.diagnostics.map(diagnostic => `${diagnostic.code} ${diagnostic.severity} ${diagnostic.location.line}`).should.deep.equal(['PLAY0929 warning 3', 'PLAY0929 warning 4', 'PLAY0929 warning 5']);
        result.diagnostics[1].message.should.equal("'corners' in a ui profile's appearance is deprecated - set the radius token values in the selected theme; appearance keeps only 'color scheme'");
    });
});
