# Protected command reads — runnable V25R2 replacement

`ProtectedReadReplacements.V25R2` is the keyless replacement under decision 0170.
The published `v25/` original remains byte-for-byte frozen. Its thirteen keyed-message
mismatches are recorded in `ProtectedReadReplacements.SpecificationDispositions`,
following decision 0168; this replacement must pass normally without dispositions.

The corpus has single-file, folder, reordered, and relocated forms. They share
one application identity and produce identical canonical ESM bytes and revisions.

Nineteen specifications cover required-read absence in empty and populated logs,
removal and recreation, precedence over requirements and generation, read paths
in requirements and produced mappings, cross-source reads, and reaction-invoked
protected commands, including missing read state that leaves the accepted triggering fact intact. Successful presence cases prove that the guarded requirement
and generation phases are reached when the read exists.

Derive/provide have no executable nodes in this ESM, so this corpus does not claim coverage for them.

The legacy control removes only `command reads protected`. It pins all binding
diagnostic codes and messages in emission order, including PLAY0271 and the
single PLAY0786 repair hint. It has no executable model or semantic revision.
Source coordinates are not pinned because they vary between physical forms.

`identity/catalog-v1.json` pins the shared empty catalog for the fixed application
identity; semantic declaration identities are deterministic from their addresses.
`expected/esm-v25.json` and `expected/semantic-revision.txt` pin the executable
contract; `expected/legacy-diagnostics.json` pins the refusal control.

Missing instances and refused protected-read keys both reject with the keyless
`<View> does not exist.` validation message (decision 0170). Runtime keys are not
included; refused-key shapes, lookup ordering and world preservation are unchanged.

Regenerate only this corpus with
`SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS_V25R2=1` while running
`for_ProtectedCommandReadsCorpus.when_loading_v25_replacement.should_compile_pin_and_run_every_replacement_source_form_without_dispositions`.
Regeneration first runs every specification, writes only this replacement's ESM and
revision resources, and intentionally fails. Review the bytes, revision and
`ProtectedReadReplacements.V25R2Sha256`, rebuild, and rerun without the variable.
It never writes originals. Race cases use the separate `races-v1r3` replacement, run through the public decide,
extend and commit calls; the source language gains no race construct.
