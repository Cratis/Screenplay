// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import type { editor } from 'monaco-editor';
import { Validator } from '../sub-languages/projection/Validator';
import type { JsonSchema, ReadModelInfo } from '../sub-languages/projection/index';

function modelFor(content: string): editor.ITextModel {
    return { getValue: () => content } as unknown as editor.ITextModel;
}

const backlogItemSchema: JsonSchema = {
    name: 'BacklogItem',
    properties: { title: { type: 'string' } },
};

const pullRequestItemSchema: JsonSchema = {
    name: 'PullRequestItem',
    properties: { pullRequestUrl: { type: 'string' } },
};

const readModels: ReadModelInfo[] = [
    { identifier: 'BacklogItem', displayName: 'BacklogItem', schema: backlogItemSchema },
    { identifier: 'PullRequestItem', displayName: 'PullRequestItem', schema: pullRequestItemSchema },
];

describe('when validating a projection with a variant naming a known read model', () => {
    let validator: Validator;
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        validator = new Validator();
        validator.setReadModels(readModels);
        markers = validator.validate(modelFor([
            'projection WorkItem',
            '  variant BacklogItem',
            '    title = title',
        ].join('\n')));
    });

    it('should not report the variant as an unknown read model', () => {
        markers.some(marker => marker.message.includes('not found')).should.be.false;
    });

    it('should not warn about a property the variant actually has', () => {
        markers.some(marker => marker.message.includes("Property 'title'")).should.be.false;
    });
});

describe('when validating a projection with a variant naming an unknown read model', () => {
    let validator: Validator;
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        validator = new Validator();
        validator.setReadModels(readModels);
        markers = validator.validate(modelFor([
            'projection WorkItem',
            '  variant SomethingElse',
            '    enters on SomethingHappened',
        ].join('\n')));
    });

    it('should report the variant read model as not found', () => {
        markers.some(marker => marker.message === "Read model 'SomethingElse' not found").should.be.true;
    });
});

describe('when validating a variant that maps a property it does not have', () => {
    let validator: Validator;
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        validator = new Validator();
        validator.setReadModels(readModels);
        markers = validator.validate(modelFor([
            'projection WorkItem',
            '  variant BacklogItem',
            '    pullRequestUrl = url',
        ].join('\n')));
    });

    it('should warn about the property missing from the variant', () => {
        markers.some(marker => marker.message.includes("Property 'pullRequestUrl' not found")).should.be.true;
    });
});

describe('when validating an enters on line', () => {
    let validator: Validator;
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        validator = new Validator();
        validator.setReadModels(readModels);
        validator.setEventSchemas({ IssueCreated: { name: 'IssueCreated', properties: {} } });
        markers = validator.validate(modelFor([
            'projection WorkItem',
            '  variant BacklogItem',
            '    enters on SomethingUnknown',
        ].join('\n')));
    });

    it('should report the unknown event type', () => {
        markers.some(marker => marker.message === "Event type 'SomethingUnknown' not found").should.be.true;
    });
});

describe('when two variants each declare their own property scope', () => {
    let validator: Validator;
    let markers: editor.IMarkerData[];

    beforeEach(() => {
        validator = new Validator();
        validator.setReadModels(readModels);
        markers = validator.validate(modelFor([
            'projection WorkItem',
            '  variant BacklogItem',
            '    title = title',
            '  variant PullRequestItem',
            '    pullRequestUrl = url',
        ].join('\n')));
    });

    it('should not cross-check a property against the wrong variant', () => {
        markers.should.be.empty;
    });
});
