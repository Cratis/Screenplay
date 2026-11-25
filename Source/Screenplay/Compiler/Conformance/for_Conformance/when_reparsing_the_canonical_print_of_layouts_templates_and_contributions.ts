// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const conformance = resolve(__dirname, '..');

// Cross-compiler printing (decision of 2026-11-25): the TypeScript compiler has no .play printer. Its exact
// syntax JSON is decoded and printed by the C# canonical printer (a C# spec holds that print to the
// committed .printed.txt file); this holds the TypeScript compiler to reading that canonical print back
// into the very syntax it was printed from.
describe('when reparsing the canonical print of layouts, templates and contributions', () => {
    let printed: ReturnType<typeof parse>;
    let golden: unknown;

    beforeAll(() => {
        printed = parse(readFileSync(join(conformance, 'layouts-templates-contributions.printed.txt'), 'utf8'));
        golden = JSON.parse(readFileSync(join(conformance, 'layouts-templates-contributions.syntax.json'), 'utf8'));
    });

    it('should parse without errors', () => printed.diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]));
    it('should read back the syntax it was printed from', () => JSON.parse(JSON.stringify(toSyntaxJson(printed.value))).should.deep.equal(golden));
});
