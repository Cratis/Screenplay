// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DocumentSymbols } from './symbols';

const exampleUuid = '"0f8fad5b-d9cb-469f-a165-70867728950e"';

const primitiveExamples: Record<string, string> = {
    Uuid: exampleUuid,
    String: '"Example"',
    Int: '1',
    Decimal: '1.5',
    Bool: 'true',
    Date: '"2026-01-15"',
    DateTime: '"2026-01-15T10:00:00Z"',
};

// An example value to write in a specification for a property of the given type, resolving concepts
// down to the primitive they wrap. Collections and unknown types fall back to a quoted string.
export function exampleValueFor(type: string, symbols: DocumentSymbols): string {
    const bare = type.replace(/\s+optional$/, '').replace(/\?$/, '');
    if (bare.endsWith('[]')) return '"Example"';
    const primitive = symbols.concepts.find(concept => concept.name === bare)?.primitive ?? bare;
    return primitiveExamples[primitive] ?? '"Example"';
}
