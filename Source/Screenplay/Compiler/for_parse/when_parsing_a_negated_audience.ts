// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { AudienceAvailabilityRequirementSyntax, availabilityLeaves } from '../Syntax/Availability';

const codes = (source: string): string[] => parse(source).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`);

const header = ['audience Plus', 'audience Incident', 'availability Open', '  for Plus', 'module Fleet'];
const slice = ['  feature Quality', '    slice StateView Vessels', '      event VesselRegistered', '        vesselId Uuid'];

function gateOf(line: string): readonly AudienceAvailabilityRequirementSyntax[] {
    const result = parse([...header, line, ...slice].join('\n'));
    const gate = result.value.modules[0].available![0].requirement;
    return availabilityLeaves(gate, 'AudienceAvailabilityRequirementSyntax');
}

describe('when parsing a negated audience', () => {
    it('should compile `for Plus and not Incident` without findings', () => {
        codes([...header, '  available for Plus and not Incident', ...slice].join('\n')).should.deep.equal([]);
    });

    it('should mark only the negated audience', () => {
        gateOf('  available for Plus and not Incident').map(leaf => `${leaf.audience}:${leaf.negated}`).should.deep.equal(['Plus:false', 'Incident:true']);
    });

    it('should accept a leading negation and a negation inside a group', () => {
        gateOf('  available for not Incident or (Plus and not Incident)').map(leaf => leaf.negated).should.deep.equal([true, false, true]);
    });

    it('should report a not on a group, on another not, on nothing and on an availability', () => {
        for (const line of ['  available for not (Plus)', '  available for not not Plus', '  available for Plus and not', '  available with not Open']) {
            codes([...header, line, ...slice].join('\n')).should.deep.equal(['PLAY1171@6']);
        }
    });

    it('should warn about a negated audience nothing declares', () => {
        codes([...header, '  available for Plus and not Ghost', ...slice].join('\n')).should.include('PLAY1135@6');
    });
});
