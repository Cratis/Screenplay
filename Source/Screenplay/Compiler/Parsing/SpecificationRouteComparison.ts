// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeRefSyntax } from '../Syntax/Declarations';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';
import { EventSourceResolutionKind } from '../Syntax/EventSources';
import { canonicalExactText } from '../Syntax/ExactMathFacts';
import { ExpressionSyntax } from '../Syntax/Expressions';
import { SpecificationStreamSyntax, SpecificationNoStreamSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax } from '../Syntax/Structure';
import { compatibleValue, uniqueByName } from './ResponseValidator';

export function formatSpecificationStreamId(expression: ExpressionSyntax | null | undefined, type: TypeRefSyntax, application: ApplicationSyntax): string | null {
    const concepts = uniqueByName(application.concepts);
    const composites = uniqueByName(application.types);
    const properties = new Map([...composites].map(([name, type]) => [name, uniqueByName(type.properties)]));
    if (expression?.kind !== 'LiteralExpressionSyntax' || !compatibleValue(expression, type, concepts, properties)) return null;
    const primitive = concepts.get(type.name)?.type ?? type.name;
    const value = expression.value;
    if (primitive === 'String' && typeof value === 'string') return value;
    if (primitive === 'Uuid' && typeof value === 'string') {
        const hex = value.replace(/[{}()-]/g, '').toLowerCase();
        return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
    }
    if (primitive === 'Int') {
        if (typeof value === 'number') return BigInt(value).toString();
        if (typeof value === 'object' && value !== null && value.literalType === 'ExactNumber') return canonicalExactText(value);
    }
    return null;
}

export function matchesSpecificationRoute(given: SpecificationStreamSyntax | null | undefined, expected: SpecificationStreamSyntax | null | undefined, noStream: SpecificationNoStreamSyntax | null | undefined, application: ApplicationSyntax): boolean | null {
    if (noStream != null) return given == null;
    if (expected == null) return true;
    if (given == null) return false;
    const catalog = new EventSourceCatalog(application);
    const actual = catalog.resolve(given.eventSource, given.stream);
    const target = catalog.resolve(expected.eventSource, expected.stream);
    if (actual.kind !== EventSourceResolutionKind.Unique || target.kind !== EventSourceResolutionKind.Unique) return null;
    if (actual.sources[0] !== target.sources[0] || actual.streams[0] !== target.streams[0]) return false;
    const stream = target.streams[0];
    const compare = (left: ExpressionSyntax | null | undefined, right: ExpressionSyntax | null | undefined, type: TypeRefSyntax): boolean | null => {
        const first = formatSpecificationStreamId(left, type, application);
        const second = formatSpecificationStreamId(right, type, application);
        return first === null || second === null ? null : first === second;
    };
    if (stream.streamIdParts.length > 0) {
        if (given.streamId !== null || expected.streamId !== null || given.streamIdParts.length !== stream.streamIdParts.length || expected.streamIdParts.length !== stream.streamIdParts.length) return null;
        const comparisons = stream.streamIdParts.map(part => {
            const left = given.streamIdParts.filter(mapping => mapping.property === part.name);
            const right = expected.streamIdParts.filter(mapping => mapping.property === part.name);
            return left.length === 1 && right.length === 1 ? compare(left[0].source, right[0].source, part.type) : null;
        });
        return comparisons.includes(false) ? false : comparisons.includes(null) ? null : true;
    }
    if (given.streamIdParts.length > 0 || expected.streamIdParts.length > 0) return null;
    if (stream.streamId === null) return given.streamId === null && expected.streamId === null ? true : null;
    return compare(given.streamId?.source, expected.streamId?.source, stream.streamId);
}
