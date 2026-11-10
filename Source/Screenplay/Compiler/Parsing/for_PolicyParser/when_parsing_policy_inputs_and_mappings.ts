// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const policy = 'policy PlanGrants\n  asset AssetId from $context.asset\n  reads AssetPlan as plan by asset\n  require plan.dataQuality == true\n';
const artifact = 'module M\n  feature F\n    slice StateChange S\n      command Request\n        vesselId AssetId identifier\n        authorize PlanGrants\n          asset = vesselId\n';

describe('when parsing policy inputs and mappings', () => {
    const parsedPolicy = parse(policy);
    const parsedArtifact = parse(artifact);
    const input = parsedPolicy.value?.policies?.[0]?.inputs?.[0];
    const command = parsedArtifact.value?.modules?.[0]?.features?.[0]?.slices?.[0]?.commands?.[0];

    it('should keep the input with its type and default source', () => {
        [input?.name, input?.type, input?.from].should.deep.equal(['asset', 'AssetId', '$context.asset']);
    });

    it('should keep the read keyed by the input', () => {
        (parsedPolicy.value?.policies?.[0]?.reads?.[0]?.by ?? '').should.equal('asset');
    });

    it('should keep the mapping under authorize', () => {
        (command?.authorize?.mappings ?? []).map(mapping => [mapping.input, mapping.property]).should.deep.equal([['asset', 'vesselId']]);
    });
});
