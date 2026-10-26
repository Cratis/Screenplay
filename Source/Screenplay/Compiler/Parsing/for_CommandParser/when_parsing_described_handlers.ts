// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { should } from 'chai';
import { parse } from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { mergeDocuments } from '../../Files/PlayFolderMerge';

should();
const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n';

for (const body of [
    '        produces E\n        handler\n          implementation',
    '        handler\n          implementation\n            hint "one"\n            hint "two"\n        produces E',
    '        handler\n          implementation',
    '        value String\n        returns value\n        handler\n          implementation',
    '        handler\n          file First.cs\n        handler\n          file Last.cs',
]) {
    describe(`when parsing a valid handler form ${body}`, () => {
        let result: ReturnType<typeof parse>;
        beforeEach(() => { result = parse(prefix + body); });
        it('should keep valid authoring syntax', () => result.diagnostics.should.be.empty);
    });
}

for (const body of ['file C.cs', 'implementation\n            file C.cs', '```csharp\n          return null;\n          ```', 'implementation\n            ```csharp\n            return null;\n            ```']) {
    describe(`when parsing an attached handler with productions ${body}`, () => {
        let result: ReturnType<typeof parse>;
        beforeEach(() => { result = parse(prefix + `        produces E\n        handler\n          ${body}`); });
        it('should admit the result contract', () => result.diagnostics.should.be.empty);
    });
}

for (const [first, second] of [
    ['implementation', 'file C.cs'],
    ['file C.cs', 'implementation'],
    ['implementation', 'implementation'],
    ['implementation\n            hint "one"', 'implementation\n            hint "two"'],
    ['implementation', '```csharp\n          return null;\n          ```'],
    ['```csharp\n          return null;\n          ```', 'implementation'],
]) {
    describe(`when parsing conflicting handler occurrences ${first} then ${second}`, () => {
        let result: ReturnType<typeof parse>;
        const firstPart = prefix + `        handler\n          ${first}\n`;
        beforeEach(() => { result = parse(firstPart + `        handler\n          ${second}`); });
        it('should report an error at the second handler', () => {
            const errors = result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.PendingIntentWithOtherHandler);
            errors.should.have.lengthOf(1);
            errors[0].severity.should.equal('error');
            errors[0].location.line.should.equal(firstPart.split('\n').length);
        });
    });
}

describe('when a pending handler is followed by a malformed handler', () => {
    it('should still diagnose the other occurrence', () => {
        parse(prefix + '        handler\n          implementation\n        handler').diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.PendingIntentWithOtherHandler);
    });
});

describe('when attached handlers replace each other', () => {
    it('should keep the last handler', () => {
        const result = parse(prefix + '        handler\n          file First.cs\n        handler\n          file Last.cs');
        result.value.modules[0].features[0].slices[0].commands[0].handler!.file!.path.should.equal('Last.cs');
    });
});

for (const [first, second] of [['implementation', 'file C.cs'], ['file C.cs', 'implementation'], ['implementation', 'implementation']]) {
    describe(`when composing command parts ${first} then ${second}`, () => {
        let result: ReturnType<typeof mergeDocuments>;
        beforeEach(() => {
            const firstDocument = parse(prefix + `        handler\n          ${first}`);
            const secondDocument = parse(prefix + `        handler\n          ${second}`);
            const firstSlice = firstDocument.value.modules[0].features[0].slices[0];
            const secondSlice = secondDocument.value.modules[0].features[0].slices[0];
            const module = firstDocument.value.modules[0];
            const feature = module.features[0];
            const value = { ...firstDocument.value, modules: [{ ...module, features: [{ ...feature, slices: [{ ...firstSlice, commands: [...firstSlice.commands, ...secondSlice.commands] }] }] }] };
            result = mergeDocuments([{ ...firstDocument, value }]);
        });
        it('should report the pending handler conflict', () => result.diagnostics.map(diagnostic => diagnostic.code).should.include(DiagnosticCodes.PendingIntentWithOtherHandler));
    });
}
