// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ExpressionSyntax } from '../../Syntax/Expressions';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { toCompleteSyntaxJson } from '../../Syntax/SyntaxJson';

const document = `policy NumberPolicy
    require claim "limit" matches 19
concept Amount : Decimal
    validate
        min 20
seed
    for "global"
        Added
            amount = 21
module Numbers
    feature Values
        slice StateChange Change
            command Change
                amount Decimal
                validate
                    require amount > 22
                        message "too small"
                produces when amount >= 23 and (amount < 24 or amount != 25)
                    Added
                        amount = 26
            event Added
                amount Decimal
            readmodel View
                amount Decimal
            query ViewByAmount => View
                by amount Decimal from 27
            projection Build => View
                key 1
                from Added key 2
                    key Composite {
                        part = 3
                    }
                    parent 4
                    amount = 5
                    add amount by 6
                    subtract amount by 7
                children values identified by 8
                    from Added
                        amount = 9
                variant Current
                    enters on Added key 10
                    from Added
                        amount = 11
                remove with Added key 12
                    parent 13
                remove via join on Added key 14
            capture ReadExternal
                map
                    amount = 15
                    summary = \`value \${16}\`
                append Added
                    when added
                        amount = 17
                children values identified by id
                    map
                        amount = 18
                    append Added
                        amount = {"nested":[28,29.5]}
                nested detail
                    map
                        amount = 30
                    append Added
                        amount = 31
            reaction React
                where amount > 32
                when Added
                    invokes Change
                        amount = 33
`;

class Values extends ScreenplaySyntaxWalker {
    readonly values: number[] = [];
    override visitExpression(syntax: ExpressionSyntax): void {
        if (syntax.kind === 'LiteralExpressionSyntax' && typeof syntax.value === 'number') this.values.push(syntax.value);
        super.visitExpression(syntax);
    }
}

describe('when retaining projection and capture operands', () => {
    it('should retain and walk every modeled numeric position', () => {
        const result = parse(document);
        result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.deep.equal([]);
        const walker = new Values();
        walker.visitApplication(result.value);
        walker.values.sort((left, right) => left - right).should.deep.equal([...Array.from({ length: 28 }, (_, index) => index + 1), 29.5, 30, 31, 32, 33]);
    });

    it('should retain envelope-shaped business members as ordinary structured data', () => {
        const result = parse(document.replace('{"nested":[28,29.5]}', '{"literalType":"ExactNumber","value":9007199254740993}'));
        JSON.stringify(toCompleteSyntaxJson(result.value)).should.contain('9007199254740992');
        const walker = new Values();
        walker.visitApplication(result.value);
        walker.values.should.include(9007199254740992);
    });
});
