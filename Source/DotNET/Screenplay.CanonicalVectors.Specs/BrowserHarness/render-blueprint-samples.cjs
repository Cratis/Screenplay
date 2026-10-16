// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Writes the approved package set of one domain blueprint exactly as its published package declares it - the manifest keeps its
// `blueprint` and `sampleData` members, which the approved set accepts and drops - for `cratis render --package-set`, and lists the
// templates the package ships so the caller can hold every `template blueprint ...` reference of a sample to them.
//
// usage: node render-blueprint-samples.cjs <node_modules directory> <package suffix> <export name> <output file>
const { mkdirSync, writeFileSync } = require('node:fs');
const { dirname, join } = require('node:path');

const [modules, suffix, exportName, output] = process.argv.slice(2);
if (!output) throw new Error('usage: node render-blueprint-samples.cjs <node_modules> <package suffix> <export name> <output file>');

const name = `@cratis/scene.blueprint.${suffix}`;
const published = require(join(modules, name));
const manifest = published[`${exportName}Manifest`];
if (!manifest) throw new Error(`${name} does not export ${exportName}Manifest`);

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${JSON.stringify({
    version: 1,
    packages: [{ manifest, module: { npm: name, version: require(join(modules, name, 'package.json')).version, export: exportName, styles: [] } }],
}, null, 2)}\n`);
console.log(`${manifest.name} ${manifest.version} <- ${name}: ${manifest.screenTemplates.length} screen templates, ${manifest.dialogTemplates.length} dialog templates`);
