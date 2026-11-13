// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertySyntax } from './Declarations';
import { CodeBlockSyntax, FileReferenceSyntax, ImplementationSyntax } from './Implementations';
import { SyntaxNode } from './SyntaxNode';

export interface SystemSyntax extends SyntaxNode {
    readonly kind: 'SystemSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly abilities: readonly AbilitySyntax[];
    /** The extension package realizing the system; written only when present. Realization metadata. */
    readonly extension?: SystemExtensionSyntax;
    /** The settings the model maps for the system; written only when there are any. Realization metadata. */
    readonly settings?: readonly SystemSettingSyntax[];
    /** The activation override; written only when present. Realization metadata. */
    readonly activation?: SystemActivationSyntax;
}

/** A top-level named secret that system settings map from; the model never holds its value. */
export interface SecretSyntax extends SyntaxNode {
    readonly kind: 'SecretSyntax';
    readonly name: string;
    readonly description: string | null;
}

/** The `extension "<reference>" [version <range>]` line of a system. */
export interface SystemExtensionSyntax extends SyntaxNode {
    readonly kind: 'SystemExtensionSyntax';
    readonly reference: string;
    readonly version: string | null;
}

/** A `setting <name> from secret <Name>` or `setting <name> = <literal>` line of a system. */
export interface SystemSettingSyntax extends SyntaxNode {
    readonly kind: 'SystemSettingSyntax';
    readonly name: string;
    readonly secret: string | null;
    readonly value: string | null;
    readonly valueKind: string | null;
}

/** The `activation always` or `activation on demand [idle after <n> <unit>]` line of a system. */
export interface SystemActivationSyntax extends SyntaxNode {
    readonly kind: 'SystemActivationSyntax';
    readonly always: boolean;
    readonly idleAmount: number | null;
    readonly idleUnit: string | null;
}

/** One typed ability a system can be asked to perform; `effectClass` is null when omitted, which means `once`. */
export interface AbilitySyntax extends SyntaxNode {
    readonly kind: 'AbilitySyntax';
    readonly name: string;
    readonly effectClass: string | null;
    readonly inputs: readonly PropertySyntax[];
    readonly returns: readonly PropertySyntax[];
    readonly outcomes: readonly AbilityOutcomeSyntax[];
    readonly description: string | null;
    readonly answersLater: boolean;
    readonly implementation: ImplementationSyntax | null;
    readonly file: FileReferenceSyntax | null;
    readonly code: CodeBlockSyntax | null;
}

/** A named business result of an ability other than success; it carries no payload. */
export interface AbilityOutcomeSyntax extends SyntaxNode {
    readonly kind: 'AbilityOutcomeSyntax';
    readonly name: string;
    readonly description: string | null;
}

/** The effect classes an ability can declare. */
export const abilityEffectClasses = ['safe', 'idempotent', 'once'] as const;

/** The outcome names an ability cannot declare because invocation sites already use them as outlets. */
export const reservedOutcomeNames: ReadonlySet<string> = new Set(['next', 'refused', 'failed', 'returned', 'expired']);
