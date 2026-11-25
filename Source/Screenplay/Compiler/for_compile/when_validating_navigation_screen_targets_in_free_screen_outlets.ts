// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

function source(scope: string, selection = ' screen Projects', fits = '', extra = ''): string {
    const screen = `screen Shared\n  outlet sidebar extends\n    item selected title "Selected" target Shop.Projects${selection}\n`;
    const indent = (spaces: number) => screen.trimEnd().split('\n').map(line => ' '.repeat(spaces) + line).join('\n') + '\n';
    return 'layout Shell\n  outlet sidebar of navigation\n' + (scope === 'application' ? screen : '') +
        'module Shop\n' +
        (scope === 'module' ? indent(2) : '') +
        '  feature Projects\n' +
        (scope === 'feature' ? indent(4) : '') +
        '    slice StateView Listing\n      uses screen Shared\n      screen Projects\n' + fits + extra;
}

describe.each(['application', 'module', 'feature'])('when validating navigation in an adopted %s free screen', scope => {
    it('should accept a selected page', () => parse(source(scope)).success.should.be.true);
    it('should preserve legacy feature targets', () => parse(source(scope, '')).success.should.be.true);
    it('should reject a missing selected screen', () => {
        const result = parse(source(scope, ' screen Missing'));
        result.success.should.be.false;
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY1168').length.should.equal(1);
    });
    it('should reject an ambiguous selected screen', () => {
        const result = parse(source(scope, undefined, '', '    slice StateView Duplicate\n      screen Projects\n'));
        result.success.should.be.false;
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY1169').length.should.equal(1);
    });
    it('should reject a selected screen that does not fit page', () => {
        const result = parse(source(scope, undefined, '        fits dialog\n'));
        result.success.should.be.false;
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY1170').length.should.equal(1);
    });
});
