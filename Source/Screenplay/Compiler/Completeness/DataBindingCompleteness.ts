// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { RefusalDeclarations } from '../Parsing/RefusalDeclarations';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { QuerySyntax } from '../Syntax/Queries';
import { ScreenDataSyntax, ScreenDirectiveSyntax } from '../Syntax/Screens';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';

interface Binding {
    readonly data: ScreenDataSyntax;
    readonly query: QuerySyntax;
}

// The C# view identity includes inferred projection/variant and reducer names, not just readmodel shapes.
function viewNames(slice: SliceSyntax): readonly { name: string }[] {
    const names = [...slice.readModels.map(model => model.name),
        ...(dependencySourcesOf(slice).reducers ?? []).map(reducer => reducer.readModel),
        ...slice.projections.flatMap(projection => {
            const variants = projection.blocks.filter(block => block.kind === 'ProjectionVariantSyntax');
            return variants.length > 0 ? variants.map(variant => variant.name) : [projection.readModel ?? projection.name];
        })];
    return [...new Set(names)].map(name => ({ name }));
}

export function dataBindingDiagnostics(application: ApplicationSyntax): readonly Diagnostic[] {
    const declarations = new RefusalDeclarations(application);
    const diagnostics: Diagnostic[] = [];
    const view = (name: string, slice: SliceSyntax) => declarations.resolve(name, slice, viewNames);
    const sameView = (first: NonNullable<ReturnType<typeof view>>, second: NonNullable<ReturnType<typeof view>>) =>
        first.node.name === second.node.name && declarations.slices.find(entry => entry.slice === first.slice)!.scope.join('.') === declarations.slices.find(entry => entry.slice === second.slice)!.scope.join('.');

    const checkContainer = (directives: readonly ScreenDirectiveSyntax[], ancestors: readonly Binding[], slice: SliceSyntax): void => {
        const visible = [...ancestors];
        for (const data of directives.filter(directive => directive.kind === 'ScreenDataSyntax')) {
            const query = declarations.resolve(data.query, slice, candidate => candidate.queries);
            if (query === null) continue;
            if (visible.some(previous => previous.data.type.name === data.type.name &&
                (previous.data.type.isCollection !== data.type.isCollection || previous.query !== query.node || previous.data.by !== data.by))) {
                diagnostics.push({ severity: 'warning', code: DiagnosticCodes.ConflictingScreenDataBinding,
                    message: `Data binding '${data.type.name}' conflicts with another visible binding - use the same cardinality, query and by parameter, or separate containers`, location: data.location });
            }
            visible.push({ data, query: query.node });
            const bound = view(data.type.name, slice);
            const returned = view(query.node.returnType.name, query.slice);
            if (bound !== null && returned !== null && (!sameView(bound, returned) || data.type.isCollection !== query.node.returnType.isCollection)) {
                diagnostics.push({ severity: 'warning', code: DiagnosticCodes.ScreenDataQueryMismatch,
                    message: `Data binding '${data.type.name}' does not match query '${data.query}' return type or cardinality`, location: data.location });
            }
        }
        // Only sections and filled template slots are containers in the C# check. Component outlets and
        // screen contributions deliberately do not extend data-binding visibility.
        for (const directive of directives) {
            if (directive.kind === 'ScreenSectionSyntax') checkContainer(directive.directives, visible, slice);
            if (directive.kind === 'ScreenTemplateReferenceSyntax') {
                for (const slot of directive.slots) checkContainer(slot.directives, visible, slice);
            }
        }
    };
    for (const { slice } of declarations.slices) {
        for (const screen of slice.screens) checkContainer(screen.directives, [], slice);
    }
    return diagnostics;
}
