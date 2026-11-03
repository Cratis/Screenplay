// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const prefix = 'module M\n  feature F\n    slice StateView S\n      readmodel Row\n';
const model = (body: string) => parse(prefix + body).value.modules[0].features[0].slices[0].readModels[0];

describe('when parsing passive read models', () => {
    it('should omit the flag for an ordinary active read model', () => {
        Object.hasOwn(model('        id String'), 'isPassive').should.equal(false);
        Object.hasOwn(toSyntaxJson(model('        id String')) as object, 'isPassive').should.equal(false);
    });

    it.each(['        passive\n        id String', '        id String\n        passive'])('should retain the bare directive in either position', body => {
        model(body).isPassive!.should.equal(true);
        (toSyntaxJson(model(body)) as { isPassive: boolean }).isPassive.should.equal(true);
    });

    it('should keep a property named passive', () => {
        const parsed = model('        passive Bool');
        Object.hasOwn(parsed, 'isPassive').should.equal(false);
        parsed.properties[0].name.should.equal('passive');
    });

    it('should retain passivity while warning on repeats', () => {
        const result = parse(prefix + '        passive\n        id String\n        passive');
        result.value.modules[0].features[0].slices[0].readModels[0].isPassive!.should.equal(true);
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0858@7']);
        result.diagnostics[0].severity.should.equal('warning');
    });

    it.each(['true', 'false', '  true', '\tfalse'])('should reject directive arguments as an invalid property', argument => {
        parse(prefix + `        passive ${argument}`).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0016']);
    });
});
