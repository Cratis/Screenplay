# Action-subject consumer fixture protocol

`native.manifest.json` and `typescript.manifest.json` enumerate the same per-screen cases. Each envelope references all five parts: authored source, raw canonical application, effective canonical application, relevant diagnostics (code, severity, message and source coordinates), and a context sidecar. `focusPath` selects the case without cropping its declarations out of the application.

The sidecar contains the complete raw declaration tree, canonical-path locations for authored nodes, and subject relations (`path`, owning `screen`/`slice`, and nearest `data` paths). Input flags (`isIdentifier`, `isGenerated`, `isKey`), nominal concepts, optionality/cardinality, command properties, forms and qualified references remain in the application. Location paths refer to that tree, not synthetic runtime action IDs. Missing declarations stay missing; consumers must not invent identity from primitive types or names. Generated effective inputs carry the action/alternative's source location; original locations are retained.

Both compiler profiles preserve their existing modeled syntax surfaces: native can include older native-only empty/default members. The paired specs compare every TS-modeled raw/effective member and all subject relations, and compare diagnostic code/severity/message/location. They also assert source immutability, idempotence, authored locations and explicit semantic expectations. These are compiler contracts, not renderer/runtime execution proof.

Cases cover explicit scope and bindings, same-name compatibility and precedence, mismatched non-key names, casing, one identifier/key, unmarked foreign keys, optional/generated/primitive identifiers, multiple keys, missing keys, qualified declarations, unresolved references, forms, slots/outlets and nearest sibling scopes, missing/equally near subjects, guarded record/collection behavior, toolbar parameters, malformed/duplicate/inapplicable clauses and malformed/duplicate inputs. `RecoveredSecondIdentifier` records existing parser recovery (the second identifier flag is cleared); direct immutable-snapshot specs separately prove the resolver does not infer Item from two identifier inputs.

## Regeneration

The owners are:

- Native `Syntax.Serialization.for_SyntaxJson.when_exporting_action_subject_fixture_packs.should_match_the_native_fixture_pack`.
- TS `Conformance/for_Conformance/when_exporting_action_subject_fixture_packs.ts`.

Set `SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES=1` only for those specs. Regeneration intentionally fails its sentinel assertion; review outputs and rerun with the variable unset. Never hand-edit canonical outputs or sidecars. Native specs run in **Debug**; build separately, then test with `--no-build --no-restore`. Generated golden/collection/schema contracts have their own separate owner generators.

Scene consumes a case's full raw application through native `ScreenActionSubjects.Resolve` or TS `resolveActionSubjects`. It must compare the effective result, not treat raw syntax as resolved. Absent-item availability, stable action identity and click-time item-source proofs remain Scene-owned. No runtime fields or executable semantic-model serialization were added here.
