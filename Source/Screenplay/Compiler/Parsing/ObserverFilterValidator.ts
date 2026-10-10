// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { EventSourceCatalog, EventSourceResolutionKind } from '../Syntax/EventSourceCatalog';
import { ObserverFilterSyntax } from '../Syntax/ObserverFilterSyntax';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validateObserverFilters(application: ApplicationSyntax, context: ParserContext, resolver: AuthoringProductionResolver): void {
    const catalog = new EventSourceCatalog(application);
    const resolve = (filter: ObserverFilterSyntax): boolean => {
        const resolution = catalog.resolve(filter.eventSource, filter.stream ?? undefined);
        if (resolution.kind === EventSourceResolutionKind.Unique) return true;
        context.error(DiagnosticCodes.InvalidObserverFilter, 'An observer filter must name one physical event source and, when supplied, one stream belonging to it.', filter.location);
        return false;
    };
    for (const { slice } of resolver.slices) {
        for (const reaction of slice.reactions) {
            if (reaction.from == null || !resolve(reaction.from)) continue;
            for (const trigger of reaction.triggers) {
                if (trigger.source.kind !== 'NamedTriggerSourceSyntax' || resolver.resolve(trigger.source.name, slice).declaration?.node.kind !== 'EventSyntax')
                    context.error(DiagnosticCodes.InvalidObserverFilter, "A filtered reaction requires only 'when <Event>' triggers on declared events.", trigger.location);
            }
        }
        for (const reducer of dependencySourcesOf(slice).reducers ?? []) {
            if (reducer.from != null) resolve(reducer.from);
        }
    }
}
