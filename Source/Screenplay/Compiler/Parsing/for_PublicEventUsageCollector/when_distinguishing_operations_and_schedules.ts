// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { PublicEventUsageCollector } from '../PublicEventUsageCollector';

const source = `system Outside
module M
  feature F
    slice Automation Send
      event Changed
      command Send
        produces Changed
        produces operation Notify
          uses Outside
      reaction Publish
        when Changed
          produces Changed
        every 5 minutes
          produces Changed
        at 09:00
          produces Changed
      specification Fixture
        given Changed
        when append Changed
        then Changed
`;

describe('when distinguishing operations and schedules from event usage', () => {
    let collector: PublicEventUsageCollector;
    let diagnostics: ReturnType<typeof parse>['diagnostics'];

    beforeEach(() => {
        const result = parse(source);
        diagnostics = result.diagnostics;
        collector = new PublicEventUsageCollector();
        collector.visitSlice(result.value.modules[0].features[0].slices[0]);
    });

    it('should use valid authoring syntax for the operational edges', () => {
        diagnostics.should.deep.equal([]);
    });
    it('should collect only the event edges and exclude operations and specification fixtures', () => {
        collector.uses.map(use => ({ name: use.name, output: use.output, command: use.command })).should.deep.equal([
            { name: 'Changed', output: true, command: true },
            { name: 'Changed', output: false, command: false },
            { name: 'Changed', output: true, command: false },
            { name: 'Changed', output: true, command: false },
            { name: 'Changed', output: true, command: false },
        ]);
    });
    it('should not turn scheduled occurrences into all event subscriptions', () => {
        collector.allEvents.should.deep.equal([]);
    });
});
