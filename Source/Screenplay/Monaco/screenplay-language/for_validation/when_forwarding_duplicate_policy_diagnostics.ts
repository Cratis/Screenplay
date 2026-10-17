// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { validateLines } from '../validation';

describe('when forwarding duplicate policy diagnostics', () => {
    it('should surface the compiler error once at the second declaration', () => {
        const issues = validateLines(['policy IsAdmin', '  require authenticated', 'policy IsAdmin', '  require role "Auditor"']);
        issues.should.have.lengthOf(1);
        issues[0].code!.should.equal('PLAY0168');
        issues[0].severity.should.equal('error');
        issues[0].line.should.equal(2);
        issues[0].startColumn.should.equal(1);
        issues[0].message.should.equal("Duplicate declaration of policy 'IsAdmin' - policy names must be unique");
    });
    it('should not duplicate the existing concept and type check', () => {
        validateLines(['concept Value : String', 'type Value', '  text String']).filter(issue => issue.code === 'PLAY0168').should.have.lengthOf(1);
    });
    it('should accept distinct policy names', () => {
        validateLines(['policy Access', '  require authenticated', 'policy access', '  require authenticated']).should.deep.equal([]);
    });
});
