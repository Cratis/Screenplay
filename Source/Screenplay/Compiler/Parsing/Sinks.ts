// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax, ReadModelSyntax } from '../Syntax/Declarations';
import { SinkKind, SinkSyntax } from '../Syntax/Sinks';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

// The ports of the C# SinkParser, SinkResolution and SinkValidator, kept in one module for the editor bundle.
export const sinkKeyword = 'sink';

const declarationPattern = pattern('^sink\\s+([A-Za-z_]\\w*)\\s+for\\s+(readmodels|graphs)$');
const directivePattern = pattern('^sink\\s+([A-Za-z_]\\w*)$');

// 'sink <Name> for readmodels|graphs' at the application root.
export function parseSinkDeclaration(context: ParserContext, line: SourceLine): SinkSyntax | undefined {
    const match = declarationPattern.exec(line.content);
    const child = context.peekChild(line.indent);
    if (child !== undefined) {
        context.error(DiagnosticCodes.InvalidSinkDeclaration, `A sink is declared on one line`, locationOf(child));
        context.skipBlock(line.indent);
    }
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSinkDeclaration, `Invalid sink declaration '${line.content}' - expected 'sink <Name> for readmodels|graphs'`, locationOf(line));
        return undefined;
    }
    return { kind: 'SinkSyntax', name: match[1], stores: match[2] === 'graphs' ? 'Graphs' : 'ReadModels', location: locationOf(line) };
}

// The 'sink' lines an owner has named so far.
export interface SinkDirectiveState {
    sink?: string;
    location?: SourceLocation;
}

// Records one 'sink <Name>' line of an owner, reporting a repeat (PLAY0937) or a second sink (PLAY0936).
function nameSink(context: ParserContext, state: SinkDirectiveState, name: string, location: SourceLocation, owner: string): void {
    if (state.sink === undefined) {
        state.sink = name;
        state.location = location;
    } else if (state.sink === name) {
        context.warning(DiagnosticCodes.RepeatedSinkDirective, `${owner} repeats 'sink ${name}'; keep one directive.`, location);
    } else {
        context.error(DiagnosticCodes.ConflictingSinkDirective, `${owner} names sink '${state.sink}' and sink '${name}'`, location);
    }
}

// 'sink <Name>' in a graph body. A read model's line is resolved once the sinks are known.
export function parseSinkDirective(context: ParserContext, line: SourceLine, state: SinkDirectiveState, owner: string): void {
    const match = directivePattern.exec(line.content);
    if (match === null) context.error(DiagnosticCodes.InvalidSinkDeclaration, `Invalid sink directive '${line.content}'`, locationOf(line));
    else nameSink(context, state, match[1], locationOf(line), owner);
}

// Whether a property is written the way the directive is: 'sink <Type>' with no escape, collection, optional or modifier.
const hasDirectiveShape = (property: PropertySyntax): boolean =>
    property.name === sinkKeyword && !property.nameWasEscaped && !property.isIdentifier && !property.isGenerated
    && !property.isSubject && !property.isKey && !property.type.isCollection && !property.type.isOptional;

// Turns a read model's 'sink <Name>' property into the directive when <Name> is a declared sink (exact match).
// Runs after a document is parsed and after a folder is merged; a resolved line is no longer a property.
export function resolveSinks(application: ApplicationSyntax, context: ParserContext): ApplicationSyntax {
    const exact = new Set((application.sinks ?? []).map(sink => sink.name));
    if (exact.size === 0) return application;
    const readModel = (model: ReadModelSyntax): ReadModelSyntax => {
        const state: SinkDirectiveState = { sink: model.sink, location: model.directiveLocations?.sink };
        const properties = model.properties.filter(property => {
            if (!hasDirectiveShape(property) || !exact.has(property.type.name)) return true;
            nameSink(context, state, property.type.name, property.location, `Read model '${model.name}'`);
            return false;
        });
        if (properties.length === model.properties.length) return model;
        if (model.isPassive === true && model.sink === undefined) {
            context.error(DiagnosticCodes.PassiveReadModelSink, `Passive read model '${model.name}' cannot name sink '${state.sink}'`, state.location!);
        }
        return { ...model, properties, sink: state.sink, directiveLocations: { ...model.directiveLocations, sink: state.location! } };
    };
    const features = (list: readonly FeatureSyntax[]): readonly FeatureSyntax[] => {
        const result = list.map(feature => {
            const nested = features(feature.features);
            const slices = feature.slices.map(slice => {
                const readModels = slice.readModels.map(readModel);
                return readModels.every((model, index) => model === slice.readModels[index]) ? slice : { ...slice, readModels };
            });
            return nested === feature.features && slices.every((slice, index) => slice === feature.slices[index]) ? feature : { ...feature, features: nested, slices };
        });
        return result.every((feature, index) => feature === list[index]) ? list : result;
    };
    const modules = application.modules.map(module => {
        const resolved = features(module.features);
        return resolved === module.features ? module : { ...module, features: resolved };
    });
    return modules.every((module, index) => module === application.modules[index]) ? application : { ...application, modules };
}

// Holds the declared sinks to what names them: duplicates ignoring case (PLAY0932), unknown (PLAY0933), kind
// mismatch (PLAY0934), case-only near misses (PLAY0939) and unused sinks (PLAY0938).
export function validateSinks(application: ApplicationSyntax, context: ParserContext): void {
    const sinks = new Map<string, SinkSyntax>();
    const seen = new Map<string, SinkSyntax>();
    for (const sink of application.sinks ?? []) {
        const key = sink.name.toLowerCase();
        if (seen.has(key)) context.error(DiagnosticCodes.DuplicateSink, `Sink '${sink.name}' is declared more than once`, sink.location);
        else {
            seen.set(key, sink);
            sinks.set(sink.name, sink);
        }
    }
    const used = new Set<string>();
    const check = (name: string, kind: SinkKind, owner: string, location: SourceLocation): void => {
        const sink = sinks.get(name);
        if (sink === undefined) {
            context.error(DiagnosticCodes.UnknownSink, `${owner} names unknown sink '${name}' - declare it with 'sink ${name} for ${kind.toLowerCase()}'`, location);
            return;
        }
        used.add(name);
        if (sink.stores !== kind) context.error(DiagnosticCodes.SinkKindMismatch, `${owner} names sink '${name}', which is declared 'for ${sink.stores.toLowerCase()}'`, location);
    };
    const visit = (list: readonly FeatureSyntax[]): void => list.forEach(feature => {
        visit(feature.features);
        for (const slice of feature.slices) {
            for (const model of slice.readModels) {
                if (model.sink !== undefined) check(model.sink, 'ReadModels', `Read model '${model.name}'`, model.directiveLocations?.sink ?? model.location);
                for (const property of model.properties.filter(hasDirectiveShape)) {
                    const nearMiss = seen.get(property.type.name.toLowerCase());
                    if (nearMiss !== undefined && nearMiss.name !== property.type.name) {
                        context.warning(DiagnosticCodes.SinkNameCaseMismatch, `Read model '${model.name}' property 'sink' type '${property.type.name}' differs only in case from sink '${nearMiss.name}'`, property.location);
                    }
                }
            }
            for (const graph of slice.graphs ?? []) if (graph.sink !== undefined) check(graph.sink, 'Graphs', `Graph '${graph.name}'`, graph.directiveLocations?.sink ?? graph.location);
        }
    });
    application.modules.forEach(module => visit(module.features));
    for (const sink of sinks.values()) {
        if (!used.has(sink.name)) context.warning(DiagnosticCodes.UnusedSink, `Sink '${sink.name}' is not used`, sink.location);
    }
}
