// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const source = 'policy DataQualityEnabled\n  reads AssetPlan as plan by assetId\n  require authenticated and plan.dataQuality == true\n\npolicy IsAuthor\n  reads ArticleView as article by slug\n  require article.author == $identity.id\n';

describe('when parsing policies that decide from recorded state', () => {
    const result = parse(source);
    const [first, second] = result.value?.policies ?? [];

    it('should parse without diagnostics', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should keep the read with its alias and key', () => {
        first?.reads?.map(read => [read.readModel, read.alias, read.by]).should.deep.equal([['AssetPlan', 'plan', 'assetId']]);
    });

    it('should parse the state condition beside authenticated', () => {
        const condition = first?.condition;
        const kind = condition?.kind === 'LogicalPolicyConditionSyntax' ? condition.right.kind : condition?.kind;
        (kind ?? '').should.equal('StateConditionSyntax');
    });

    it('should parse a comparison with the caller identity', () => {
        (second?.condition?.kind === 'StateConditionSyntax' ? second?.condition.comparison.left : '').should.equal('article.author');
    });
});
