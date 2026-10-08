// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A value-free reason why a portable stream identity cannot be formatted. */
export enum StreamIdFormatFailure {
    Empty = 'Empty', NotNfc = 'NotNfc', LoneSurrogate = 'LoneSurrogate', OutOfRange = 'OutOfRange',
    NotIntegral = 'NotIntegral', MalformedUuid = 'MalformedUuid', Arity = 'Arity', Escape = 'Escape', Noncanonical = 'Noncanonical',
}

export type StreamIdScalarKind = 'String' | 'Uuid' | 'Int';
export type StreamIdFormatResult = { readonly value: string; readonly failure: null } | { readonly value: null; readonly failure: StreamIdFormatFailure };

/** Formats text without rewriting its identity. */
export function tryFormatText(value: string): StreamIdFormatResult {
    if (value.length === 0) return { value: null, failure: StreamIdFormatFailure.Empty };
    if (/[\uD800-\uDBFF](?![\uDC00-\uDFFF])|(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]/u.test(value)) return { value: null, failure: StreamIdFormatFailure.LoneSurrogate };
    if (value.normalize('NFC') !== value) return { value: null, failure: StreamIdFormatFailure.NotNfc };
    return { value, failure: null };
}

/** Accepts only authored N, D, B and P UUID forms, returning lowercase D. */
export function tryFormatUuidText(value: string): StreamIdFormatResult {
    if (!/^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}|\{[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\}|\([0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\))(?![\s\S])/i.test(value)) return { value: null, failure: StreamIdFormatFailure.MalformedUuid };
    const hex = value.replace(/[{}()-]/g, '').toLowerCase();
    return { value: `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`, failure: null };
}

/** Formats an integer losslessly; only Double numeric mode imposes the safe-integer bound. */
export function tryFormatInteger(value: bigint | number, doubleBound = typeof value === 'number'): StreamIdFormatResult {
    if (typeof value === 'number' && !Number.isInteger(value)) return { value: null, failure: StreamIdFormatFailure.NotIntegral };
    const integer = BigInt(value);
    if (doubleBound && (integer < -9007199254740991n || integer > 9007199254740991n)) return { value: null, failure: StreamIdFormatFailure.OutOfRange };
    return { value: integer.toString(), failure: null };
}

/** A diagnostic message that never contains the value. */
export function streamIdFailureMessage(failure: StreamIdFormatFailure): string {
    switch (failure) {
        case StreamIdFormatFailure.Empty: return 'A stream id text literal cannot be empty.';
        case StreamIdFormatFailure.NotNfc: return 'A stream id text literal must be Unicode NFC.';
        case StreamIdFormatFailure.LoneSurrogate: return 'A stream id text literal cannot contain lone UTF-16 surrogates.';
        case StreamIdFormatFailure.OutOfRange: return 'A stream id integer literal must be between -9007199254740991 and 9007199254740991 in Double numeric mode.';
        case StreamIdFormatFailure.NotIntegral: return 'A stream id integer must be a finite integral value.';
        case StreamIdFormatFailure.MalformedUuid: return 'A stream id UUID must use N, D, B or P form.';
        case StreamIdFormatFailure.Arity: return 'A composite stream id must match the declared part count of at least two.';
        case StreamIdFormatFailure.Escape: return 'A composite stream id permits only %25 and %7C escapes.';
        case StreamIdFormatFailure.Noncanonical: return 'A composite stream id part must use canonical scalar spelling.';
    }
}

/** Encodes already formatted parts in declaration order. */
export function encodeComposite(parts: readonly string[]): string {
    return parts.map(part => part.replaceAll('%', '%25').replaceAll('|', '%7C')).join('|');
}

/** Decodes once, then checks canonical scalar spelling. Never repairs an identity. */
export function tryDecodeComposite(value: string, kinds: readonly StreamIdScalarKind[], doubleBound = true): { readonly parts: readonly string[] | null; readonly failure: StreamIdFormatFailure | null } {
    const encoded = value.split('|');
    if (kinds.length < 2 || encoded.length !== kinds.length) return { parts: null, failure: StreamIdFormatFailure.Arity };
    const parts: string[] = [];
    for (let index = 0; index < encoded.length; index++) {
        const component = encoded[index];
        if (/%(?!25|7C)/u.test(component)) return { parts: null, failure: StreamIdFormatFailure.Escape };
        const decoded = component.replace(/%25|%7C/g, escape => escape === '%25' ? '%' : '|');
        let formatted: StreamIdFormatResult;
        if (kinds[index] === 'String') formatted = tryFormatText(decoded);
        else if (kinds[index] === 'Uuid') formatted = tryFormatUuidText(decoded);
        else if (/^-?[0-9]+$(?![\s\S])/u.test(decoded)) formatted = tryFormatInteger(BigInt(decoded), doubleBound);
        else return { parts: null, failure: StreamIdFormatFailure.Noncanonical };
        if (formatted.failure !== null) return { parts: null, failure: formatted.failure };
        if (formatted.value !== decoded) return { parts: null, failure: StreamIdFormatFailure.Noncanonical };
        parts.push(decoded);
    }
    return { parts, failure: null };
}
