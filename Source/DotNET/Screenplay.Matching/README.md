# Cratis.Screenplay.Matching

BCL-only runtime matching for Screenplay's portable linear dialect on .NET 8, 9 and 10.
It uses the same parser and linear matcher as the compiler, not a platform regex engine.

```csharp
using Cratis.Screenplay.Matching;

var matcher = PortableMatcher.Compile("^[A-Z]{3}$");
var valid = matcher.IsMatch("ABC");
```

Compile once per concept and reuse the immutable, thread-safe matcher for every value.
Unanchored patterns search for a substring; `^` and `$` mean the absolute start and end.
Matching operates on UTF-16 code units, including lone surrogates. Admission limits are
1,000 UTF-16 pattern units and 3,000 expanded nodes. Refused or invalid patterns throw
`InvalidPortableMatchPattern` with the compiler's admission reason.

Use the same package version as the Screenplay compiler that admitted the pattern.
The package has no dependency on the compiler or other Cratis runtime packages.
See the [portable match dialect](https://www.cratis.io/screenplay/commands/#portable-match-dialect).
