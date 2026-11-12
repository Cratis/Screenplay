// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SecretSyntax } from '../Syntax/Operations';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

/** Validates secrets and system settings against the application's declarations: what the model alone can decide. */
export function validateSystemConfiguration(application: ApplicationSyntax, context: ParserContext): void {
    const declared = new Map<string, SecretSyntax>();
    for (const secret of application.secrets ?? []) {
        const key = secret.name.toLowerCase();
        if (declared.has(key)) context.error(DiagnosticCodes.DuplicateSecret, `Secret '${secret.name}' is declared more than once; secret names compare ignoring case.`, secret.location);
        else declared.set(key, secret);
    }

    const used = new Set<string>();
    for (const system of application.systems ?? []) {
        const mapped = new Set<string>();
        if (system.extension !== undefined && system.abilities.length > 0) {
            context.error(DiagnosticCodes.SystemAbilitiesBesideExtension, `System '${system.name}' names extension '${system.extension.reference}' and declares abilities; the extension's contract is the only source of its abilities.`, system.abilities[0].location);
        }
        for (const setting of system.settings ?? []) {
            if (mapped.has(setting.name)) {
                context.error(DiagnosticCodes.DuplicateSystemSetting, `System '${system.name}' maps setting '${setting.name}' more than once.`, setting.location);
                continue;
            }
            mapped.add(setting.name);
            if (setting.secret === null) continue;
            const key = setting.secret.toLowerCase();
            if (declared.has(key)) used.add(key);
            else context.error(DiagnosticCodes.UnknownSecret, `System '${system.name}' setting '${setting.name}' maps from secret '${setting.secret}', which is not declared.`, setting.location);
        }
    }

    for (const [key, secret] of declared) {
        if (!used.has(key)) context.error(DiagnosticCodes.UnusedSecret, `Secret '${secret.name}' is declared but no system setting maps from it.`, secret.location);
    }
}
