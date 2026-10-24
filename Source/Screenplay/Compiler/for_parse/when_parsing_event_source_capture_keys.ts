// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse, CompilationResult } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CaptureSyntax, ApplicationSyntax } from '../Syntax';

describe('when parsing event source capture keys', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let capture: CaptureSyntax;
    let unmarked: CaptureSyntax;
    let refused: CompilationResult<ApplicationSyntax>;

    beforeAll(() => {
        const source = readFileSync(resolve(__dirname, '../Conformance/event-source-capture-keys.play'), 'utf8');
        result = parse(source);
        capture = result.value!.modules[0].features[0].slices[0].captures[0];
        unmarked = parse(source.replace(' is event source', '')).value!.modules[0].features[0].slices[0].captures[0];
        refused = parse(['module M', '  feature F', '    slice Translate Receive', '      capture Feed', '        source webhook', '          path /feed', '        key id is event source'].join('\n'));
    });

    it('should accept the shared fixture', () => result.success.should.be.true);
    it('should strip the marker from the key', () => capture.key!.should.equal('orderId'));
    it('should record the marker', () => capture.keyIsEventSource!.should.be.true);
    it('should leave an unmarked capture without the member', () => (unmarked.keyIsEventSource === undefined).should.be.true);
    it('should refuse the marker outside source events', () => refused.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.CaptureEventSourceKeyOutsideEvents));
});
