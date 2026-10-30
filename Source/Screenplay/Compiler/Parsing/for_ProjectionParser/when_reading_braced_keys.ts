// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

const head = 'module M\n  feature F\n    slice StateChange P\n      event E\n        id Uuid\n        other Uuid\n    slice StateView C\n      readmodel V\n        id Uuid\n      query FindC => V optional\n        by id Uuid\n      projection CProjection => V\n';
const braced = head + '        from E key id\n          key {\n            id = id\n            other = other\n          }\n        remove with E\n          key Composite {\n            id = id\n          }\n';
const plain = head + '        from E key id\n          key\n            id = id\n            other = other\n        remove with E\n          key Composite\n            id = id\n';

const codes = (source: string) => compiler.parse(source).diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.RedundantCompositeKeyBraces);
const shape = (source: string) => JSON.stringify(compiler.parse(source).value, (key, value: unknown) => key === 'location' ? undefined : value);

describe('when reading braced keys', () => {
    it('should report one information diagnostic per braced key at the key line', () => {
        const reported = codes(braced);
        expect(reported.map(diagnostic => diagnostic.location.line)).toEqual([14, 19]);
        expect(reported.every(diagnostic => diagnostic.severity === 'information')).toBe(true);
    });

    it('should not report keys written without braces', () => expect(codes(plain)).toEqual([]));

    it('should parse to the same syntax as the form without braces', () => expect(shape(braced)).toBe(shape(plain)));
});
