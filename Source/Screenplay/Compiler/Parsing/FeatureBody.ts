// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthorizeSyntax } from '../Syntax/Authorization';
import { AvailableSyntax } from '../Syntax/Availability';
import { addAvailabilityGate } from './AvailabilityParser';
import { SpecificationExampleSyntax } from '../Syntax/Specifications';
import { DependsOnSyntax, FeatureSyntax, FileImportSyntax, SliceSyntax } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { combineAuthorize, parseAuthorize, recordAuthorizationRefusal } from './AuthorizeParser';
import { parseDescription } from './DescriptionParser';
import { PurposeReferenceSyntax } from '../Syntax/Purposes';
import { parsePurposeReference } from './PurposeParser';
import { parseDocumentation } from './DocumentationParser';
import { parseDependsOn } from './DependsOnParser';
import { parseFileImport } from './FileImportParser';
import { parseProvide } from './ProvideParser';
import { ProvideSyntax } from '../Syntax/ProvideSyntax';
import { collectInputUses } from './InputUses';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { misplacedReactionIdentity } from './ReactionIdentityParser';
import { parseExample } from './SpecificationExampleParser';
import { reportFeatureToolbar } from './ScreenParser';
import { addOutletOverride } from './OutletParser';
import type { OutletSyntax } from '../Syntax/Outlets';
import { parseSlice } from './SliceParser';
import { locationOf, SourceLine } from './SourceLine';

const featurePattern = pattern('^feature\\s+([A-Za-z_]\\w*)$');

// Feature members the C# compiler knows that this compiler does not model. They are skipped whole, not reported.
const opaqueFeatureMembers = new Set(['on', 'uses', 'contribute', 'template']);

// What a feature body may hold, as it reads in a diagnostic.
export const featureBodyExpected = 'description, documentation, depends on <Name>, authorize, available, provide, import, feature, slice, contribute, outlet, example, \'on <trigger>\' or \'uses <Behavior>\'';

// Collects the body of a feature - written beneath a 'feature' header, or at the top level of a file imported
// into the feature. The port of the C# FeatureBody.
export class FeatureBody {
    readonly #features: FeatureSyntax[] = [];
    readonly #slices: SliceSyntax[] = [];
    readonly #examples: SpecificationExampleSyntax[] = [];
    readonly #fileImports: FileImportSyntax[] = [];
    readonly #dependsOn: DependsOnSyntax[] = [];
    readonly #purposes: PurposeReferenceSyntax[] = [];
    readonly #provides: ProvideSyntax[] = [];
    readonly #available: AvailableSyntax[] = [];
    readonly #outlets: OutletSyntax[] = [];
    #description: string | null = null;
    #documentation: string | null = null;
    #authorize: AuthorizeSyntax | null = null;
    #authorizationDepthExceeded = false;

    constructor(readonly name: string) {}

    // Parses one already consumed line of the body; false when the line does not belong to a feature body,
    // and nothing was reported.
    tryParse(context: ParserContext, line: SourceLine): boolean {
        const keyword = firstWord(line.content);
        switch (keyword) {
            case 'runs':
                misplacedReactionIdentity(context, line);
                return true;
            case 'description':
                this.#description = parseDescription(context, line, this.#description, `Feature '${this.name}'`);
                return true;
            case 'documentation':
                this.#documentation = parseDocumentation(context, line, this.#documentation, `Feature '${this.name}'`);
                return true;
            case 'depends':
                parseDependsOn(context, line, this.#dependsOn, DiagnosticCodes.UnknownFeatureDirective);
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
            case 'available':
                addAvailabilityGate(context, line, this.#available);
                return true;
            case 'provide':
                parseProvide(context, line, this.#provides);
                return true;
            case 'import':
                parseFileImport(context, line, this.#fileImports);
                return true;
            case 'toolbar':
                reportFeatureToolbar(context, line, 'feature');
                return true;
            case 'outlet':
                addOutletOverride(context, line, 'feature', this.#outlets);
                return true;
            case 'feature':
                this.#features.push(parseFeature(context, line));
                return true;
            case 'example':
                this.#examples.push(parseExample(context, line));
                return true;
            case 'slice':
                this.#slices.push(parseSlice(context, line));
                return true;
            default:
                if (opaqueFeatureMembers.has(keyword)) {
                    collectInputUses(context, line);
                    return true;
                }
                return false;
        }
    }

    // Builds the feature, located at its header or, when it only places an imported file, at that file.
    build(location: SourceLocation, isPlacement = false): FeatureSyntax {
        return {
            kind: 'FeatureSyntax',
            name: this.name,
            description: this.#description,
            documentation: this.#documentation,
            authorize: this.#authorize,
            dependsOn: this.#dependsOn,
            purposes: this.#purposes,
            ...(this.#provides.length > 0 ? { provides: this.#provides } : {}),
            ...(this.#available.length > 0 ? { available: this.#available } : {}),
            ...(this.#outlets.length > 0 ? { outlets: this.#outlets } : {}),
            features: this.#features,
            slices: this.#slices,
            examples: this.#examples,
            fileImports: this.#fileImports,
            isPlacement,
            location,
        };
    }
}

// Parses a feature from its already consumed header line.
export function parseFeature(context: ParserContext, line: SourceLine): FeatureSyntax {
    const name = featurePattern.exec(line.content)?.[1] ?? '';
    if (name === '') {
        context.error(DiagnosticCodes.InvalidFeatureDeclaration, `Invalid feature declaration '${line.content}' - expected 'feature <Name>'`, locationOf(line));
    }
    const body = new FeatureBody(name);
    const previous = context.scope;
    context.scope = [...previous, name];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (!body.tryParse(context, child)) {
            context.error(DiagnosticCodes.UnknownFeatureDirective, `Unexpected '${firstWord(child.content)}' in feature body - expected ${featureBodyExpected}`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    context.scope = previous;
    return body.build(locationOf(line));
}
