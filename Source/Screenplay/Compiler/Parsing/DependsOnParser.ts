// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { DependsOnSyntax } from '../Syntax/Structure';
import { dotNetWhitespace, nativePattern } from '../Text/patterns';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const declaration = nativePattern(`^depends${dotNetWhitespace}+on${dotNetWhitespace}+([A-Za-z_]\\w*(?:\\.[A-Za-z_]\\w*)*)$`);

export function parseDependsOn(context: ParserContext, line: SourceLine, declarations: DependsOnSyntax[], errorCode: string): void {
    const target = declaration.exec(line.content)?.[1];
    if (target === undefined) {
        context.error(errorCode, `Invalid dependency declaration '${line.content}' - expected 'depends on <Name>'`, locationOf(line));
        context.skipBlock(line.indent);
        return;
    }
    if (declarations.some(dependency => dependency.target === target)) {
        context.warning(DiagnosticCodes.RepeatedDependencyDeclaration, `Dependency '${target}' is already declared on this container - this repeated declaration is ignored`, locationOf(line));
        return;
    }
    declarations.push({ kind: 'DependsOnSyntax', target, location: locationOf(line) });
}
