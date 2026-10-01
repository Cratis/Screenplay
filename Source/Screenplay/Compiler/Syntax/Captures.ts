// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// One setting of where a capture reads from - 'api LegacyInvoicingApi', 'poll 5m'.
export interface CaptureSourceSettingSyntax extends SyntaxNode {
    readonly kind: 'CaptureSourceSettingSyntax';
    readonly name: string;
    readonly value: string;
}

// 'source <kind>' and its settings. The C# member Kind is written 'syntaxKind' so it does not collide with
// the discriminator.
export interface CaptureSourceSyntax extends SyntaxNode {
    readonly kind: 'CaptureSourceSyntax';
    readonly syntaxKind: string;
    readonly settings: readonly CaptureSourceSettingSyntax[];
}

// 'append <Event>' - an event a capture appends when what it reads changes. When it is appended and what
// it carries are not modeled.
export interface CaptureAppendSyntax extends SyntaxNode {
    readonly kind: 'CaptureAppendSyntax';
    readonly event: string;
}

// 'children <collection> identified by <key>'.
export interface CaptureChildrenSyntax extends SyntaxNode {
    readonly kind: 'CaptureChildrenSyntax';
    readonly property: string;
    readonly identifiedBy: string;
    readonly appends: readonly CaptureAppendSyntax[];
}

// 'nested <Property>'.
export interface CaptureNestedSyntax extends SyntaxNode {
    readonly kind: 'CaptureNestedSyntax';
    readonly property: string;
    readonly appends: readonly CaptureAppendSyntax[];
}

// 'capture <Name>' - how a translation turns what an outside system holds into events. How it maps the
// values it reads is not modeled.
export interface CaptureSyntax extends SyntaxNode {
    readonly kind: 'CaptureSyntax';
    readonly name: string;
    readonly source: CaptureSourceSyntax | null;
    readonly key: string | null;
    readonly appends: readonly CaptureAppendSyntax[];
    readonly children: readonly CaptureChildrenSyntax[];
    readonly nested: readonly CaptureNestedSyntax[];
}
