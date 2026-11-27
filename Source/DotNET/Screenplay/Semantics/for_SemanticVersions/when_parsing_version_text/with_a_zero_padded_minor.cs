// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticVersions.when_parsing_version_text;

public class with_a_zero_padded_minor : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => SemanticVersion.Parse("1.00"));

    [Fact] void should_reject_the_noncanonical_version() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_report_the_rejected_value() => (_error?.Message).ShouldEqual(
        "'1.00' is not a canonical semantic version supported by ESM schema v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20 or v21.");
}
