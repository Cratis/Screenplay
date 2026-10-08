// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax } from './Declarations';
import { PropertyMappingSyntax } from './Expressions';
import { SyntaxNode } from './SyntaxNode';

// Authored classification is independent of an event production's identity destination.
export interface CommandStreamSyntax extends SyntaxNode {
    readonly kind: 'CommandStreamSyntax';
    readonly eventSource: string;
    readonly stream: string;
    readonly streamId: PropertyMappingSyntax | null;
    readonly streamIdParts: PropertyMappingSyntax[];
    readonly propertyCandidate: PropertySyntax | null;
    readonly referenceLocation?: SourceLocation;
    readonly referenceLength?: number;
}
