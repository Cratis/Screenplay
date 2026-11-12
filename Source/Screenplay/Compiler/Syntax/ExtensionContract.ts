// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax } from './Declarations';
import { AbilitySyntax, SystemActivationSyntax } from './SystemSyntax';

/** How far an extension setting's value travels. */
export type ExtensionSettingExposure = 'backend' | 'public' | 'secret';

/** The component kinds of a contract. */
export const extensionComponentKinds: readonly string[] = ['react', 'mount', 'screenplay'];

/** The platforms a component or package can support. */
export const extensionPlatforms: readonly string[] = ['web', 'ios', 'android', 'desktop'];

/** The requirement kinds of a requires line. */
export const extensionRequirementKinds: readonly string[] = ['stage', 'screenplay', 'platform', 'runtime'];

export interface ExtensionImage {
    readonly path: string;
    readonly caption: string | null;
    readonly location: SourceLocation;
}

export interface ExtensionRequirement {
    readonly kind: string;
    readonly value: string;
    readonly location: SourceLocation;
}

export interface ExtensionSetting {
    readonly name: string;
    readonly exposure: ExtensionSettingExposure;
    readonly location: SourceLocation;
}

export interface ExtensionTrigger {
    readonly name: string;
    readonly payload: readonly PropertySyntax[];
    readonly location: SourceLocation;
}

export interface ExtensionSignal {
    readonly name: string;
    readonly values: readonly PropertySyntax[];
    readonly location: SourceLocation;
}

export interface ExtensionComponent {
    readonly name: string;
    readonly kind: string | null;
    readonly platforms: readonly string[];
    readonly placeholder: string | null;
    readonly properties: readonly PropertySyntax[];
    readonly signals: readonly ExtensionSignal[];
    readonly invokes: readonly string[];
    readonly location: SourceLocation;
}

export interface ExtensionBackend {
    readonly runtime: string;
    readonly version: string;
    readonly assembly: string;
    readonly location: SourceLocation;
}

/** The parsed contract.play of an extension package: its manifest and everything it contributes. */
export interface ExtensionContract {
    readonly name: string;
    readonly displayName: string | null;
    readonly description: string | null;
    readonly icon: string | null;
    readonly images: readonly ExtensionImage[];
    readonly requirements: readonly ExtensionRequirement[];
    readonly activation: SystemActivationSyntax | null;
    readonly settings: readonly ExtensionSetting[];
    readonly abilities: readonly AbilitySyntax[];
    readonly triggers: readonly ExtensionTrigger[];
    readonly components: readonly ExtensionComponent[];
    readonly backend: ExtensionBackend | null;
    readonly frontend: string | null;
    readonly location: SourceLocation;
}
