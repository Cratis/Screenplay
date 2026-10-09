// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionKind, AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { EventSyntax, ImportSyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { EventVisibility } from '../Syntax/EventVisibility';
import { isEventsSource } from '../Syntax/CaptureEventsSource';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { TranslationDirection } from '../Syntax/TranslationDirection';
import { ParserContext } from './ParserContext';
import { PublicEventUsageCollector } from './PublicEventUsageCollector';
import { validatePublicEventMetadata } from './PublicEventMetadataValidator';

/** Validates public contract boundaries against the complete assembled declaration inventory. */
export function validatePublicEventUsage(application: ApplicationSyntax, context: ParserContext): void {
    validatePublicEventMetadata(application, context);
    const resolver = new AuthoringProductionResolver(application);
    if (!resolver.declarations.some(declaration => declaration.node.kind === 'EventSyntax' && ((declaration.node as EventSyntax).visibility === EventVisibility.Public || (declaration.node as EventSyntax).origin != null)) &&
        !application.imports.some(imported => imported.visibility === EventVisibility.Public || imported.origin != null) &&
        !resolver.slices.some(({ slice }) => slice.direction != null || usesEventSource(slice) || targetsEvent(slice, resolver))) return;
    const events = new AuthoringProductionResolver(application, resolver.declarations.filter(declaration => declaration.kind === AuthoringProductionKind.Event));
    for (const { slice, scope } of resolver.slices) validateSlice(application, slice, scope, resolver, events, context);
    for (const seed of application.seeds?.flatMap(seed => seed.groups.flatMap(group => group.events)) ?? []) {
        const contract = resolve(application, events, [], seed.event, seed, true, context);
        if (contract?.visibility !== EventVisibility.Public) continue;
        const code = contract.origin == null ? DiagnosticCodes.PublicEventRequiresOutboundTranslation : DiagnosticCodes.ForeignPublicEventProduced;
        context.error(code, `Seed cannot append public event '${seed.event}'; public production belongs to outbound translation, and foreign contracts cannot be produced locally.`, seed.location);
    }
}

function validateSlice(application: ApplicationSyntax, slice: SliceSyntax, scope: readonly string[], resolver: AuthoringProductionResolver, events: AuthoringProductionResolver, context: ParserContext): void {
    const collector = new PublicEventUsageCollector();
    collector.visitSlice(slice);
    const contracts: { node: EventSyntax | ImportSyntax; output: boolean }[] = [];
    const explicitOutbound = slice.type === 'Translate' && slice.direction === TranslationDirection.Outbound;
    const readModels = new Set(slice.readModels.map(readModel => readModel.name));
    for (const { target, node } of collector.projectionTargets) {
        // '=> Name' names a read model unless it resolves to an event: then the projection or reducer publishes that event.
        const contract = readModels.has(target) ? undefined : resolve(application, events, scope, target, node, false, context);
        if (contract === undefined) continue;
        if (!explicitOutbound) {
            context.error(DiagnosticCodes.EventTargetOutsideOutboundTranslation, `'${target}' is an event: a projection or reducer may only target an event in an explicitly outbound Translate slice.`, node.location);
            continue;
        }
        contracts.push({ node: contract, output: true });
        validateUse(slice, target, node, true, false, contract, context);
    }
    // An outbound slice with a capture is already refused as a whole by the translation-direction check below.
    if (!(slice.type === 'Translate' && slice.direction === TranslationDirection.Inbound) && !explicitOutbound) {
        for (const source of collector.eventSources) context.error(DiagnosticCodes.EventsSourceOutsideInboundTranslation, `'source events' reads another application's public events and belongs to a Translate slice with 'direction inbound', not '${slice.name}'.`, source.location);
    }
    for (const use of collector.uses) {
        const contract = resolve(application, use.node.kind === 'ProducesSyntax' ? resolver : events, scope, use.name, use.node, use.output, context);
        if (contract === undefined) continue;
        contracts.push({ node: contract, output: use.output });
        validateUse(slice, use.name, use.node, use.output, use.command, contract, context);
    }
    // 'all' subscribes to every event type; 'every' maps only the projection's existing inputs.
    for (const all of collector.allEvents) {
        const declarations = resolver.declarations.filter(declaration => declaration.kind === AuthoringProductionKind.Event).map(declaration => declaration.node as EventSyntax);
        for (const contract of [...declarations, ...application.imports.filter(imported => imported.visibility === EventVisibility.Public)]) {
            contracts.push({ node: contract, output: false });
            validateUse(slice, contract.kind === 'ImportSyntax' ? contract.qualifiedName : contract.name, all, false, false, contract, context);
        }
    }
    if (slice.type !== 'Translate') return;
    if (slice.direction == null && (eventDeclarations(slice).some(event => event.visibility === EventVisibility.Public) || contracts.some(contract => contract.node.visibility === EventVisibility.Public))) {
        context.error(DiagnosticCodes.PublicTranslationRequiresDirection, "A Translate slice using public events requires an explicit 'direction inbound' or 'direction outbound'. Legacy translations without public metadata remain inbound.", slice.location);
    }
    if (slice.direction !== TranslationDirection.Outbound) return;
    const outputs = new Set(contracts.filter(contract => contract.output && contract.node.visibility === EventVisibility.Public && contract.node.origin == null).map(contract => contract.node)).size;
    if (outputs !== 1) context.error(DiagnosticCodes.OutboundPublicEventCount, `Outbound Translate slice '${slice.name}' must produce exactly one local public event type; found ${outputs}.`, slice.location);
    for (const capture of slice.captures) context.error(DiagnosticCodes.TranslationConstructDirection, 'An outbound translation consumes private local events, not an external-data capture. Capture belongs to inbound translation.', capture.location);
}

