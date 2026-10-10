// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { ExpressionSyntax } from '@cratis/screenplay-compiler';
import { expressionText } from '../expressionText';

const location = { line: 1, column: 1 };
const vectors: [ExpressionSyntax, string][] = [
    [{ kind: 'LiteralExpressionSyntax', value: 'A"B\\C', location }, '"A\\"B\\\\C"'],
    [{ kind: 'LiteralExpressionSyntax', value: 42, location }, '42'],
    [{ kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '9007199254740993' }, location }, '9007199254740993'],
    [{ kind: 'EventSourceIdExpressionSyntax', location }, '$eventSourceId'],
    [{ kind: 'EventContextExpressionSyntax', path: 'occurred', location }, '$eventContext.occurred'],
    [{ kind: 'CausedByExpressionSyntax', property: null, location }, '$causedBy'],
    [{ kind: 'CausedByExpressionSyntax', property: 'name', location }, '$causedBy.name'],
    [{ kind: 'TemplateExpressionSyntax', parts: [{ kind: 'TemplateTextSyntax', text: 'value ', location }, { kind: 'TemplateInterpolationSyntax', expression: { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '9007199254740993' }, location }, location }], location }, '`value ${9007199254740993}`'],
    [{ kind: 'LiteralExpressionSyntax', value: false, location }, 'false'],
    [{ kind: 'LiteralExpressionSyntax', value: null, location }, 'null'],
    [{ kind: 'PathExpressionSyntax', path: '@uses.email', location }, '@uses.email'],
    [{ kind: 'ContextExpressionSyntax', path: 'identity.name', location }, '$context.identity.name'],
    [{ kind: 'IdentityExpressionSyntax', path: 'name', location }, '$identity.name'],
    [{ kind: 'EnvironmentExpressionSyntax', name: 'region', location }, '$env.region'],
    [{ kind: 'StringsExpressionSyntax', key: 'welcome', location }, '$strings.welcome'],
    [{ kind: 'SourceItemExpressionSyntax', path: 'name', location }, '$.name'],
    [{ kind: 'RawExpressionSyntax', text: 'not evaluated', location }, 'not evaluated'],
    [{ kind: 'ListExpressionSyntax', items: [], location }, '[]'],
    [{ kind: 'ObjectExpressionSyntax', members: [], location }, '{  }'],
    [{ kind: 'ObjectExpressionSyntax', members: [{ kind: 'ObjectMemberSyntax', name: 'quoted"key', value: { kind: 'ListExpressionSyntax', items: [{ kind: 'LiteralExpressionSyntax', value: 1, location }], location }, location }], location }, '{ "quoted\\"key": [1] }']
];

describe('when displaying authored expressions', () => {
    it.each(vectors)('should show %j as %s without compiler objects or evaluation', (expression, expected) => {
        expect(expressionText(expression)).toBe(expected);
    });
});
