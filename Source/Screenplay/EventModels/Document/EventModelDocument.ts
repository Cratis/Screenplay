// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventVisibility, TranslationDirection } from '@cratis/screenplay-compiler';

// The JSON document the event model board reads (@cratis/event-models' event-model-document.schema.json).
// It is plain data - string ids, numeric enumerations, JSON Schema objects - so it can cross into a webview
// as-is and be read there with readEventModelDocument. Members the board defaults when absent are left out.

export type JsonSchemaObject = Record<string, unknown>;

export const SliceType = { stateChange: 0, stateView: 1, automation: 2, translator: 3 } as const;
export const SliceStatus = { notStarted: 0 } as const;
export const AutomationTriggerType = { timer: 0, event: 1, custom: 2, none: 3 } as const;
export const ActorType = { uiRole: 0, systemRole: 1 } as const;
export const PrototypeLayout = { absolute: 0 } as const;
export const PrototypeVisibility = { visible: 0 } as const;
export const PrototypeAnchoring = { none: 0 } as const;
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
    actors: ActorDocument[];
}

// Someone the model is drawn for. A UI role gets a row of prototypes on the board.
export interface ActorDocument {
    id: string;
    name: string;
    actorType: number;
    description: string;
    persona?: { id: string; name: string };
}

// What a UI role sees in one slice - the screens drawn as prototype elements (@cratis/scene UIElement data).
export interface UserExperienceActorDocument {
    id: string;
    type: number;
    elements: PrototypeElementDocument[];
    layout: number;
    windowWidth: number;
    windowHeight: number;
}

// One element of a prototype. 'type' is the placeholder the board draws it as when no component library
// is registered to render it - a button, a data table, a panel.
export interface PrototypeElementDocument {
    id: string;
    name: string;
    type: string;
    width: number;
    height: number;
    ZIndex: number;
    visibility: number;
    isEnabled: boolean;
    opacity: number;
    anchoring: number;
    properties: { canvas: { x: number; y: number } };
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
    direction?: TranslationDirection;
    id: string;
    name: string;
    sliceType: number;
    description: string;
    status: number;
    collapsed: boolean;
    sortOrder: number;
    command?: CommandItemDocument;
    readModel?: ReadModelItemDocument;
    externalEvents: ExternalEventItemDocument[];
    events: EventItemDocument[];
    queries: QueryItemDocument[];
    actors: UserExperienceActorDocument[];
    automationTrigger?: AutomationTriggerDocument;
    specifications: SliceSpecificationDocument[];
    commentCount: number;
}

// Something from outside the model a translation turns into events - for a capture, the system it reads.
export interface ExternalEventItemDocument {
    id: string;
    name: string;
}

export interface EventConstraintDocument {
    name: string;
    message: string;
}

export interface EventItemDocument {
    visibility?: EventVisibility;
    origin?: string;
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

export interface SpecificationCallerDocument {
    authenticated: boolean;
    roles: string[];
    claims: Record<string, string>;
}

export interface SliceSpecificationDocument {
    id: string;
    name: string;
    given: SpecificationStepDocument[];
    when?: { id: string; commandId?: string; name: string; values: Record<string, unknown>; generatedValues?: Record<string, unknown> };
    caller?: SpecificationCallerDocument;
    thenEvents: SpecificationStepDocument[];
    thenErrors: { id: string; name: string; message?: string }[];
    // The pinned board reader ignores these additions; the header also displays their effective values.
    givenReadModels?: { name: string; values: Record<string, unknown>; exactly: boolean }[];
    thenReadModels?: { name: string; values: Record<string, unknown>; exactly: boolean }[];
    whenRedelivered?: { eventType: string; reaction: string; values: Record<string, unknown>; for?: unknown };
    thenNoEvents?: boolean;
    thenReturns?: { value: unknown } | { fields: Record<string, unknown> };
    thenAbsentReadModels?: { name: string; key: unknown }[];
    collapsed: boolean;
}
