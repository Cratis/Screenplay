// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pattern } from './patterns';

// .NET \w admits Lu, Ll, Lt, Lm, Lo, Mn, Nd and Pc, evaluated per UTF-16 code unit.
// The shared Unicode pattern supplies those categories; exclude surrogate code units so
// a supplementary letter (or an unpaired surrogate) cannot become a new authored name.
// This boundary is source/stream-only; existing unrelated name/type grammar is unchanged.
export function sourceStreamPattern(source: string): RegExp {
    return pattern(`(?![\\s\\S]*[\\uD800-\\uDFFF\\u{10000}-\\u{10FFFF}])${source}`);
}

const identifier = sourceStreamPattern('^[A-Za-z_]\\w*(?![\\s\\S])');
const typeName = sourceStreamPattern('^[\\w.]+(?![\\s\\S])');

export function isSourceStreamName(value: unknown): value is string {
    return typeof value === 'string' && identifier.test(value);
}

export function isSourceStreamTypeName(value: unknown): value is string {
    return typeof value === 'string' && typeName.test(value);
}
