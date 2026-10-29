// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { CommandSyntax } from '../Syntax/Commands';
import { HandlerSyntax } from '../Syntax/Implementations';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function isPendingHandler(handler: HandlerSyntax): boolean {
    return handler.implementation != null && handler.file === null && handler.code === null;
}

export function validateHandlerOccurrences(occurrences: readonly { handler: HandlerSyntax | null; location: SourceLocation }[], context: ParserContext): void {
    const hasPending = occurrences.some(entry => entry.handler !== null && isPendingHandler(entry.handler));
    const code = hasPending ? DiagnosticCodes.PendingIntentWithOtherHandler : DiagnosticCodes.DuplicateDeclaration;
    const message = hasPending
        ? 'A pending handler intent cannot coexist with another handler on the same command.'
        : 'A command cannot declare more than one handler.';
    for (const entry of occurrences.slice(1)) {
        context.error(code, message, entry.location);
    }
}

export function validateApplicationHandlers(application: ApplicationSyntax, context: ParserContext): void {
    const validateFeature = (feature: FeatureSyntax): void => {
        for (const slice of feature.slices) validateCommandHandlers(slice.commands, context);
        feature.features.forEach(validateFeature);
    };
    for (const module of application.modules) module.features.forEach(validateFeature);
}

export function validateHandlerContracts(handlers: readonly { handler: HandlerSyntax | null; location: SourceLocation }[], handler: HandlerSyntax | null, hasProduces: boolean, context: ParserContext): void {
    validateHandlerOccurrences(handlers, context);
    validatePossibleEvents(handler, hasProduces, context);
}

export function validatePossibleEvents(handler: HandlerSyntax | null, hasProduces: boolean, context: ParserContext): void {
    if (hasProduces && handler?.possibleEvents != null) {
        context.error(DiagnosticCodes.InvalidPossibleEventsDeclaration, "A command cannot combine 'produces' with a handler 'may produce' set.", handler.possibleEvents.location);
    }
}

export function validateCommandHandlers(commands: readonly CommandSyntax[], context: ParserContext): void {
    const groups = new Map<string, CommandSyntax[]>();
    for (const command of commands) groups.set(command.name, [...(groups.get(command.name) ?? []), command]);
    for (const group of groups.values()) {
        validateHandlerOccurrences(group.flatMap(command => command.handler == null ? [] : [{ handler: command.handler, location: command.handler.location }]), context);
        const hasProduces = group.some(command => command.produces.length > 0);
        group.forEach(command => validatePossibleEvents(command.handler, hasProduces, context));
    }
}
