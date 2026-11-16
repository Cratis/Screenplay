// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_the_same_shape_is_needed_twice_with_aliases : given.a_free_screens_model
{
    void Establish() => Compile("screen Twice\n  data Invoice[] as overdue\n  data Invoice[] as upcoming\n  table overdue\n    column amount\n  table upcoming\n    column amount\n\n" + Slice("uses screen Twice\n  data overdue via query ListInvoices\n  data upcoming via query ListInvoices"));

    [Fact] void should_accept_the_aliases() => With(DiagnosticCodes.DuplicateScreenNeed).ShouldBeEmpty();
    [Fact] void should_provide_both_needs() => With(DiagnosticCodes.UnprovidedScreenNeed).ShouldBeEmpty();
    [Fact] void should_find_the_aliased_fields() => With(DiagnosticCodes.ProvisionFieldMissing).ShouldBeEmpty();
}
