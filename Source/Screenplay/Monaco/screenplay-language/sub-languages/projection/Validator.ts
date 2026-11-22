// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { editor } from 'monaco-editor';
import type { JsonSchema, ReadModelInfo, DraftReadModelInfo } from './index';
import { fenceMap } from '../../document-context';
import { validateEventContextPaths } from '../../validation';

export class Validator {
    private readModels: ReadModelInfo[] = [];
    private eventSchemas: Record<string, JsonSchema> = {};
    private draftReadModel: DraftReadModelInfo | null = null;

    setReadModels(readModels: ReadModelInfo[]): void {
        this.readModels = readModels || [];
    }

    setDraftReadModel(draft: DraftReadModelInfo | null): void {
        this.draftReadModel = draft;
    }

    // Keep for backwards compatibility
    setReadModelSchemas(schemas: JsonSchema[]): void {
        this.readModels = schemas.map(schema => ({
            identifier: this.getSchemaName(schema) || '',
            displayName: this.getSchemaName(schema) || '',
            schema
        }));
    }

    setEventSchemas(schemas: Record<string, JsonSchema> | JsonSchema[]): void {
        if (!schemas) {
            this.eventSchemas = {};
            return;
        }

        if (Array.isArray(schemas)) {
            const map: Record<string, JsonSchema> = {};
            schemas.forEach((s, i) => {
                if (!s) return;
                const name = this.getSchemaName(s) || `Event${i + 1}`;
                map[name] = s;
            });
            this.eventSchemas = map;
            return;
        }

        this.eventSchemas = schemas;
    }

    validate(model: editor.ITextModel): editor.IMarkerData[] {
        const markers: editor.IMarkerData[] = [];
        const content = model.getValue();

        // Avoid spurious errors before content is loaded.
        if (!content.trim()) return markers;

        const lines = content.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n');
        const header = this.prepareHeader(lines, markers);
        if (!header) return markers;

        this.validateBody(lines, header, markers);
        this.appendEventContextMarkers(lines, markers);
        return markers;
    }

    private prepareHeader(lines: string[], markers: editor.IMarkerData[]): { activeSchema: JsonSchema | undefined; readModelName: string | null } | undefined {
        const firstNonEmptyLineIndex = lines.findIndex(line => {
            const trimmed = line.trim();
            return !!trimmed && !trimmed.startsWith('#');
        });
        if (firstNonEmptyLineIndex < 0) return undefined;

        const firstLine = lines[firstNonEmptyLineIndex].trim();
        const projectionWithReadModelMatch = firstLine.match(/^projection\s+([\w.]+)\s*=>\s*([\w.]+)\s*$/);
        const projectionAdHocMatch = !projectionWithReadModelMatch && firstLine.match(/^projection\s+([\w.]+)\s*$/);

        if (!projectionWithReadModelMatch && !projectionAdHocMatch) {
            markers.push(this.createMarker({ lineNumber: firstNonEmptyLineIndex + 1, col: 1, length: firstLine.length }, 8, 'Projection definition must start with "projection <ProjectionName>" or "projection <ProjectionName> => <ReadModelIdentifier>"'));
            return undefined;
        }
        if (!projectionWithReadModelMatch) return { activeSchema: undefined, readModelName: null };

        const readModelName = projectionWithReadModelMatch[2];
        const readModelExists = this.resolveReadModelIdentifier(readModelName) !== null;
        const isDraftReadModel = this.isDraftReadModel(readModelName);
        const location = { lineNumber: firstNonEmptyLineIndex + 1, col: this.getReadModelStartColumn(firstLine, readModelName), length: readModelName.length };
        this.validateReadModelReference({ name: readModelName, exists: readModelExists, isDraft: isDraftReadModel, location }, markers);

        // Check draft precedence separately, retaining the header's second lookup.
        const isDraft = this.isDraftReadModel(readModelName);
        const activeReadModel = isDraft ? null : this.resolveReadModelInfo(readModelName);
        const activeSchema = isDraft ? this.draftReadModel!.schema : activeReadModel?.schema;
        return { activeSchema, readModelName };
    }

