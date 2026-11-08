// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { designContrast } from '../Design/DesignColor';
import { DesignProblem, DesignThemeEvaluation, ResolvedDesignComponent } from '../Design/DesignThemeEvaluation';
import { categoriesForPart, designParts, designStates } from '../Design/DesignVocabulary';
import { DesignAssignmentSyntax, DesignComponentSyntax, DesignSystemSyntax, ThemeSyntax } from '../Syntax/DesignSystemSyntax';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { SourceLocation } from '../Diagnostics/SourceLocation';

const sameLocation = (left: SourceLocation, right: SourceLocation): boolean => left.path === right.path && left.line === right.line && left.column === right.column;

// Checks the design system of an application against its themes: the design system itself, the themes that implement it, the
// default theme, and every theme's values in every mode - including the contrast the design system's rules ask for. The port of
// the C# DesignSystemValidator; the deprecated appearance warning (PLAY0929) needs only the profile and is reported before the design system is looked for.
export function validateDesignSystem(application: ApplicationSyntax, context: ParserContext): void {
    const designSystem = application.designSystems?.[0];
    const themes = application.themes ?? [];
    for (const theme of themes.filter(candidate => candidate.implements !== undefined && (designSystem === undefined || designSystem.name !== candidate.implements))) {
        const message = designSystem === undefined
            ? `Theme '${theme.name}' implements '${theme.implements}', but the application declares no design system`
            : `Theme '${theme.name}' implements '${theme.implements}', but the application's design system is '${designSystem.name}'`;
        context.error(DiagnosticCodes.ThemeImplementsUnknownDesignSystem, message, theme.location);
    }
    validateAppearance(application, context);
    if (designSystem === undefined) return;
    validateDeclaredDesignSystem(designSystem, context);
    validateDefaultTheme(designSystem, themes, context);
    for (const theme of themes.filter(candidate => candidate.implements === designSystem.name)) validateTheme(designSystem, theme, context);
    for (const profile of application.designProfiles ?? []) {
        const selected = profile.theme === null ? undefined : themes.find(theme => theme.name === profile.theme);
        if (selected !== undefined && selected.implements !== designSystem.name) {
            context.error(DiagnosticCodes.ProfileThemeDoesNotImplementDesignSystem,
                `The ui profile '${profile.name}' selects theme '${profile.theme}', which does not implement the design system '${designSystem.name}'`, profile.location);
        }
    }
}

const appearanceReplacements: Readonly<Record<string, string>> = {
    accent: 'set a palette entry or color token in the selected theme',
    corners: 'set the radius token values in the selected theme',
    font: 'set the font token values in the selected theme',
};

function validateAppearance(application: ApplicationSyntax, context: ParserContext): void {
    for (const profile of application.designProfiles ?? []) {
        for (const { member, location } of profile.deprecatedAppearance) {
            context.warning(DiagnosticCodes.DeprecatedAppearanceMember,
                `'${member}' in a ui profile's appearance is deprecated - ${appearanceReplacements[member]}; appearance keeps only 'color scheme'`, location);
        }
    }
}

function validateDeclaredDesignSystem(designSystem: DesignSystemSyntax, context: ParserContext): void {
    const declared = new Map<string, string>();
    for (const category of designSystem.tokens) for (const token of category.tokens) declared.set(`${category.category}.${token.name}`, category.category);
    for (const component of designSystem.components) validateComponent(component, declared, context);
    for (const rule of designSystem.rules) {
        for (const part of [rule.foreground, rule.background].filter(candidate => !designParts.includes(candidate))) {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Unknown part '${part}' in a contrast rule - expected one of ${designParts.join(', ')}`, rule.location);
        }
    }
}

function validateComponent(component: DesignComponentSyntax, declared: Map<string, string>, context: ParserContext): void {
    for (const part of component.parts.filter(candidate => !designParts.includes(candidate))) {
        context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Unknown part '${part}' on component '${component.name}' - expected one of ${designParts.join(', ')}`, component.location);
    }
    for (const variant of component.variants) {
        validateParts(component, variant.parts, declared, context);
        for (const state of variant.states) {
            if (!designStates.includes(state.name)) {
                context.error(DiagnosticCodes.InvalidDesignSystemDeclaration,
                    `Unknown state '${state.name}' on '${component.name}' variant '${variant.name}' - expected one of ${designStates.join(', ')}`, state.location);
            }
            validateParts(component, state.parts, declared, context);
        }
    }
}

