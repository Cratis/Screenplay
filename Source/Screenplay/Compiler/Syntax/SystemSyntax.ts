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
