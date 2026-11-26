// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogTemplateSyntax, ScreenTemplateSyntax } from '../Syntax/InteractionOwners';
import { ModuleSyntax } from '../Syntax/Structure';
import { parseDialogTemplate, parseScreenTemplate } from './InteractionOwners';
import { ParserContext } from './ParserContext';
import { isScreenTemplateHeader } from './ScreenParser';
import { SourceLine } from './SourceLine';

/** Owns the module template definitions and leaves ordinary screens and opaque members to ModuleBody. */
export class ModuleInteractionBody {
    readonly #screens: ScreenTemplateSyntax[] = [];
    readonly #dialogs: DialogTemplateSyntax[] = [];

    tryParse(context: ParserContext, line: SourceLine): boolean {
        if (isScreenTemplateHeader(line.content)) {
            this.#screens.push(parseScreenTemplate(context, line));
            return true;
        }
        if (/^dialog\s+template(?:\s|$)/.test(line.content)) {
            this.#dialogs.push(parseDialogTemplate(context, line));
            return true;
        }
        return false;
    }

    members(): Pick<ModuleSyntax, 'screenTemplates' | 'dialogTemplates'> {
        return {
            ...(this.#screens.length > 0 ? { screenTemplates: this.#screens } : {}),
            ...(this.#dialogs.length > 0 ? { dialogTemplates: this.#dialogs } : {}),
        };
    }
}
