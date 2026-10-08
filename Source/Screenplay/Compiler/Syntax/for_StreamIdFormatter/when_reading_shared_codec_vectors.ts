// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { encodeComposite, streamIdFailureMessage, StreamIdFormatFailure, StreamIdScalarKind, tryDecodeComposite, tryFormatInteger, tryFormatText, tryFormatUuidText } from '../StreamIdFormatter';

interface Input { input?: string; utf16?: string[] }
interface Scalar extends Input { kind: StreamIdScalarKind; doubleBound?: boolean; canonical?: string; failure?: StreamIdFormatFailure }
interface Composite { parts: string[]; kinds: StreamIdScalarKind[]; encoded: string }
interface Decode extends Input { kinds: StreamIdScalarKind[]; failure: StreamIdFormatFailure }
const vectors: { scalars: Scalar[]; composites: Composite[]; decode: Decode[]; messages: Record<StreamIdFormatFailure, string> } = JSON.parse(readFileSync(new URL('../../Conformance/stream-id-codec.json', import.meta.url), 'utf8'));
const input = (value: Input) => value.input ?? String.fromCharCode(...value.utf16!.map(unit => parseInt(unit, 16)));

describe('when reading shared stream id codec vectors', () => {
    it.each(vectors.scalars)('should format or refuse scalar $kind $input', vector => {
        const value = input(vector);
        const result = vector.kind === 'String' ? tryFormatText(value) : vector.kind === 'Uuid' ? tryFormatUuidText(value)
            : tryFormatInteger(value.includes('.') ? Number(value) : BigInt(value), vector.doubleBound);
        expect(result).toEqual({ value: vector.canonical ?? null, failure: vector.failure ?? null });
        if (vector.kind === 'Int' && vector.doubleBound) expect(tryFormatInteger(Number(value))).toEqual(result);
    });
    it.each(vectors.composites)('should encode and round trip $encoded in declaration order', vector => {
        expect(encodeComposite(vector.parts)).toBe(vector.encoded);
        const decoded = tryDecodeComposite(vector.encoded, vector.kinds);
        expect(decoded).toEqual({ parts: vector.parts, failure: null });
        expect(encodeComposite(decoded.parts!)).toBe(vector.encoded);
    });
    it.each(vectors.decode)('should refuse noncanonical composite $input', vector => expect(tryDecodeComposite(input(vector), vector.kinds)).toEqual({ parts: null, failure: vector.failure }));
    it('should share value-free messages and keep exact-mode decoding unbounded', () => {
        for (const failure of Object.values(StreamIdFormatFailure)) expect(streamIdFailureMessage(failure)).toBe(vectors.messages[failure]);
        expect(tryDecodeComposite('9007199254740993|b', ['Int', 'String'], false)).toEqual({ parts: ['9007199254740993', 'b'], failure: null });
        for (const value of [NaN, Infinity, -Infinity]) expect(tryFormatInteger(value).failure).toBe(StreamIdFormatFailure.NotIntegral);
    });
});
