// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax, PropertyMappingSyntax } from './Expressions';
import { TagSyntax } from './Declarations';
import { SyntaxNode } from './SyntaxNode';
import { SourceOptions } from './SourceOptions';

export interface CaptureTranslationSyntax extends SyntaxNode {
    readonly kind: 'CaptureTranslationSyntax';
    readonly from: string;
    readonly to: string;
}

export interface CaptureMapEntrySyntax extends SyntaxNode {
    readonly kind: 'CaptureMapEntrySyntax';
    readonly property: string;
    readonly source: ExpressionSyntax;
    readonly translations: readonly CaptureTranslationSyntax[];
}

export interface CaptureSplitSyntax extends SyntaxNode {
    readonly kind: 'CaptureSplitSyntax';
    readonly source: ExpressionSyntax;
    readonly separator: string;
    readonly targets: readonly string[];
}

export type CaptureMapOperationSyntax = CaptureMapEntrySyntax | CaptureSplitSyntax;

export interface CaptureWhenSyntax extends SyntaxNode {
    readonly kind: 'CaptureWhenSyntax';
    readonly syntaxKind: 'Added' | 'Removed' | 'Changed' | 'Expression' | 'PropertyChanged' | 'ValueTransition' | 'LogicalOr' | 'LogicalAnd';
    readonly properties: readonly string[];
    readonly fromValue: string | null;
    readonly toValue: string | null;
    readonly expression: string | null;
}

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
    readonly when?: CaptureWhenSyntax | null;
    readonly mappings?: readonly PropertyMappingSyntax[];
    readonly tags?: readonly TagSyntax[];
}

// 'children <collection> identified by <key>'.
export interface CaptureChildrenSyntax extends SyntaxNode {
    readonly kind: 'CaptureChildrenSyntax';
    readonly property: string;
    readonly map?: readonly CaptureMapOperationSyntax[];
    readonly identifiedBy: string;
    readonly appends: readonly CaptureAppendSyntax[];
}

// 'nested <Property>'.
export interface CaptureNestedSyntax extends SyntaxNode {
    readonly kind: 'CaptureNestedSyntax';
    readonly property: string;
    readonly map?: readonly CaptureMapOperationSyntax[];
    readonly appends: readonly CaptureAppendSyntax[];
}

// 'capture <Name>' - how a translation turns what an outside system holds into events. How it maps the
// values it reads is not modeled.
export interface CaptureSyntax extends SyntaxNode {
    readonly kind: 'CaptureSyntax';
    readonly sourceOptions?: SourceOptions;
    readonly name: string;
    readonly map?: readonly CaptureMapOperationSyntax[];
    readonly source: CaptureSourceSyntax | null;
    readonly key: string | null;
    readonly appends: readonly CaptureAppendSyntax[];
    readonly children: readonly CaptureChildrenSyntax[];
    readonly nested: readonly CaptureNestedSyntax[];
}
