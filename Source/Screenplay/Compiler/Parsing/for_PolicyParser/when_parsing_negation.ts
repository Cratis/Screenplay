// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { PolicyConditionSyntax } from '../../Syntax/Policies';

function condition(text: string): PolicyConditionSyntax {
    const result = parse(`policy Access\n  require ${text}`);
    result.diagnostics.should.deep.equal([]);
    const policyCondition = result.value?.policies?.[0]?.condition;
    if (policyCondition === undefined || policyCondition === null) throw new Error('Expected a parsed policy condition');
    return policyCondition;
}

function describeCondition(node: PolicyConditionSyntax): string {
    switch (node.kind) {
        case 'AuthenticatedConditionSyntax': return 'authenticated';
        case 'RoleConditionSyntax': return `role:${node.role}`;
        case 'ClaimConditionSyntax': return `claim:${node.claim}`;
        case 'NotPolicyConditionSyntax': return `not ${describeCondition(node.operand)}`;
        case 'LogicalPolicyConditionSyntax': return `(${describeCondition(node.left)} ${node.operator} ${describeCondition(node.right)})`;
    }
}

describe('when parsing policy negation', () => {
    it('should bind not tighter than and and or', () => {
        describeCondition(condition('not role "Service" and authenticated or role "Controller"')).should.equal('((not role:Service And authenticated) Or role:Controller)');
    });
    it('should negate grouped conditions', () => {
        describeCondition(condition('not (role "Service" or claim "actorKind" matches "service") and authenticated')).should.equal('(not (role:Service Or claim:actorKind) And authenticated)');
    });
    it('should nest repeated negation', () => {
        describeCondition(condition('not not authenticated')).should.equal('not not authenticated');
    });
    it('should refuse a missing operand', () => {
        parse('policy Access\n  require not').diagnostics.some(diagnostic => diagnostic.code === 'PLAY0116').should.equal(true);
    });
    it('should refuse negated policy references', () => {
        parse('policy Access\n  require not OpaquePolicy').diagnostics.some(diagnostic => diagnostic.code === 'PLAY0115').should.equal(true);
    });
    it('should refuse negation in authorize', () => {
        parse('policy Access\n  require authenticated\nmodule Portal\n  authorize not Access').diagnostics.some(diagnostic => diagnostic.code === 'PLAY0123').should.equal(true);
    });
});
