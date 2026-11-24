# Protected-read races — runnable V1R3 replacement

`ProtectedReadReplacements.RacesV1R3` contains all seventeen V1R2 cases with only
`absent-then-created`'s first commit refusal message changed to `View does not exist.`. Fixture
contract version 1, vector order and every other expectation remain unchanged.
`RaceModelV1R3` shares V1's frozen source, identity catalog, ESM v25 bytes and revision.

Decision 0170 follows 0168: the original `races-v1` and `races-v1r2` files and public
pins remain frozen. `ProtectedReadReplacements.RaceDispositions` records each
original's exact incompatible commit and points to this runnable replacement.
Originals must produce precisely that mismatch and no other; this replacement
must pass all public decide/commit outcomes normally, without a disposition.

The exact-byte SHA-256, including the trailing newline, is
`fd1c9972411f9b5fe2745e1108ede1911f5071c4e836a9b5481dc9b59943d0fb`.
Verify before parsing; do not reserialize first.

To regenerate only this replacement, set
`SCREENPLAY_REGENERATE_PROTECTED_READ_RACES_V1R3=1` and run
`for_ProtectedCommandReadRaceVectors.when_loading_the_v1r3_replacement` in
`Screenplay.CanonicalVectors.Specs`. It runs the existing V1R2 authoring table
through public decide/commit calls, writes only `races.v1r3.json`, and deliberately
fails for review. Review the result and update `ProtectedReadReplacements.RacesV1R3Sha256`,
rebuild and rerun without the variable. The old V1/V1R2 regeneration paths refuse
to write frozen originals.