    private validateReadModelReference(reference: { name: string; exists: boolean; isDraft: boolean; location: { lineNumber: number; col: number; length: number } }, markers: editor.IMarkerData[]): void {
        if (!reference.exists && !reference.isDraft) {
            markers.push(this.createMarker(reference.location, 8, `Read model '${reference.name}' not found`));
        } else if (reference.isDraft) {
            markers.push(this.createMarker(reference.location, 2, `Read model '${reference.name}' is a draft (not yet saved)`));
        }
    }

    private validateBody(lines: string[], root: { activeSchema: JsonSchema | undefined; readModelName: string | null }, markers: editor.IMarkerData[]): void {
        // These stacks intentionally remain independent: joins do not push a schema.
        const state = {
            contextStack: [root.activeSchema],
            indentStack: [0],
            lastIndent: 0,
            eventContextStack: [new Map<string, { lineNumber: number; col: number }>()],
        };

        // Keep starting at index 1, even when comments precede the header.
        for (let i = 1; i < lines.length; i++) {
            const text = lines[i];
            const trimmed = text.trim();
            if (!trimmed || trimmed.startsWith('#')) continue;

            const line = { text, trimmed, lineNumber: i + 1, indent: text.search(/\S/) };
            this.leaveNestedContexts(line, state);
            const currentEventContext = state.eventContextStack[state.eventContextStack.length - 1];

            // These are independent checks in source order, not an exclusive grammar dispatcher.
            this.validateEventReference(line, trimmed.match(/^from\s+([\w-]+)/), currentEventContext, markers);
            this.enterVariant(line, state, markers);
            this.validateEventReference(line, trimmed.match(/^enters\s+on\s+([\w-]+)/), currentEventContext, markers);
            this.validateEventReference(line, trimmed.match(/^events\s+([\w-]+)/), currentEventContext, markers);
            this.validateEventReference(line, trimmed.match(/^remove\s+(?:with|via\s+join\s+on)\s+([\w-]+)/), currentEventContext, markers);
            this.validatePropertyTargets(line, state.contextStack[state.contextStack.length - 1], markers);
            this.enterChildren(line, state, markers);
            this.validateJoin(line, root, state, markers);
            this.validateKey(line, root.activeSchema, markers);
        }
    }

    private leaveNestedContexts(line: { indent: number }, state: { contextStack: (JsonSchema | undefined)[]; indentStack: number[]; lastIndent: number; eventContextStack: Map<string, { lineNumber: number; col: number }>[] }): void {
        while (line.indent < state.lastIndent && state.indentStack.length > 1) {
            const poppedIndent = state.indentStack.pop();
            if (poppedIndent !== undefined && line.indent < poppedIndent) {
                state.contextStack.pop();
                state.eventContextStack.pop();
            }
        }
        state.lastIndent = line.indent;
    }

    private validateEventReference(line: { text: string; lineNumber: number }, match: RegExpMatchArray | null, currentEventContext: Map<string, { lineNumber: number; col: number }>, markers: editor.IMarkerData[]): void {
        if (!match) return;

        const eventType = match[1];
        const col = line.text.indexOf(eventType) + 1;
        const location = { lineNumber: line.lineNumber, col, length: eventType.length };
        if (Object.keys(this.eventSchemas).length > 0 && !this.eventSchemas[eventType]) {
            markers.push(this.createMarker(location, 8, `Event type '${eventType}' not found`));
        }
        if (currentEventContext.has(eventType)) {
            markers.push(this.createMarker(location, 8, `Duplicate event type '${eventType}' - event types can only be used once at each level`));
        } else {
            currentEventContext.set(eventType, { lineNumber: line.lineNumber, col });
        }
    }

    private enterVariant(line: { text: string; trimmed: string; lineNumber: number; indent: number }, state: { contextStack: (JsonSchema | undefined)[]; indentStack: number[]; eventContextStack: Map<string, { lineNumber: number; col: number }>[] }, markers: editor.IMarkerData[]): void {
        const variantMatch = line.trimmed.match(/^variant\s+([\w.]+)\s*$/);
        if (!variantMatch) return;

        const [, variantName] = variantMatch;
        const col = line.text.indexOf(variantName) + 1;
        const isDraft = this.isDraftReadModel(variantName);
        const readModelExists = this.resolveReadModelIdentifier(variantName) !== null;
        const location = { lineNumber: line.lineNumber, col, length: variantName.length };
        this.validateReadModelReference({ name: variantName, exists: readModelExists, isDraft, location }, markers);

        const variantSchema = isDraft ? this.draftReadModel!.schema : this.resolveReadModelInfo(variantName)?.schema;
        state.contextStack.push(variantSchema);
        state.indentStack.push(line.indent);
        state.eventContextStack.push(new Map());
    }

