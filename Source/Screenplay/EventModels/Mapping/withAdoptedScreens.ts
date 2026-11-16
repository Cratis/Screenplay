// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationSyntax, FeatureSyntax, ModuleSyntax, ScreenSyntax, SliceSyntax } from '@cratis/screenplay-compiler';

interface Declared {
    readonly screen: ScreenSyntax;
    readonly scope: readonly string[];
}

// A screen declared free of any slice is drawn with the slice that adopts it, exactly as a screen the slice declares itself.
// The adopting slice resolves the screen the way the compiler does: from the nearest scope outwards, then by a unique name. A
// screen no slice adopts is design-only and is not drawn, because the board draws a slice's screens.
export function withAdoptedScreens(application: ApplicationSyntax): ApplicationSyntax {
    const declared: Declared[] = [
        ...(application.screens ?? []).map(screen => ({ screen, scope: [] })),
        ...application.modules.flatMap(module => [
            ...(module.screens ?? []).map(screen => ({ screen, scope: [module.name] })),
            ...module.features.flatMap(feature => featureScreens(feature, [module.name])),
        ]),
    ];
    if (declared.length === 0) {
        return application;
    }
    const resolve = (name: string, from: readonly string[]): ScreenSyntax | undefined => {
        for (let depth = from.length; depth >= 0; depth--) {
            const scope = from.slice(0, depth);
            const found = declared.filter(entry => entry.screen.name === name && entry.scope.length === scope.length && entry.scope.every((part, index) => part === scope[index]));
            if (found.length === 1) return found[0].screen;
        }
        const everywhere = declared.filter(entry => entry.screen.name === name);
        return everywhere.length === 1 ? everywhere[0].screen : undefined;
    };
    const adopt = (slice: SliceSyntax, from: readonly string[]): SliceSyntax => {
        const adopted = (slice.usedScreens ?? []).map(used => resolve(used.screen, from)).filter((screen): screen is ScreenSyntax => screen !== undefined);
        return adopted.length === 0 ? slice : { ...slice, screens: [...slice.screens, ...adopted.filter(screen => !slice.screens.includes(screen))] };
    };
    const inFeature = (feature: FeatureSyntax, from: readonly string[]): FeatureSyntax => ({
        ...feature,
        features: feature.features.map(child => inFeature(child, [...from, child.name])),
        slices: feature.slices.map(slice => adopt(slice, from)),
    });
    const inModule = (module: ModuleSyntax): ModuleSyntax => ({
        ...module,
        features: module.features.map(feature => inFeature(feature, [module.name, feature.name])),
    });
    return { ...application, modules: application.modules.map(inModule) };
}

function featureScreens(feature: FeatureSyntax, scope: readonly string[]): Declared[] {
    const here = [...scope, feature.name];
    return [...(feature.screens ?? []).map(screen => ({ screen, scope: here })), ...feature.features.flatMap(child => featureScreens(child, here))];
}
