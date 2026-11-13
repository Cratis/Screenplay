// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ExtensionContract } from '../Syntax/ExtensionContract';
import { ApplicationSyntax } from '../Syntax/Structure';
import { SystemSyntax } from '../Syntax/SystemSyntax';
import { SourceLocation } from '../Diagnostics/SourceLocation';

/** What a model's systems resolved to: the contracts by the reference the model writes. */
export interface ExtensionResolution {
    readonly contracts: ReadonlyMap<string, ExtensionContract>;
    readonly diagnostics: readonly Diagnostic[];
}

// Gives syntax read from a contract the location of the line that names the extension, so the model binds and
// reports against its own source document.
function relocate<T>(value: T, to: SourceLocation): T {
    if (Array.isArray(value)) return value.map(item => relocate(item, to)) as unknown as T;
    if (value === null || typeof value !== 'object') return value;
    const record = value as Record<string, unknown>;
    if (typeof record.line === 'number' && typeof record.column === 'number') return to as unknown as T;
    const copy: Record<string, unknown> = {};
    for (const [key, member] of Object.entries(record)) copy[key] = relocate(member, to);
    return copy as T;
}

/**
 * Returns the application as if the author had declared what each contract offers: the abilities on the system.
 * The printed source is not affected.
 */
export function applyExtensionContracts(application: ApplicationSyntax, contracts: ReadonlyMap<string, ExtensionContract>): ApplicationSyntax {
    if (contracts.size === 0 || application.systems === undefined) return application;
    const systems: SystemSyntax[] = application.systems.map(system => {
        const contract = system.extension === undefined ? undefined : contracts.get(system.extension.reference);
        return contract === undefined || system.extension === undefined ? system : { ...system, abilities: relocate(contract.abilities, system.extension.location) };
    });
    return { ...application, systems };
}

/** Checks each system's settings against the contract of the extension it names. */
export function checkExtensionSettings(application: ApplicationSyntax, contracts: ReadonlyMap<string, ExtensionContract>): readonly Diagnostic[] {
    const diagnostics: Diagnostic[] = [];
    for (const system of application.systems ?? []) {
        const contract = system.extension === undefined ? undefined : contracts.get(system.extension.reference);
        if (contract === undefined || system.extension === undefined) continue;
        const declared = new Map(contract.settings.map(setting => [setting.name, setting]));
        for (const setting of system.settings ?? []) {
            const found = declared.get(setting.name);
            if (found === undefined) diagnostics.push({ severity: 'error', code: DiagnosticCodes.UnknownSystemSetting, message: `System '${system.name}' maps setting '${setting.name}', which extension '${system.extension.reference}' does not declare.`, location: setting.location });
            else if (setting.secret === null && found.exposure === 'secret') diagnostics.push({ severity: 'error', code: DiagnosticCodes.SecretSettingLiteral, message: `System '${system.name}' gives setting '${setting.name}' a literal value, but extension '${system.extension.reference}' declares it secret; map it from a secret.`, location: setting.location });
            else if (setting.secret !== null && found.exposure === 'public') diagnostics.push({ severity: 'error', code: DiagnosticCodes.SecretOnPublicSetting, message: `System '${system.name}' maps secret '${setting.secret}' to setting '${setting.name}', which extension '${system.extension.reference}' declares public and may reach the browser.`, location: setting.location });
        }
    }
    return diagnostics;
}
