// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { SpecificationExampleSyntax } from '../Syntax/Specifications';
import { DependsOnSyntax, FeatureSyntax, FileImportSyntax, ModuleSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { combineAuthorize, parseAuthorize, recordAuthorizationRefusal } from './AuthorizeParser';
import { parseDescription } from './DescriptionParser';
import { PurposeReferenceSyntax } from '../Syntax/Purposes';
import { parsePurposeReference } from './PurposeParser';
import { parseDocumentation } from './DocumentationParser';
import { parseDependsOn } from './DependsOnParser';
import { parseFeature } from './FeatureBody';
import { parseFileImport } from './FileImportParser';
import { parseProvide } from './ProvideParser';
import { ProvideSyntax } from '../Syntax/ProvideSyntax';
import { reportFeatureToolbar } from './ScreenParser';
import { addOutletOverride, isNavigationRegion, reportNavigationRegion } from './OutletParser';
import type { OutletSyntax } from '../Syntax/Outlets';
import { collectInputUses } from './InputUses';
import { firstWord } from './LineText';
import { parseExample } from './SpecificationExampleParser';
import { ParserContext } from './ParserContext';
import { misplacedReactionIdentity } from './ReactionIdentityParser';
import { locationOf, SourceLine } from './SourceLine';

export const modulePattern = pattern('^module\\s+([A-Za-z_]\\w*)$');

// Module members the C# compiler knows that this compiler does not model. They are skipped whole, not reported.
const opaqueModuleMembers = new Set(['on', 'uses', 'screen', 'dialog', 'form', 'contribute', 'navigation', 'template']);

// What a module body may hold, as it reads in a diagnostic.
export const moduleBodyExpected = 'description, documentation, depends on <Name>, authorize, provide, import, screen template, dialog template, form, contribute, outlet, feature, example, \'on <trigger>\' or \'uses <Behavior>\'';

// Collects the body of a module - written beneath a 'module' header, or at the top level of a file imported
// into the module. The port of the C# ModuleBody.
export class ModuleBody {
    readonly #features: FeatureSyntax[] = [];
    readonly #examples: SpecificationExampleSyntax[] = [];
    readonly #fileImports: FileImportSyntax[] = [];
    readonly #dependsOn: DependsOnSyntax[] = [];
    readonly #purposes: PurposeReferenceSyntax[] = [];
    readonly #outlets: OutletSyntax[] = [];
    readonly #provides: ProvideSyntax[] = [];
    #description: string | null = null;
    #documentation: string | null = null;
    #authorize: AuthorizeSyntax | null = null;
    #authorizationDepthExceeded = false;

    constructor(readonly name: string) {}

    // Parses one already consumed line of the body; false when the line does not belong to a module body,
    // and nothing was reported.
    tryParse(context: ParserContext, line: SourceLine): boolean {
        const keyword = firstWord(line.content);
        switch (keyword) {
            case 'runs':
                misplacedReactionIdentity(context, line);
                return true;
            case 'description':
                this.#description = parseDescription(context, line, this.#description, `Module '${this.name}'`);
                return true;
            case 'documentation':
                this.#documentation = parseDocumentation(context, line, this.#documentation, `Module '${this.name}'`);
                return true;
            case 'depends':
                parseDependsOn(context, line, this.#dependsOn, DiagnosticCodes.UnknownModuleDirective);
                return true;
            case 'purpose':
                parsePurposeReference(context, line, this.#purposes);
                return true;
            case 'authorize': {
                const diagnosticCount = context.diagnostics.length;
                const next = parseAuthorize(context, line);
                if (next === null && context.diagnostics.slice(diagnosticCount).some(diagnostic => diagnostic.code === DiagnosticCodes.ConditionDepthExceeded)) {
                    this.#authorize = null;
                    this.#authorizationDepthExceeded = true;
                }
                if (!this.#authorizationDepthExceeded) {
                    const previous = this.#authorize;
                    this.#authorize = combineAuthorize(this.#authorize, next, context);
                    this.#authorizationDepthExceeded = previous !== null && this.#authorize === null;
                }
                if (this.#authorizationDepthExceeded) recordAuthorizationRefusal(this.#fileImports);
                return true;
            }
            case 'provide':
                parseProvide(context, line, this.#provides);
                return true;
            case 'import':
                parseFileImport(context, line, this.#fileImports);
                return true;
            case 'example':
                this.#examples.push(parseExample(context, line));
                return true;
            case 'toolbar':
                reportFeatureToolbar(context, line, 'module');
                return true;
            case 'outlet':
                addOutletOverride(context, line, 'module', this.#outlets);
                return true;
            case 'feature':
                this.#features.push(parseFeature(context, line));
                return true;
            case 'navigation':
                if (isNavigationRegion(line.content)) reportNavigationRegion(context, line);
                else collectInputUses(context, line);
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
            documentation: this.#documentation,
            authorize: this.#authorize,
            dependsOn: this.#dependsOn,
            purposes: this.#purposes,
            ...(this.#provides.length > 0 ? { provides: this.#provides } : {}),
            ...(this.#outlets.length > 0 ? { outlets: this.#outlets } : {}),
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