function validateParts(component: DesignComponentSyntax, parts: readonly DesignAssignmentSyntax[], declared: Map<string, string>, context: ParserContext): void {
    for (const part of parts) {
        if (!component.parts.includes(part.name)) {
            context.error(DiagnosticCodes.InvalidDesignSystemDeclaration, `Component '${component.name}' does not declare the part '${part.name}'`, part.location);
            continue;
        }
        const category = declared.get(part.value);
        if (category === undefined) {
            context.error(DiagnosticCodes.UnresolvedDesignReference, `'${part.name} = ${part.value}' on '${component.name}' names no token of the design system`, part.location);
            continue;
        }
        const accepted = categoriesForPart(part.name);
        if (accepted.length > 0 && !accepted.includes(category)) {
            context.error(DiagnosticCodes.DesignPartCategoryMismatch, `The part '${part.name}' of '${component.name}' takes a ${accepted.join(' or ')} token, not '${part.value}'`, part.location);
        }
    }
}

function validateDefaultTheme(designSystem: DesignSystemSyntax, themes: readonly ThemeSyntax[], context: ParserContext): void {
    const name = designSystem.defaultTheme;
    if (name === null) {
        context.error(DiagnosticCodes.InvalidDefaultTheme, `Design system '${designSystem.name}' declares no 'default theme' - a ui profile without a theme resolves to it`, designSystem.location);
        return;
    }
    const theme = themes.find(candidate => candidate.name === name);
    if (theme === undefined) {
        context.error(DiagnosticCodes.InvalidDefaultTheme, `The default theme '${name}' of design system '${designSystem.name}' is not declared`, designSystem.location);
    } else if (theme.implements !== designSystem.name) {
        context.error(DiagnosticCodes.InvalidDefaultTheme, `The default theme '${name}' does not implement design system '${designSystem.name}' - add 'implements ${designSystem.name}'`, theme.location);
    }
}

function validateTheme(designSystem: DesignSystemSyntax, theme: ThemeSyntax, context: ParserContext): void {
    const declared = new Set(designSystem.tokens.flatMap(category => category.tokens.map(token => `${category.category}.${token.name}`)));
    for (const block of (theme.modeValues ?? []).filter(candidate => !designSystem.modes.includes(candidate.mode))) {
        context.error(DiagnosticCodes.UnknownDesignMode,
            `Theme '${theme.name}' has a block for the mode '${block.mode}', which design system '${designSystem.name}' does not declare (modes: ${designSystem.modes.join(', ')})`, block.location);
    }
    for (const assignment of [...(theme.values ?? []), ...(theme.modeValues ?? []).flatMap(block => block.values)].filter(candidate => !declared.has(candidate.name))) {
        context.error(DiagnosticCodes.ThemeValueForUndeclaredToken, `Theme '${theme.name}' assigns '${assignment.name}', which design system '${designSystem.name}' does not declare`, assignment.location);
    }
    const modes: (string | null)[] = designSystem.modes.length > 0 ? [...designSystem.modes] : [null];
    const problems: DesignProblem[] = [];
    for (const mode of modes) {
        const evaluation = new DesignThemeEvaluation(designSystem, theme, mode);
        const components = evaluation.evaluate();
        for (const problem of evaluation.problems) {
            if (!problems.some(known => known.code === problem.code && sameLocation(known.location, problem.location))) problems.push(problem);
        }
        const colorTokens = designSystem.tokens.filter(category => category.category === 'color')
            .flatMap(category => category.tokens.map(token => `${category.category}.${token.name}`));
        for (const token of colorTokens.filter(address => !evaluation.hasValue(address))) {
            context.error(DiagnosticCodes.ColorTokenMissingModeValue, `Theme '${theme.name}' gives '${token}' no value${mode === null ? '' : ` in the mode '${mode}'`}`, theme.location);
        }
        reportContrast(designSystem, theme, mode, components, context);
    }
    for (const problem of problems) context.error(problem.code, problem.message, problem.location);
}

function reportContrast(designSystem: DesignSystemSyntax, theme: ThemeSyntax, mode: string | null, components: readonly ResolvedDesignComponent[], context: ParserContext): void {
    for (const rule of designSystem.rules) {
        const minimum = Number.parseFloat(rule.minimum);
        for (const component of components) {
            for (const variant of component.variants) {
                const states: [string, ReadonlyMap<string, string>][] = [['default', variant.parts], ...[...variant.states].filter(([state]) => state !== 'disabled')];
                for (const [state, parts] of states) {
                    const foreground = parts.get(rule.foreground);
                    const background = parts.get(rule.background);
                    if (foreground === undefined || background === undefined) continue;
                    const ratio = designContrast(foreground, background);
                    if (ratio === undefined || ratio >= minimum) continue;
                    const where = state === 'default' ? `${component.name} ${variant.name}` : `${component.name} ${variant.name} ${state}`;
                    context.warning(DiagnosticCodes.ContrastBelowRule,
                        `In theme '${theme.name}'${mode === null ? '' : ` (${mode})`}, ${where}: ${rule.foreground} ${foreground} on ${rule.background} ${background} has contrast ${Number(ratio.toFixed(2))}, below the required ${rule.minimum}`,
                        theme.location);
                }
            }
        }
    }
}
