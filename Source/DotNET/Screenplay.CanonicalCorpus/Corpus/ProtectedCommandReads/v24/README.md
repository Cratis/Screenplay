# Protected command reads — working ESM v24

The corpus has single-file, folder, reordered, and relocated forms. They share
one application identity and produce identical canonical ESM bytes and revisions.

Twelve specifications cover required-read absence in empty and populated logs,
removal and recreation, precedence over requirements and generation, read paths
in requirements and produced mappings, cross-source reads, and reaction-invoked
protected commands. Successful presence cases prove that the guarded requirement
and generation phases are reached when the read exists.

Derive/provide have no executable nodes in this ESM, so this corpus does not claim coverage for them.

The legacy control removes only `command reads protected`. It pins all binding
diagnostic codes and messages in emission order, including PLAY0271 and the
single PLAY0786 repair hint. It has no executable model or semantic revision.
Source coordinates are not pinned because they vary between physical forms.

`identity/catalog-v1.json` pins the shared empty catalog for the fixed application
identity; semantic declaration identities are deterministic from their addresses.
`expected/esm-v24.json` and `expected/semantic-revision.txt` pin the executable
contract; `expected/legacy-diagnostics.json` pins the refusal control.

Regenerate only this corpus with
`SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS=1` while running
`for_ProtectedCommandReadsCorpus.when_loading_v24_source.should_select_v24_only_with_the_mode`.
Regeneration first runs every specification, writes the new resources, and then
intentionally fails. Review the bytes, rebuild, and rerun without the variable.
Race cases remain in the evaluator's decide/commit specifications; the source
language gains no race construct.
