// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_refuting_builtin_shapes
{
    [Fact] void should_use_the_uuid_version_and_variant() => Check(new("uuid"), "^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$").Witness.ShouldEqual("00000000-0000-4000-8000-000000000000");
    [Fact] void should_use_uuidv7_uppercase_positions_without_changing_the_prefix() => Check(new("uuidv7") { Case = "upper", Prefix = "ref_" }, "^ref_[0-9a-f-]+$").Witness.ShouldEqual("ref_00000000-0000-7000-8000-00000000000A");
    [Fact] void should_bound_the_ulid_first_symbol() => Check(new("ulid"), "^[0-6].*$").Witness.ShouldEqual("7" + new string('0', 25));
    [Fact] void should_preserve_the_typeid_variant_bits() => Check(new("typeid") { Prefix = "order" }, "^order_[0-7][0-9a-hjkmnp-tv-z]{9}[ef][0-9a-hjkmnp-tv-z]{2}[89abrst][0-9a-hjkmnp-tv-z]{12}$").Witness.ShouldEqual("order_" + new string('0', 10) + "e00v" + new string('0', 12));
    [Fact] void should_order_default_nanoid_characters_ordinally() => Check(new("nanoid") { Size = 2 }, "^[A-Za-z0-9_]+$").Witness.ShouldEqual("--");
    [Fact] void should_refute_with_only_valid_base64url_last_symbols() => Check(new("bytes") { Count = 1, Encoding = "base64url" }, "^.[AQg]$").Witness.ShouldEqual("-w");
    [Fact] void should_refute_with_only_valid_base32_last_symbols() => Check(new("bytes") { Count = 1, Encoding = "base32" }, "^.[AEIMQUY]$").Witness.ShouldEqual("24");
    [Fact] void should_apply_hex_case_to_the_encoded_value_only() => Check(new("bytes") { Count = 1, Encoding = "hex", Prefix = "ref_", Case = "upper" }, "^ref_[0-9a-f]+$").Witness.ShouldEqual("ref_0A");

    static RuleConsistencyResult Check(SemanticGeneration generation, string pattern) => RuleConsistency.Check(generation, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(pattern), null));
}
