// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';

// Every syntax node names its kind - the C# record's type name - and where it came from.
//
// A node carries exactly the members of its C# record that this compiler models, under their camelCase
// names, so its canonical JSON (see toSyntaxJson) is the C# SyntaxJson narrowed to those members. A member
// this compiler does not model is absent, never present-but-empty: an empty value would claim the source
// had none.
export interface SyntaxNode {
    readonly kind: string;
    readonly location: SourceLocation;
}