    private validatePropertyTargets(line: { text: string; trimmed: string; lineNumber: number }, schema: JsonSchema | undefined, markers: editor.IMarkerData[]): void {
        this.validatePropertyTarget(line, line.trimmed.match(/^(\w+)\s*=\s*(.+)$/), schema, markers);
        this.validatePropertyTarget(line, line.trimmed.match(/^(?:increment|decrement|count)\s+(\w+)$/), schema, markers);
        this.validatePropertyTarget(line, line.trimmed.match(/^(?:add|subtract)\s+(\w+)\s+by\s+/), schema, markers);
    }

    private validatePropertyTarget(line: { text: string; lineNumber: number }, match: RegExpMatchArray | null, schema: JsonSchema | undefined, markers: editor.IMarkerData[]): void {
        if (!match || !schema?.properties) return;

        const [, propertyName] = match;
        if (schema.properties[propertyName]) return;

        const location = { lineNumber: line.lineNumber, col: line.text.indexOf(propertyName) + 1, length: propertyName.length };
        const schemaName = this.getSchemaName(schema) || 'current schema';
        markers.push(this.createMarker(location, 4, `Property '${propertyName}' not found in ${schemaName}`));
    }

    private enterChildren(line: { text: string; trimmed: string; lineNumber: number; indent: number }, state: { contextStack: (JsonSchema | undefined)[]; indentStack: number[]; eventContextStack: Map<string, { lineNumber: number; col: number }>[] }, markers: editor.IMarkerData[]): void {
        const childrenMatch = line.trimmed.match(/^children\s+(\w+)\s+identified\s+by\s+/);
        const currentSchema = state.contextStack[state.contextStack.length - 1];
        if (!childrenMatch || !currentSchema?.properties) return;

        const [, propertyName] = childrenMatch;
        const prop = currentSchema.properties[propertyName];
        if (!prop) {
            const location = { lineNumber: line.lineNumber, col: line.text.indexOf(propertyName) + 1, length: propertyName.length };
            const schemaName = this.getSchemaName(currentSchema) || 'current schema';
            markers.push(this.createMarker(location, 4, `Collection property '${propertyName}' not found in ${schemaName}`));
            return;
        }
        if (prop.type !== 'array' && !prop.items) {
            const location = { lineNumber: line.lineNumber, col: line.text.indexOf(propertyName) + 1, length: propertyName.length };
            markers.push(this.createMarker(location, 4, `Property '${propertyName}' is not a collection (array) type`));
            return;
        }
        const childSchema = prop.items;
        if (!childSchema) return;

        state.contextStack.push(childSchema);
        state.indentStack.push(line.indent);
        state.eventContextStack.push(new Map());
    }

    private validateJoin(line: { text: string; trimmed: string; lineNumber: number; indent: number }, root: { activeSchema: JsonSchema | undefined; readModelName: string | null }, state: { indentStack: number[]; eventContextStack: Map<string, { lineNumber: number; col: number }>[] }, markers: editor.IMarkerData[]): void {
        const joinMatch = line.trimmed.match(/^join\s+(\w+)\s+on\s+(\w+)/);
        const { activeSchema, readModelName } = root;
        if (!joinMatch || !activeSchema?.properties) return;

        const [, collectionName, joinProperty] = joinMatch;
        const collectionProp = activeSchema.properties[collectionName];
        this.validateJoinCollection(line, { collectionName, collectionProp, readModelName }, markers);

        if (!activeSchema.properties[joinProperty]) {
            const col = line.text.indexOf(joinProperty, line.text.indexOf('on')) + 1;
            const location = { lineNumber: line.lineNumber, col, length: joinProperty.length };
            markers.push(this.createMarker(location, 4, `Property '${joinProperty}' not found in read model '${readModelName}'`));
        }

        // A join pushes an event context and indent, but never a schema context.
        state.eventContextStack.push(new Map());
        state.indentStack.push(line.indent);
    }

