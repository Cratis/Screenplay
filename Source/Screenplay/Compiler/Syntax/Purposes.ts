// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export interface PurposeSyntax extends SyntaxNode {
    readonly kind: 'PurposeSyntax';
    readonly name: string;
    readonly description: string | null;
    readonly basis: string | null;
    readonly basisReference: string | null;
    readonly interest: string | null;
    readonly condition: string | null;
    readonly conditionReference: string | null;
    readonly authorization: string | null;
    readonly subjects: readonly string[];
    readonly retention: string | null;
    readonly recipients: readonly string[];
    readonly transfers: readonly PurposeTransferSyntax[];
    readonly erasureException: string | null;
}

export interface PurposeReferenceSyntax extends SyntaxNode {
    readonly kind: 'PurposeReferenceSyntax';
    readonly name: string;
}

export interface PurposeTransferSyntax extends SyntaxNode {
    readonly kind: 'PurposeTransferSyntax';
    readonly destination: string;
    readonly safeguard: string;
}
