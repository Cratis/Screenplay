// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConditionSyntax } from './Conditions';
import { LogicalOperator } from './Authorization';
import { SyntaxNode } from './SyntaxNode';

// 'audience <Name>' - a group a tenant, user or system caller can be in at runtime. The description is written only when present.
export interface AudienceSyntax extends SyntaxNode {
    readonly kind: 'AudienceSyntax';
    readonly name: string;
    readonly description?: string | null;
}

// 'availability <Name>' - a reusable rule 'available with' names; every 'for', 'when' and 'with' line has to hold.
export interface AvailabilitySyntax extends SyntaxNode {
    readonly kind: 'AvailabilitySyntax';
    readonly name: string;
    readonly requirements: readonly AvailabilityRequirementSyntax[];
    readonly description?: string | null;
}

// 'available for | when | with <requirement>' - a gate on a module, feature, slice, command, query or screen.
export interface AvailableSyntax extends SyntaxNode {
    readonly kind: 'AvailableSyntax';
    readonly requirement: AvailabilityRequirementSyntax;
}

// An audience named by an 'available for'.
export interface AudienceAvailabilityRequirementSyntax extends SyntaxNode {
    readonly kind: 'AudienceAvailabilityRequirementSyntax';
    readonly audience: string;
}

// An availability named by an 'available with'.
export interface AvailabilityReferenceSyntax extends SyntaxNode {
    readonly kind: 'AvailabilityReferenceSyntax';
    readonly availability: string;
}

// The condition of an 'available when', over '$context'.
export interface ConditionAvailabilityRequirementSyntax extends SyntaxNode {
    readonly kind: 'ConditionAvailabilityRequirementSyntax';
    readonly condition: ConditionSyntax;
}

// Two requirements joined by 'and' or 'or'. 'and' binds tighter than 'or', and parentheses group.
export interface LogicalAvailabilityRequirementSyntax extends SyntaxNode {
    readonly kind: 'LogicalAvailabilityRequirementSyntax';
    readonly left: AvailabilityRequirementSyntax;
    readonly operator: LogicalOperator;
    readonly right: AvailabilityRequirementSyntax;
}

export type AvailabilityRequirementSyntax =
    | AudienceAvailabilityRequirementSyntax
    | AvailabilityReferenceSyntax
    | ConditionAvailabilityRequirementSyntax
    | LogicalAvailabilityRequirementSyntax;

/** The leaves of one kind under a requirement, left to right. */
export function availabilityLeaves<TKind extends AvailabilityRequirementSyntax['kind']>(
    requirement: AvailabilityRequirementSyntax,
    kind: TKind): readonly Extract<AvailabilityRequirementSyntax, { kind: TKind }>[] {
    if (requirement.kind === kind) return [requirement as Extract<AvailabilityRequirementSyntax, { kind: TKind }>];
    if (requirement.kind === 'LogicalAvailabilityRequirementSyntax') {
        return [...availabilityLeaves(requirement.left, kind), ...availabilityLeaves(requirement.right, kind)];
    }
    return [];
}