function validateUse(slice: SliceSyntax, name: string, node: SyntaxNode, output: boolean, command: boolean, contract: EventSyntax | ImportSyntax, context: ParserContext): void {
    const foreign = contract.origin != null;
    const publicEvent = contract.visibility === EventVisibility.Public;
    const error = (code: string, message: string) => context.error(code, message, node.location);
    if (output) {
        if (command && publicEvent) error(DiagnosticCodes.CommandProducesPublicEvent, `Command cannot produce public event '${name}'; publish through an outbound Translate slice.`);
        if (slice.type === 'Translate' && slice.direction === TranslationDirection.Outbound && !publicEvent) error(DiagnosticCodes.OutboundTranslationOutput, `Outbound translation must produce its one local public event, not private event '${name}'.`);
        if (foreign) error(DiagnosticCodes.ForeignPublicEventProduced, `Foreign public event '${name}' cannot be produced locally; translate it into a private local event.`);
        if (publicEvent && !foreign && (slice.type !== 'Translate' || slice.direction !== TranslationDirection.Outbound)) error(DiagnosticCodes.PublicEventRequiresOutboundTranslation, `Local public event '${name}' may only be produced by an explicitly outbound Translate slice.`);
        if (slice.type === 'Translate' && (slice.direction ?? TranslationDirection.Inbound) === TranslationDirection.Inbound && (publicEvent || foreign)) error(DiagnosticCodes.InboundTranslationOutput, `Inbound translation must produce private local events, not '${name}'.`);
    } else {
        if (foreign && (slice.type !== 'Translate' || slice.direction !== TranslationDirection.Inbound)) error(DiagnosticCodes.ForeignPublicEventConsumer, `Foreign public event '${name}' may only be consumed by an explicitly inbound Translate slice.`);
        if (slice.type === 'Translate' && slice.direction === TranslationDirection.Outbound && (publicEvent || foreign)) error(DiagnosticCodes.OutboundTranslationInput, `Outbound translation must consume private local events, not '${name}'.`);
        if (slice.type === 'Translate' && slice.direction === TranslationDirection.Inbound && (!publicEvent || !foreign)) error(DiagnosticCodes.InboundTranslationInput, `Explicitly inbound translation must consume foreign public events, not '${name}'.`);
    }
}

function resolve(application: ApplicationSyntax, resolver: AuthoringProductionResolver, scope: readonly string[], name: string, use: SyntaxNode, output: boolean, context: ParserContext): EventSyntax | ImportSyntax | undefined {
    const resolution = resolver.resolveFromScope(name, scope);
    if (resolution.declaration?.node.kind === 'EventSyntax') return resolution.declaration.node as EventSyntax;
    if (resolution.kind === AuthoringProductionKind.Operation) return undefined;
    if (resolution.kind === AuthoringProductionKind.Ambiguous) {
        context.warning(DiagnosticCodes.AmbiguousReference, `Ambiguous event reference '${name}'; qualify it with its owning module, feature and slice.`, use.location);
        return undefined;
    }
    const imports = application.imports.filter(imported => imported.qualifiedName.split('.').at(-1) === name || imported.qualifiedName === name);
    if (imports.length === 1) {
        const imported = imports[0];
        const local = resolver.resolveFromScope(imported.qualifiedName, scope);
        if (local.declaration?.node.kind === 'EventSyntax') return local.declaration.node as EventSyntax;
        if (local.kind === AuthoringProductionKind.Ambiguous) {
            context.warning(DiagnosticCodes.AmbiguousReference, `Ambiguous imported event reference '${name}'; qualify its declaration.`, use.location);
            return undefined;
        }
        return imported.visibility === EventVisibility.Public ? imported : undefined;
    }
    if (imports.length > 1) context.warning(DiagnosticCodes.AmbiguousReference, `Ambiguous imported event reference '${name}'; use its qualified name.`, use.location);
    else if (((output && use.kind !== 'CaptureAppendSyntax') || use.kind === 'CaptureSourceSettingSyntax') && !context.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.UnknownEvent && diagnostic.location.path === use.location.path && diagnostic.location.line === use.location.line && diagnostic.location.column === use.location.column)) {
        context.warning(DiagnosticCodes.UnknownEvent, `Unknown event '${name}' - declare its contract before classifying its usage.`, use.location);
    }
    return undefined;
}

function usesEventSource(slice: SliceSyntax): boolean {
    return slice.captures.some(capture => capture.source != null && isEventsSource(capture.source));
}

function targetsEvent(slice: SliceSyntax, resolver: AuthoringProductionResolver): boolean {
    const targets = [...slice.projections.map(projection => projection.readModel), ...(dependencySourcesOf(slice).reducers ?? []).map(reducer => reducer.readModel)];
    return targets.some(target => target != null && resolver.declarations.some(declaration => declaration.kind === AuthoringProductionKind.Event && declaration.name === target));
}
