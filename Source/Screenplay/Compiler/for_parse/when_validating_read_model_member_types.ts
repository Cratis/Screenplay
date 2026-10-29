// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';

const prefix = 'module M\n  feature F\n    slice StateView View\n      readmodel Parent\n';
const message = "Read model 'Parent' member 'child' references unsupported type 'Child'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.";

describe('when validating read-model member types', () => {
    it.each(['Child', 'Child[]', 'Child optional', 'Child[] optional'])('should reject a read-model member of type %s', type => {
        const result = parse(prefix + `        child ${type}\n      readmodel Child\n        value String`, 'model.play');
        result.success.should.be.false;
        result.diagnostics.should.deep.equal([{
            code: 'PLAY0273', severity: 'error', message, location: { line: 5, column: 15, path: 'model.play' }, arguments: {},
        }]);
    });
    it('should retain the warning for a skipped slice-local type and diagnose its member', () => {
        const result = parse(prefix + '        child Child[]\n      type Child\n        value String');
        result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location.line]).should.deep.equal([
            ['PLAY0029', 'warning', 6], ['PLAY0273', 'error', 5],
        ]);
        result.diagnostics[0].message.should.equal("Unknown construct 'type' in slice 'View'");
        result.diagnostics[1].message.should.equal(message);
    });
    it('should leave an unused slice-local type warning-only', () => {
        const result = parse(prefix + '        child String\n      type Child\n        value String');
        result.success.should.be.true;
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0029']);
    });
    it('should reject an undeclared type', () => {
        parse(prefix + '        child Child[]').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0273']);
    });
    it('should not admit a sibling read model as a value type', () => {
        parse(prefix + '        child Child[]\n    slice StateView Other\n      readmodel Child\n        value String').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0273']);
    });
    it('should admit forward top-level composites, primitives, concepts and imported names', () => {
        const result = parse(prefix + '        child Child[]\n        scalar Child\n        optional Child optional\n        name Name\n        external Imported\n        id Uuid\n        text String\n        count Int\n        amount Decimal\n        active Bool\n        date Date\n        timestamp DateTime\ntype Child\n  value String\nconcept Name : String\nimport External.Imported');
        result.diagnostics.should.be.empty;
        result.success.should.be.true;
    });
    it('should prefer the value namespace when a read model shares its name', () => {
        parse(prefix + '        child Child[]\n      readmodel Child\n        value String\ntype Child\n  value String').diagnostics.should.be.empty;
    });
    it('should defer validation for partial authoring parses', () => {
        parseForAuthoring(prefix + '        child Child[]', 'fragment.play', [], false).diagnostics.should.be.empty;
    });
});
