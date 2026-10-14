// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_DerivedPattern;

public class when_deriving_builtin_patterns
{
    [Fact] void should_derive_the_implicit_uuid() => DerivedPattern.Create(null).ShouldEqual("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$");
    [Fact] void should_derive_uuidv7() => DerivedPattern.Create(new("uuidv7")).ShouldEqual("^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$");
    [Fact] void should_derive_the_documented_customer_id() => DerivedPattern.Create(new("ulid") { Prefix = "cus_", Case = "lower" }).ShouldEqual("^cus_[0-7][0-9a-hjkmnp-tv-z]{25}$");
    [Fact] void should_derive_typeid_version_and_variant() => DerivedPattern.Create(new("typeid") { Prefix = "user" }).ShouldEqual("^user_[0-7][0-9a-hjkmnp-tv-z]{9}[ef][0-9a-hjkmnp-tv-z]{2}[89abrstv][0-9a-hjkmnp-tv-z]{12}$");
    [Fact] void should_omit_the_separator_of_an_empty_typeid_prefix() => DerivedPattern.Create(new("typeid") { Prefix = "" }).ShouldEqual("^[0-7][0-9a-hjkmnp-tv-z]{9}[ef][0-9a-hjkmnp-tv-z]{2}[89abrstv][0-9a-hjkmnp-tv-z]{12}$");
    [Fact] void should_derive_the_default_nanoid() => DerivedPattern.Create(new("nanoid")).ShouldEqual("^[A-Za-z0-9_-]{21}$");
    [Fact] void should_derive_the_documented_binary_code() => DerivedPattern.Create(new("nanoid") { Size = 64, Alphabet = "ab", Prefix = "code_" }).ShouldEqual("^code_[ab]{64}$");
    [Fact] void should_derive_documented_base64url_trailing_bits() => DerivedPattern.Create(new("bytes") { Count = 32, Encoding = "base64url" }).ShouldEqual("^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$");
    [Fact] void should_derive_documented_uppercase_hex() => DerivedPattern.Create(new("bytes") { Count = 16, Encoding = "hex", Prefix = "ref_", Case = "upper" }).ShouldEqual("^ref_[0-9A-F]{32}$");
    [Fact] void should_derive_documented_base32_trailing_bits() => DerivedPattern.Create(new("bytes") { Count = 16, Encoding = "base32" }).ShouldEqual("^[A-Z2-7]{25}[AEIMQUY4]$");
    [Fact] void should_escape_a_literal_prefix_and_alphabet() => DerivedPattern.Create(new("nanoid") { Size = 2, Alphabet = "^-]\\", Prefix = "a.b$" }).ShouldEqual(@"^a\.b\$[\^\-\]\\]{2}$");
}
