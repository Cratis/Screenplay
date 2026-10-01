// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The JSON document the event model board reads (@cratis/event-models' event-model-document.schema.json).
// It is plain data - string ids, numeric enumerations, JSON Schema objects - so it can cross into a webview
// as-is and be read there with readEventModelDocument. Members the board defaults when absent are left out.

export type JsonSchemaObject = Record<string, unknown>;

export const SliceType = { stateChange: 0, stateView: 1, automation: 2, translator: 3 } as const;
export const SliceStatus = { notStarted: 0 } as const;
export const AutomationTriggerType = { timer: 0, event: 1, custom: 2, none: 3 } as const;
export const QueryParameterType = { text: 0, number: 1, boolean: 2, date: 3, time: 4, uniqueId: 5 } as const;

export interface EventModelDocument {
    id: string;
    name: string;
    collections: ModuleCollectionDocument[];
    stickyNotes: [];
    links: [];
}

export interface ModuleCollectionDocument {
    id: string;
    position: { x: number; y: number };
    modules: ModuleDocument[];
    actors: [];
}

export interface ModuleDocument {
    id: string;
    name: string;
    features: FeatureDocument[];
    collapsed: boolean;
    sortOrder: number;
    commentCount: number;
}

export interface FeatureDocument {
    id: string;
    name: string;
    subFeatures: FeatureDocument[];
    slices: SliceDocument[];
    collapsed: boolean;
    rowCollapsed: boolean;
    enabled: boolean;
    commentCount: number;
}

export interface SliceDocument {
    id: string;
    name: string;
    sliceType: number;
    description: string;
    status: number;
    collapsed: boolean;
    sortOrder: number;
    command?: CommandItemDocument;
    readModel?: ReadModelItemDocument;
    externalEvents: [];
    events: EventItemDocument[];
    queries: QueryItemDocument[];
    actors: [];
    automationTrigger?: AutomationTriggerDocument;
    specifications: SliceSpecificationDocument[];
    commentCount: number;
}

export interface EventConstraintDocument {
    name: string;
    message: string;
}

export interface EventItemDocument {
    id: string;
    name: string;
    schema: JsonSchemaObject;
    sourceEventId?: string;
    constraints?: { unique?: EventConstraintDocument; uniqueEventType?: EventConstraintDocument };
}

export interface CommandRuleDocument {
    errorMessage: string;
    ruleType: string;
}

export interface CommandItemDocument {
    id: string;
    name: string;
    schema: JsonSchemaObject;
    stateSchema: JsonSchemaObject;
    logicDescription: string;
    rules: { propertyName: string; rules: CommandRuleDocument[] }[];
}

export interface ReadModelItemDocument {
    id: string;
    name: string;
    schema: JsonSchemaObject;
    materializes: boolean;
}

export interface QueryItemDocument {
    id: string;
    name: string;
    parameters: { name: string; type: number }[];
}

export interface AutomationTriggerDocument {
    type: number;
    description: string;
    eventId?: string;
    schedule?: {
        intervalMinutes: number;
        intervalHours: number;
        timeOfDay: string;
        dayOfWeek: number;
        dayOfMonth: number;
        description: string;
    };
}

export interface SpecificationStepDocument {
    id: string;
    name: string;
    eventId: string;
    values: Record<string, unknown>;
}

export interface SliceSpecificationDocument {
    id: string;
    name: string;
    given: SpecificationStepDocument[];
    when?: { id: string; commandId?: string; name: string; values: Record<string, unknown> };
    thenEvents: SpecificationStepDocument[];
    thenErrors: { id: string; name: string }[];
    collapsed: boolean;
}
