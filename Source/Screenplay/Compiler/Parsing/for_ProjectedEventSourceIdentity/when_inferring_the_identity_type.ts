// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { beforeAll, describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { projectedEventSourceIdentity } from '../ProjectedEventSourceIdentity';
import { RefusalDeclarations } from '../RefusalDeclarations';

describe('when inferring the projected event source identity type', () => {
    let source: string;

    const listView = () => source.replace('query DayById => DayView optional\n        by dayId String', 'query AllDays => DayView[]');
    const infer = (text: string) => {
        const application = parse(text).value!;
        const slice = application.modules[0].features[0].slices[0];
        return projectedEventSourceIdentity(slice.events[1], new RefusalDeclarations(application));
    };

    beforeAll(() => {
        source = readFileSync(resolve(__dirname, '../../Conformance/producerless-event-source.play'), 'utf8');
    });

    it('should use an explicit read model key', () => {
        infer(source.replace('dayId String\n        slots', 'dayId String key\n        slots'))!.name.should.equal('String');
    });
    it('should use a conventional identity for an admitted list view', () => {
        infer(listView())!.name.should.equal('String');
    });
    it('should prefer the conventional identity outside a collection query filter', () => {
        const filtered = listView().replace('slots Int', 'slots Int\n        otherId Int')
            .replace('query AllDays => DayView[]', 'query AllDays => DayView[]\n        by otherId Int');
        infer(filtered)!.name.should.equal('String');
    });
    it('should accept an explicit source identity projection key', () => {
        infer(source.replace('from SlotTaken', 'from SlotTaken key $eventSourceId'))!.name.should.equal('String');
    });
    it('should not infer the source identity from an event payload key', () => {
        expect(infer(source.replace('from SlotTaken', 'from SlotTaken key dayId'))).toBeUndefined();
    });
    it('should refuse a view without a key or a query identifying it', () => {
        expect(infer(source.replace('      query DayById => DayView optional\n        by dayId String\n', ''))).toBeUndefined();
    });
    it('should refuse a list view without a conventional identity', () => {
        expect(infer(listView().replaceAll('dayId', 'day'))).toBeUndefined();
    });
    it.each(['?', '[]'])('should refuse a nonscalar key with modifier %s', modifier => {
        expect(infer(source.replaceAll('dayId String', `dayId String${modifier}`))).toBeUndefined();
    });
    it('should refuse a composite identity type', () => {
        const composite = 'type DayKey\n  value String\n' + source.replaceAll('dayId String', 'dayId DayKey');
        expect(infer(composite)).toBeUndefined();
    });
    it('should refuse an unresolved projected read model', () => {
        expect(infer(source.replace('projection DayProjection => DayView', 'projection DayProjection => MissingView'))).toBeUndefined();
    });
    it('should refuse conflicting projected identity types', () => {
        const conflict = source + '\n      readmodel OtherDay\n        id Int key\n      projection OtherProjection => OtherDay\n        from SlotTaken\n          id = 1\n';
        expect(infer(conflict)).toBeUndefined();
    });
});
