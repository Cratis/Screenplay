// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { contextVariableItems } from '../completion-items';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { identityProperties } from '../language';
import { validateLines } from '../validation';

const prefix = ['module M', '  feature F', '    slice StateChange S', '      command C', '        produces E'];

describe('when authoring identity paths', () => {
    it('should complete the same built-in properties as context identity', () => {
        contextVariableItems.filter(item => item.label.startsWith('$identity.')).map(item => item.label.replace('$identity.', '').replace(/\.$/, '')).should.deep.equal(identityProperties);
    });

    it('should replace the complete identity prefix', () => {
        const line = '          caller = $identity.';
        planCompletions([...prefix, line], prefix.length, line).should.deep.equal({ kind: 'contextVariables', replaceLength: '$identity.'.length });
    });

    it('should warn about an unknown property with the compiler code', () => {
        const issues = validateLines([...prefix, '          caller = $identity.nope']).filter(issue => issue.code === 'PLAY0155');
        issues.should.have.lengthOf(1);
        issues[0].severity.should.equal('warning');
        issues[0].message.should.contain("Unknown $identity property 'nope'");
    });

    it('should leave claim names opaque', () => {
        validateLines([...prefix, '          caller = $identity.claims.anything.here']).filter(issue => issue.code === 'PLAY0155').should.deep.equal([]);
    });

    it('should leave a bare identity in descriptions alone', () => {
        validateLines([...prefix.slice(0, 4), '        description "Runs as $identity, not as a user"']).filter(issue => issue.code === 'PLAY0155' || issue.severity === 'error').should.deep.equal([]);
    });

    it('should not claim longer root names', () => {
        validateLines([...prefix, '          caller = $identityOther.id']).filter(issue => issue.code === 'PLAY0155').should.deep.equal([]);
    });

    it('should explain the caller on hover', () => {
        const line = '          caller = $identity.id';
        hoverContent([line], 0, 'id', line.length - 1, line.length + 1)!.should.contain('caller');
    });
});
