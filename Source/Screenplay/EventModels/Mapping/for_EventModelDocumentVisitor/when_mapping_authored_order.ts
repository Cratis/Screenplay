// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative, resolve } from 'node:path';
import { describe, it } from 'vitest';
import { compileApplication, parse, parsePlacedDocuments, toSyntaxJson } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { compileEventModelApplication } from '../compileEventModelApplication';

const samples = resolve(__dirname, '../../../../../Samples');
function filesIn(folder: string): string[] {
    return readdirSync(folder).flatMap(name => {
        const path = join(folder, name);
        return statSync(path).isDirectory() ? filesIn(path) : path.endsWith('.play') ? [path] : [];
    });
}
function sample(name: string) {
    const folder = join(samples, name);
    const files = new Map(filesIn(folder).map(file => [relative(folder, file), readFileSync(file, 'utf8')]));
    const compilation = compileEventModelApplication([...files].map(([path, source]) => ({ path, source })));
    const baseline = parsePlacedDocuments(compilation.documents);
    JSON.stringify(toSyntaxJson(compilation.value)).should.equal(JSON.stringify(toSyntaxJson(baseline.value)));
    // Merge diagnostics remain byte-for-byte the baseline, followed by exactly the new timeline findings.
    compilation.diagnostics.slice(0, baseline.diagnostics.length).should.deep.equal(baseline.diagnostics);
    const expected: Record<string, string[]> = {
        Commerce: [
            'PLAY0516@Catalog/Products/ProductList.play:26',
            'PLAY0516@Catalog/Products/ProductList.play:32',
            'PLAY0516@Ordering/Orders/MyOrders.play:29',
            'PLAY0516@Ordering/Orders/MyOrders.play:34',
            'PLAY0517@Ordering/Orders/MyOrders.play:38',
            'PLAY0516@Fulfillment/Shipping/ShipmentQueue.play:30',
            'PLAY0517@Fulfillment/Shipping/ShipmentQueue.play:38',
        ],
        TimeTracking: [
            'PLAY0517@Payroll/Runs/ReviewingRuns.play:43',
            'PLAY0516@Payroll/Handover/QueueingApprovedWeeks.play:10',
            'PLAY0516@Payroll/Handover/RemindingConsultants.play:10',
            'PLAY0516@Payroll/Absences/BookingTimeOff.play:8',
            'PLAY0516@Timesheets/Recording/FollowingMyWeeks.play:23',
            'PLAY0516@Timesheets/Recording/FollowingMyWeeks.play:25',
            'PLAY0516@Timesheets/Recording/FollowingMyWeeks.play:27',
            'PLAY0516@Timesheets/Recording/FollowingMyWeeks.play:29',
        ],
    };
    const timeline = compilation.diagnostics.slice(baseline.diagnostics.length);
    timeline.map(diagnostic => `${diagnostic.code}@${diagnostic.location.path}:${diagnostic.location.line}`).should.deep.equal(expected[name]);
    timeline.every(diagnostic => diagnostic.severity === 'information').should.be.true;
    return toEventModelDocument(compilation.value, name).collections[0].modules;
}

describe('when mapping authored order', () => {
    it('should draw Commerce modules in the order of its root imports', () => {
        const modules = sample('Commerce');
        modules.map(module => module.name).should.deep.equal(['Catalog', 'Ordering', 'Fulfillment']);
        modules.map(module => module.sortOrder).should.deep.equal([0, 1, 2]);
        // Products imports *.play, so its slices intentionally remain alphabetical.
        modules[0].features[0].slices.map(slice => slice.name).should.deep.equal(['DiscontinueProduct', 'ProductList', 'RegisterProduct']);
        modules[0].features[0].slices.map(slice => slice.sortOrder).should.deep.equal([0, 1, 2]);
    });

    it('should not let TimeTracking placement stubs precede the declared features', () => {
        const modules = sample('TimeTracking');
        // The root itself is **/*.play: modules, as well as slice glob matches, stay alphabetical.
        modules.map(module => module.name).should.deep.equal(['Engagements', 'Payroll', 'Timesheets']);
        modules[2].features.map(feature => feature.name).should.deep.equal(['Recording', 'Approval', 'Reporting']);
        modules[2].features[0].slices.map(slice => slice.name).should.deep.equal(['MyTimesheets', 'RecordTime', 'StartTimesheet', 'SubmitTimesheet']);
    });

    it('should expand imports where they occur among declarations', () => {
        const files = new Map([
            ['application.play', 'module Story\n  feature Steps\n    slice StateChange First\n    import "middle.play"\n    slice StateChange Last'],
            ['middle.play', 'slice StateChange Middle'],
        ]);
        const compilation = compileEventModelApplication([...files].map(([path, source]) => ({ path, source })));
        const bytes = JSON.stringify(toSyntaxJson(compilation.value));
        const document = toEventModelDocument(compilation.value, 'Story');
        document.collections[0].modules[0].features[0].slices.map(slice => slice.name).should.deep.equal(['First', 'Middle', 'Last']);
        JSON.stringify(toSyntaxJson(compilation.value)).should.equal(bytes);
        compilation.documents.map(document => document.path).should.deep.equal(['application.play', 'middle.play']);
    });

    it('should keep single-file order and identities unchanged', () => {
        const source = 'module Z\n  feature F\n    slice StateChange Z\n    slice StateChange A\nmodule A\n  feature F\n    slice StateChange X';
        const direct = toEventModelDocument(parse(source).value, 'Single');
        const assembled = toEventModelDocument(compileApplication(new Map([['single.play', source]]), ['single.play']).value, 'Single');
        assembled.should.deep.equal(direct);
    });
});
