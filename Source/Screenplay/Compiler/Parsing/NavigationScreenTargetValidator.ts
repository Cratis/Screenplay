// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { NavigationTargetSyntax } from '../Syntax/Outlets';
import { ScreenSyntax } from '../Syntax/Screens';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax, ModuleSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ParserContext } from './ParserContext';

type ScopedScreen = { screen: ScreenSyntax; scope: readonly string[] };

// Item destinations name executable ownership, not a global screen-name search.
export function validateNavigationScreenTargets(application: ApplicationSyntax, context: ParserContext): void {
    createNavigationScreenTargetValidator(application, context).visitApplication(application);
}

export function createNavigationScreenTargetValidator(application: ApplicationSyntax, context: ParserContext): NavigationTargets {
    const screens: ScopedScreen[] = [];
    const features = new Set<string>();
    const free: ScopedScreen[] = (application.screens ?? []).map(screen => ({ screen, scope: [] }));
    const slices = new AuthoringProductionResolver(application).slices;
    const collect = (declarations: readonly FeatureSyntax[], scope: readonly string[]): void => {
        for (const feature of declarations) {
            const path = [...scope, feature.name];
            features.add(path.join('.'));
            free.push(...(feature.screens ?? []).map(screen => ({ screen, scope: path })));
            collect(feature.features, path);
        }
    };
    for (const module of application.modules) {
        free.push(...(module.screens ?? []).map(screen => ({ screen, scope: [module.name] })));
        collect(module.features, [module.name]);
    }
    for (const { slice, scope } of slices) screens.push(...slice.screens.map(screen => ({ screen, scope })));
    const declared = [...free, ...screens];
    const claimed = new Set(screens.map(entry => entry.screen));
    for (const { slice, scope } of slices) {
        for (const adoption of slice.usedScreens ?? []) {
            const resolved = resolve(adoption.screen, scope, declared);
            if (resolved === undefined || claimed.has(resolved.screen)) continue;
            claimed.add(resolved.screen);
            screens.push({ screen: resolved.screen, scope });
        }
    }
    return new NavigationTargets(screens, features, context);
}

function resolve(reference: string, scope: readonly string[], declarations: readonly ScopedScreen[]): ScopedScreen | undefined {
    const parts = reference.split('.');
    const named = declarations.filter(entry => entry.screen.name === parts.at(-1));
    const qualifiers = parts.slice(0, -1);
    if (qualifiers.length > 0) return single(named.filter(entry => entry.scope.slice(-qualifiers.length).join('.') === qualifiers.join('.')));
    for (let depth = scope.length; depth >= 0; depth--) {
        const matches = named.filter(entry => entry.scope.length >= depth && entry.scope.slice(0, depth).join('.') === scope.slice(0, depth).join('.'));
        if (matches.length > 0) return single(matches);
    }
    return undefined;
}

function single(entries: readonly ScopedScreen[]): ScopedScreen | undefined {
    return entries.length === 1 ? entries[0] : undefined;
}

class NavigationTargets extends ScreenplaySyntaxWalker {
    validateItem(target: NavigationTargetSyntax, module: string | null): void {
        const previous = this.module;
        this.module = module ?? undefined;
        this.visitNode(target);
        this.module = previous;
    }
    private module: string | undefined;

    constructor(private readonly screens: readonly ScopedScreen[], private readonly features: ReadonlySet<string>, private readonly context: ParserContext) { super(); }

    override visitModule(module: ModuleSyntax): void {
        this.module = module.name;
        super.visitModule(module);
        this.module = undefined;
    }

    override visitNode(node: SyntaxNode): void {
        if (node.kind !== 'NavigationTargetSyntax') return;
        const target = node as NavigationTargetSyntax;
        if (target.screen == null) return;
        if (this.module !== undefined && target.module !== this.module) return;
        this.validateTarget(target);
    }

    private validateTarget(target: NavigationTargetSyntax): void {
        const path = `${target.module}.${target.feature}`;
        if (!this.features.has(path)) return;
        const matches = this.screens.filter(entry => entry.scope.slice(0, -1).join('.') === path && entry.screen.name === target.screen);
        if (matches.length === 0) this.context.error(DiagnosticCodes.UnknownNavigationScreen, `Navigation target '${path}' has no executable screen '${target.screen}'`, target.location);
        else if (matches.length > 1) this.context.error(DiagnosticCodes.AmbiguousNavigationScreen, `Navigation target '${path}' has more than one executable screen named '${target.screen}'`, target.location);
        else if ((matches[0].screen.fits?.length ?? 0) > 0 && !matches[0].screen.fits!.includes('Page')) this.context.error(DiagnosticCodes.NonPageNavigationScreen, `Navigation screen '${target.screen}' does not fit page`, target.location);
    }
}
