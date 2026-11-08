// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax } from '../Syntax/Structure';
import { ThemeSyntax } from '../Syntax/DesignSystemSyntax';
import { DesignThemeEvaluation, ResolvedDesignComponent } from './DesignThemeEvaluation';

// A theme resolved in one mode: every token it gives a value down to a literal, and every component part through those tokens.
export interface ResolvedDesignTheme {
    readonly designSystem: string;
    readonly theme: string;
    readonly mode: string | null;
    readonly tokens: ReadonlyMap<string, string>;
    readonly components: readonly ResolvedDesignComponent[];
    readonly problems: readonly string[];
}

// Resolves a theme against the application's design system in a mode. Null when the application declares no design system, the
// theme does not implement it, or the mode is not one it declares. The C# DesignSystemResolver also selects the theme and mode from
// a ui profile; this compiler does not model profiles, so the caller names them.
export function resolveDesignTheme(application: ApplicationSyntax, theme: ThemeSyntax, mode: string | null): ResolvedDesignTheme | null {
    const designSystem = application.designSystems?.[0];
    if (designSystem === undefined || theme.implements !== designSystem.name || (mode !== null && !designSystem.modes.includes(mode))) return null;
    const evaluation = new DesignThemeEvaluation(designSystem, theme, mode);
    const components = evaluation.evaluate();
    return { designSystem: designSystem.name, theme: theme.name, mode, tokens: evaluation.tokens, components, problems: evaluation.problems.map(problem => problem.message) };
}
