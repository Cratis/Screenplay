// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticArgumentKind } from './DiagnosticArgumentKind';

export type DiagnosticArgument =
    | { readonly kind: DiagnosticArgumentKind.Text | DiagnosticArgumentKind.Keyword; readonly value: string }
    | { readonly kind: DiagnosticArgumentKind.Index; readonly value: number }
    | { readonly kind: DiagnosticArgumentKind.Declaration; readonly value: { readonly kind: string; readonly path: readonly string[] } };

export function diagnosticText(name: string, value: string): Readonly<Record<string, DiagnosticArgument>> {
    return { [name]: { kind: DiagnosticArgumentKind.Text, value } };
}

export function diagnosticKeyword(name: string, value: string): Readonly<Record<string, DiagnosticArgument>> {
    return { [name]: { kind: DiagnosticArgumentKind.Keyword, value } };
}
