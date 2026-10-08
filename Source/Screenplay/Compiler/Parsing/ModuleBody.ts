// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { SpecificationExampleSyntax } from '../Syntax/Specifications';
import { DependsOnSyntax, FeatureSyntax, FileImportSyntax, ModuleSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { combineAuthorize, parseAuthorize } from './AuthorizeParser';
import { parseDescription } from './DescriptionParser';
import { parseDependsOn } from './DependsOnParser';
import { parseFeature } from './FeatureBody';
import { parseFileImport } from './FileImportParser';
import { collectInputUses } from './InputUses';
import { firstWord } from './LineText';
import { parseExample } from './SpecificationExampleParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

export const modulePattern = pattern('^module\\s+([A-Za-z_]\\w*)$');

// Module members the C# compiler knows that this compiler does not model. They are skipped whole, not reported.
const opaqueModuleMembers = new Set(['on', 'uses', 'screen', 'dialog', 'form', 'contribute']);

// What a module body may hold, as it reads in a diagnostic.
export const moduleBodyExpected = 'description, depends on <Name>, authorize, import, screen template, dialog template, form, contribute, feature, example, \'on <trigger>\' or \'uses <Behavior>\'';

// Collects the body of a module - written beneath a 'module' header, or at the top level of a file imported
// into the module. The port of the C# ModuleBody.
export class ModuleBody {
    readonly #features: FeatureSyntax[] = [];
    readonly #examples: SpecificationExampleSyntax[] = [];
    readonly #fileImports: FileImportSyntax[] = [];
    readonly #dependsOn: DependsOnSyntax[] = [];
    #description: string | null = null;
    #authorize: AuthorizeSyntax | null = null;

    constructor(readonly name: string) {}

    // Parses one already consumed line of the body; false when the line does not belong to a module body,
    // and nothing was reported.
    tryParse(context: ParserContext, line: SourceLine): boolean {
        const keyword = firstWord(line.content);
        switch (keyword) {
            case 'description':
                this.#description = parseDescription(context, line, this.#description, `Module '${this.name}'`);
                return true;
            case 'depends':
                parseDependsOn(context, line, this.#dependsOn, DiagnosticCodes.UnknownModuleDirective);
                return true;
            case 'authorize':
                this.#authorize = combineAuthorize(this.#authorize, parseAuthorize(context, line));
                return true;
            case 'import':
                parseFileImport(context, line, this.#fileImports);
                return true;
            case 'example':
                this.#examples.push(parseExample(context, line));
                return true;
            case 'feature':
                this.#features.push(parseFeature(context, line));
                return true;
            default:
                if (opaqueModuleMembers.has(keyword)) {
                    collectInputUses(context, line);
                    return true;
                }
                return false;
        }
    }

    // Parses the children of a header line into this body, reporting what does not belong in a module.
    parseChildren(context: ParserContext, line: SourceLine): void {
        for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
            context.reader.takeSignificant();
            if (!this.tryParse(context, child)) {
                context.error(DiagnosticCodes.UnknownModuleDirective, `Unexpected '${firstWord(child.content)}' in module body - expected ${moduleBodyExpected}`, locationOf(child));
                context.skipBlock(child.indent);
            }
        }
    }

    // Builds the module, located at its header or, when it only places an imported file, at that file.
    build(location: SourceLocation, isPlacement = false): ModuleSyntax {
        return {
            kind: 'ModuleSyntax',
            name: this.name,
            description: this.#description,
            authorize: this.#authorize,
            dependsOn: this.#dependsOn,
            features: this.#features,
            examples: this.#examples,
            fileImports: this.#fileImports,
            isPlacement,
            location,
        };
    }
}

// Parses a module from its already consumed header line.
export function parseModule(context: ParserContext, line: SourceLine): ModuleSyntax {
    const name = modulePattern.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidModuleDeclaration, `Invalid module declaration '${line.content}' - expected 'module <Name>'`, locationOf(line));
    }
    const body = new ModuleBody(name);
    const previous = context.scope;
    context.scope = [name];
    body.parseChildren(context, line);
    context.scope = previous;
    return body.build(locationOf(line));
}
