// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface FileReferenceSyntax extends SyntaxNode {
    readonly kind: 'FileReferenceSyntax';
    readonly path: string;
}

interface ConstraintMembers extends SyntaxNode {
    readonly name: string;
    readonly additionalRules: readonly ConstraintSyntax[];
    readonly releasedBy: readonly string[];
    readonly ignoreCasing: boolean;
    readonly message: string | null;
}

// 'unique <property>[, <property>...] on <Event>'.
export interface UniquePropertyConstraintSyntax extends ConstraintMembers {
    readonly kind: 'UniquePropertyConstraintSyntax';
    readonly property: string;
    readonly additionalProperties: readonly string[];
    readonly event: string;
}

// 'unique event <Event>'.
export interface UniqueEventConstraintSyntax extends ConstraintMembers {
    readonly kind: 'UniqueEventConstraintSyntax';
    readonly event: string;
}

// 'file <path>' - a constraint implemented outside the document.
export interface FileConstraintSyntax extends ConstraintMembers {
    readonly kind: 'FileConstraintSyntax';
    readonly file: FileReferenceSyntax;
}

export type ConstraintSyntax = UniquePropertyConstraintSyntax | UniqueEventConstraintSyntax | FileConstraintSyntax;
