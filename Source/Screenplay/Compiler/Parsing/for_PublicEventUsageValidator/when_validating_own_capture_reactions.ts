// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';
import { validatePublicEventUsage } from '../PublicEventUsageValidator';

const source = `import M.F.Facts.Recorded
module M
  feature F
    slice StateChange Facts
      event Recorded generation 1
      event Recorded generation 2
    slice Translate Import
      capture Feed
        append Recorded
      reaction FollowUp
        when Recorded`;

describe('when a directionless translation uses qualified programmatic event references', () => {
    let application: ApplicationSyntax;
    let context: ParserContext;

    beforeEach(() => {
        const parsed = parse(source).value;
        const module = parsed.modules[0];
        const feature = module.features[0];
        const slice = feature.slices[1];
        const reaction = slice.reactions[0];
        const trigger = reaction.triggers[0];
        const qualified = { ...trigger, source: { kind: 'NamedTriggerSourceSyntax' as const, name: 'M.F.Facts.Recorded', location: trigger.location } };
        const altered = { ...slice, reactions: [{ ...reaction, triggers: [trigger, qualified] }] };
        application = { ...parsed, modules: [{ ...module, features: [{ ...feature, slices: [feature.slices[0], altered] }] }] };
        context = new ParserContext(new LineReader([]));
        validatePublicEventUsage(application, context);
    });

    it('should warn once at the reaction and preserve successful compilation', () => {
        context.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location.line])
            .should.deep.equal([[DiagnosticCodes.DirectionlessTranslationReactsToOwnCapture, 'warning', 10]]);
        parse(source).success.should.be.true;
    });
    it('should list a resolved event only once across different spellings and generations', () => {
        context.diagnostics[0].message.should.equal("Reaction 'FollowUp' reacts to events appended by a capture in the same directionless Translate slice (Recorded); keep the capture in this Translate slice and move the reaction to an Automation slice.");
    });
});
