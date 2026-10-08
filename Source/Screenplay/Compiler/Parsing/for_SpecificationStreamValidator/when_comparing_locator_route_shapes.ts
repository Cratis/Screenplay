// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ExpressionSyntax } from '../../Syntax/Expressions';
import { SpecificationStreamSyntax } from '../../Syntax/Specifications';
import { formatSpecificationStreamId, matchesSpecificationRoute } from '../SpecificationRouteComparison';

const application = parse('concept Period : Int\neventsource A\n  identifier String\n  stream Composite\n    streamId\n      one String\n      two String\n  stream Scalar\n    streamId String\n  stream Plain\neventsource B\n  stream Plain').value;
const location = { line: 1, column: 1 };
const literal = (value: string | number): ExpressionSyntax => ({ kind: 'LiteralExpressionSyntax', value, location });
const mapping = (property: string, value: string) => ({ kind: 'PropertyMappingSyntax' as const, property, source: literal(value), location });
const route = (stream: string): SpecificationStreamSyntax => ({ kind: 'SpecificationStreamSyntax', eventSource: 'A', stream, streamId: null, streamIdParts: [], location, referenceLocation: location, referenceLength: stream.length + 2 });
const composite: SpecificationStreamSyntax = { ...route('Composite'), streamIdParts: [mapping('one', 'a'), mapping('two', 'b')] };
const noStream = { kind: 'SpecificationNoStreamSyntax' as const, location };
const same = (given: SpecificationStreamSyntax | null, expected: SpecificationStreamSyntax | null, unrouted = false) => matchesSpecificationRoute(given, expected, unrouted ? noStream : null, application);

describe('when comparing locator route shapes', () => {
    it('should retain wildcard and explicit unrouted meanings', () => {
        expect(same(null, null)).toBe(true);
        expect(same(composite, null)).toBe(true);
        expect(same(null, composite)).toBe(false);
        expect(same(composite, null, true)).toBe(false);
        expect(same(null, null, true)).toBe(true);
    });
    it('should distinguish source and stream identities', () => {
        expect(same(route('Plain'), { ...route('Plain'), eventSource: 'B' })).toBe(false);
        expect(same(route('Plain'), route('Scalar'))).toBe(false);
        expect(same({ ...composite, eventSource: 'Missing' }, composite)).toBeNull();
    });
    it('should keep malformed composite shapes undecidable', () => {
        expect(same({ ...composite, streamId: mapping('streamId', 'joined') }, composite)).toBeNull();
        expect(same(composite, { ...composite, streamId: mapping('streamId', 'joined') })).toBeNull();
        expect(same({ ...composite, streamIdParts: [] }, composite)).toBeNull();
        expect(same(composite, { ...composite, streamIdParts: [] })).toBeNull();
        expect(same({ ...composite, streamIdParts: [mapping('one', 'a'), mapping('one', 'a')] }, composite)).toBeNull();
    });
    it('should keep malformed scalar and unkeyed shapes undecidable', () => {
        expect(same({ ...route('Scalar'), streamIdParts: composite.streamIdParts }, route('Scalar'))).toBeNull();
        expect(same(route('Scalar'), { ...route('Scalar'), streamIdParts: composite.streamIdParts })).toBeNull();
        expect(same(route('Plain'), { ...route('Plain'), streamId: mapping('streamId', 'extra') })).toBeNull();
        expect(same(route('Plain'), route('Plain'))).toBe(true);
        expect(same(route('Scalar'), route('Scalar'))).toBeNull();
    });
    it('should keep unrelated composite value declarations out of scalar identity formatting', () => {
        const model = parse('type Detail\n  value String\n  number Int\nconcept Key : Uuid').value;
        const type = { kind: 'TypeRefSyntax' as const, name: 'Key', isOptional: false, isCollection: false, location };
        expect(formatSpecificationStreamId(literal('3FA85F64-5717-4562-B3FC-2C963F66AFA6'), type, model)).toBe('3fa85f64-5717-4562-b3fc-2c963f66afa6');
    });
    it('should format only known portable literal types', () => {
        const type = (name: string) => ({ kind: 'TypeRefSyntax' as const, name, isOptional: false, isCollection: false, location });
        expect(formatSpecificationStreamId(undefined, type('String'), application)).toBeNull();
        expect(formatSpecificationStreamId({ kind: 'PathExpressionSyntax', path: 'value', location }, type('String'), application)).toBeNull();
        expect(formatSpecificationStreamId(literal(1), type('String'), application)).toBeNull();
        expect(formatSpecificationStreamId(literal('value'), type('Unknown'), application)).toBeNull();
        expect(formatSpecificationStreamId(literal(-0), type('Period'), application)).toBe('0');
        expect(formatSpecificationStreamId({ kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '9007199254740991' }, location }, type('Period'), application)).toBe('9007199254740991');
    });
});
