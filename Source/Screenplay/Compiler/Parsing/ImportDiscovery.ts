// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isDocumentPlacement, PlayPlacement } from '../Files/PlayPlacement';
import { FeatureSyntax, FileImportSyntax } from '../Syntax/Structure';
import { parseFeature } from './FeatureBody';
import { tryParseFileImport } from './FileImportParser';
import { firstWord } from './LineText';
import { parseModule } from './ModuleBody';
import { ParserContext } from './ParserContext';

// A file import, together with the module and feature names around it in the document, outermost first, and
// whether the outermost name is a 'module' written at the document's top level rather than a 'feature'. The
// port of the C# DiscoveredFileImport.
export interface DiscoveredImport {
    readonly scope: readonly string[];
    readonly startsAtModule: boolean;
    readonly fileImport: FileImportSyntax;
}

// Where an import places what it imports, given where the document holding it is placed - undefined when the
// import cannot place anything from there. A top level 'feature' only means something once the document is
// placed in a module, so until then its imports place nothing - the document reports the misplaced feature
// itself if it never is. A top level 'module' is the module's own declaration in a whole document, a
// restatement that joins the placement in a document placed in that module, and nothing anywhere else.
export function placementFrom(discovered: DiscoveredImport, document: PlayPlacement): PlayPlacement | undefined {
    const { scope } = discovered;
    if (scope.length === 0) return document;
    if (!discovered.startsAtModule) return isDocumentPlacement(document) ? undefined : [...document, ...scope];
    if (isDocumentPlacement(document)) return scope;
    return document.length === 1 && document[0] === scope[0] ? [...document, ...scope.slice(1)] : undefined;
}

// Finds the files a document imports and where in it each import is written, without settling where the
// document itself belongs - the port of the C# ScreenplayParser.DiscoverImports. Where a file belongs depends
// on what imports it, and what it imports depends on where the imports are written in it, so this reads the
// module and feature structure of any file, whether its top level is the application's or the body of a
// module or feature it will later be placed in. The context's diagnostics are not the document's.
export function discoverImports(context: ParserContext): DiscoveredImport[] {
    const found: DiscoveredImport[] = [];
    for (let line = context.reader.peekSignificant(); line !== undefined; line = context.reader.peekSignificant()) {
        context.reader.takeSignificant();
        const keyword = firstWord(line.content);
        const fileImport = keyword === 'import' ? tryParseFileImport(line) : undefined;
        if (fileImport !== undefined) {
            found.push({ scope: [], startsAtModule: false, fileImport });
        } else if (keyword === 'module') {
            const module = parseModule(context, line);
            found.push(...module.fileImports.map(each => ({ scope: [module.name], startsAtModule: true, fileImport: each })));
            found.push(...module.features.flatMap(feature => importsIn(feature, [module.name], true)));
        } else if (keyword === 'feature') {
            found.push(...importsIn(parseFeature(context, line), [], false));
        } else {
            context.skipOpaqueBlock(line.indent);
        }
    }
    return found;
}

function importsIn(feature: FeatureSyntax, outer: readonly string[], startsAtModule: boolean): DiscoveredImport[] {
    const scope = [...outer, feature.name];
    return [
        ...feature.fileImports.map(fileImport => ({ scope, startsAtModule, fileImport })),
        ...feature.features.flatMap(nested => importsIn(nested, scope, startsAtModule)),
    ];
}
