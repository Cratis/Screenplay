// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse, CompilationResult } from '../ScreenplayCompiler';
import { CaptureSyntax, ApplicationSyntax } from '../Syntax';

describe('when parsing typed capture keys', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let capture: CaptureSyntax;

    beforeAll(() => {
        result = parse(readFileSync(resolve(__dirname, '../Conformance/typed-capture-keys.play'), 'utf8'));
        capture = result.value!.modules[0].features[0].slices[0].captures[0];
    });

    it('should accept the shared typed capture fixture', () => result.success.should.be.true);
    it('should preserve the typed source property', () =>
        result.value!.modules[0].features[0].slices[0].events[0].properties[0].type.name.should.equal('OrderId'));
    it('should preserve the source event', () => capture.source!.settings[0].value.should.equal('OrderPlaced'));
    it('should preserve the key', () => capture.key!.should.equal('orderId'));
    it('should preserve the appended event', () => capture.appends[0].event.should.equal('OrderReceived'));
});