    private validateJoinCollection(line: { text: string; lineNumber: number }, collection: { collectionName: string; collectionProp: { type?: string; items?: JsonSchema } | undefined; readModelName: string | null }, markers: editor.IMarkerData[]): void {
        const { collectionName, collectionProp, readModelName } = collection;
        if (!collectionProp) {
            const location = { lineNumber: line.lineNumber, col: line.text.indexOf(collectionName) + 1, length: collectionName.length };
            markers.push(this.createMarker(location, 4, `Collection property '${collectionName}' not found in read model '${readModelName}'`));
        } else if (collectionProp.type !== 'array' && !collectionProp.items) {
            const location = { lineNumber: line.lineNumber, col: line.text.indexOf(collectionName) + 1, length: collectionName.length };
            markers.push(this.createMarker(location, 4, `Property '${collectionName}' is not a collection (array) type`));
        }
    }

    private validateKey(line: { text: string; trimmed: string; lineNumber: number }, activeSchema: JsonSchema | undefined, markers: editor.IMarkerData[]): void {
        const keyMatch = line.trimmed.match(/^key\s+(\w+)\s*$/);
        if (!keyMatch || !activeSchema) return;

        const [, keyName] = keyMatch;
        const isInDefinitions = activeSchema.definitions && activeSchema.definitions[keyName];
        const isProperty = activeSchema.properties && activeSchema.properties[keyName];
        if (isInDefinitions || isProperty) return;
        if (this.isBuiltInExpression(keyName)) return;

        const location = { lineNumber: line.lineNumber, col: line.text.indexOf(keyName) + 1, length: keyName.length };
        markers.push(this.createMarker(location, 4, `Composite key type '${keyName}' not found in read model schema`));
    }

    private appendEventContextMarkers(lines: string[], markers: editor.IMarkerData[]): void {
        // Shared path checks stay after all local diagnostics, including later lines.
        for (const issue of validateEventContextPaths(lines, fenceMap(lines))) {
            markers.push({
                severity: issue.severity === 'error' ? 8 : 4,
                startLineNumber: issue.line + 1,
                endLineNumber: issue.line + 1,
                startColumn: issue.startColumn,
                endColumn: issue.endColumn,
                message: issue.message,
                code: issue.code,
            });
        }
    }

    private resolveReadModelInfo(readModelToken: string): ReadModelInfo | null {
        return this.readModels.find(rm => rm.identifier === readModelToken || rm.displayName === readModelToken) || null;
    }

    private getReadModelStartColumn(firstLine: string, readModelName: string): number {
        const prefixMatch = firstLine.match(/^\s*projection\s+[\w.]+\s*=>\s*/);
        if (!prefixMatch) {
            const fallbackIndex = firstLine.lastIndexOf(readModelName);
            return (fallbackIndex >= 0 ? fallbackIndex : 0) + 1;
        }
        return prefixMatch[0].length + 1;
    }

    private resolveReadModelIdentifier(readModelToken: string): string | null {
        if (this.isDraftReadModel(readModelToken)) {
            return this.draftReadModel!.identifier;
        }
        const readModel = this.resolveReadModelInfo(readModelToken);
        return readModel?.identifier ?? null;
    }

    private isDraftReadModel(readModelToken: string): boolean {
        return !!this.draftReadModel && (this.draftReadModel.identifier === readModelToken || this.draftReadModel.displayName === readModelToken);
    }

    private isBuiltInExpression(text: string): boolean {
        return text.startsWith('$') || ['true', 'false', 'null'].includes(text);
    }

    private createMarker(location: { lineNumber: number; col: number; length: number }, severity: number, message: string): editor.IMarkerData {
        return {
            severity,
            startLineNumber: location.lineNumber,
            startColumn: location.col,
            endLineNumber: location.lineNumber,
            endColumn: location.col + location.length,
            message,
        };
    }

    private getSchemaName(schema: JsonSchema): string | undefined {
        if (schema.name) return schema.name;
        if (schema.title) return schema.title;
        if (typeof schema.$id === 'string') {
            const parts = schema.$id.split('/');
            return parts[parts.length - 1] || schema.$id;
        }
        return undefined;
    }
}
