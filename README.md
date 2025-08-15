# BigO.Types

Useful, allocation-conscious custom types for .NET: an inclusive `DateRange` value type, a validated/normalized `EmailAddress`, plus small utilities and adapters.

<p align="center">
  <img src="src/BigO.Types/Resources/bigo.png" alt="BigO logo" width="140"/>
</p>
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Target: net9.0](https://img.shields.io/badge/target-net9.0-black)

---

## Why

- **Make domain code explicit.** Replace ad‑hoc tuples/strings with purpose‑built types.
- **Safer defaults.** Inclusive date math, proper email validation/normalization.
- **Ergonomics.** Friendly `DateOnly` APIs, extension methods for common calendar ops, and JSON support.

------

## Types at a glance

### `DateRange` (value type)

An **inclusive** range of dates based on `DateOnly`.

**Core members (from tests):**

- `StartDate : DateOnly`
- `EndDate : DateOnly?`
- `EffectiveEnd : DateOnly` (max when open‑ended)
- `IsOpenEnded : bool`
- Deconstruction: `(start, end)`
- Extensions: `Contains(date)`, `Duration()`, `EnumerateDays()`, `GetWeeksInRange(...)`, `Overlaps(...)`, `Intersection(...)`
- Formatting/Parsing: `ToString()` uses `YYYY-MM-DD|YYYY-MM-DD` and `∞` for open‑ended; `Parse(...)` supports round‑trip string format.
- JSON: `DateRangeConverter` for `System.Text.Json`. ([GitHub](https://github.com/omarbesiso/BigO.Types/commit/587ee24fe1702a50f8a780361b9fad04226e497b))

**Semantics (from tests):**

- `default(DateRange)` behaves as **MinDate → ∞**.
- `EndDate` may equal `StartDate` (single‑day range).
- Constructing with `end < start` throws. ([GitHub](https://github.com/omarbesiso/BigO.Types/commit/587ee24fe1702a50f8a780361b9fad04226e497b))

**Usage**

```csharp
using BigO.Types;

// Open-ended range from 2025-08-01 to infinity.
var open = new DateRange(new DateOnly(2025, 8, 1));

// Closed range for August 2025.
var august = new DateRange(new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 31));

// Inclusive checks
bool hasMidMonth = august.Contains(new DateOnly(2025, 8, 15));   // true
int days = august.Duration();                                     // inclusive day count

// Enumerate each day (inclusive)
foreach (var d in august.EnumerateDays())
{
    // ...
}

// Overlap/intersection helpers (via extensions)
bool overlaps = august.Overlaps(open);
var intersection = august.Intersection(open); // returns the overlapping portion

// Round-trip formatting
var s = august.ToString();                     // "2025-08-01|2025-08-31"
var parsed = DateRange.Parse(s);               // == august

// Open-ended formatting uses ∞
var s2 = open.ToString();  // e.g. "2025-08-01|∞"
```

**System.Text.Json**

```csharp
using System.Text.Json;
using BigO.Types;

var options = new JsonSerializerOptions();
options.Converters.Add(new DateRangeConverter());

var json = JsonSerializer.Serialize(august, options);
var back = JsonSerializer.Deserialize<DateRange>(json, options);
```

------

### `EmailAddress`

A lightweight type for **validated** and **normalized** email addresses.

**From tests:**

- Construction & normalization; `TryParse` support; equality & ordering; default instance behavior.
- Interop with `System.Net.Mail.MailAddress` (adapter tests). ([GitHub](https://github.com/omarbesiso/BigO.Types/commit/587ee24fe1702a50f8a780361b9fad04226e497b))

**Typical usage**

```csharp
using BigO.Types;

if (EmailAddress.TryParse("  Jane.Doe@Example.COM  ", out var email))
{
    // Normalized representation (e.g., trimmed, domain normalization).
    Console.WriteLine(email.ToString());
}

// Using the .NET BCL type when needed:
var mail = new System.Net.Mail.MailAddress(email.ToString());
// ... and you can go the other way if an adapter API is provided in the library.
```

> Normalization is designed to provide consistent equality/ordering; specifics are in the implementation/tests. Avoid relying on provider‑specific rules (e.g., Gmail dot‑rules) unless explicitly documented. ([GitHub](https://github.com/omarbesiso/BigO.Types/commit/587ee24fe1702a50f8a780361b9fad04226e497b))

------

### Utilities

- `DisposableObject` — base class to simplify safe resource cleanup.
- `ObservableObject` — base for property change notifications.
   Both are introduced in the initial commit. Check XML docs in code for the exact patterns. ([GitHub](https://github.com/omarbesiso/BigO.Types/commit/587ee24fe1702a50f8a780361b9fad04226e497b))

------

## Build, test, pack

```bash
# build
dotnet build src/BigO.Types.sln -c Release

# run tests + coverage (requires coverlet collector present in tests project)
dotnet test src/BigO.Types.sln -c Release --collect:"XPlat Code Coverage"

# produce a nupkg (local)
dotnet pack src/BigO.Types/BigO.Types.csproj -c Release -o ./artifacts
```

> Packaging metadata is already included in the project (per initial commit notes). When you’re ready, publish to NuGet or your private feed. ([GitHub](https://github.com/omarbesiso/BigO.Types/commit/587ee24fe1702a50f8a780361b9fad04226e497b))

------

## Design notes

- **Inclusive ranges** reduce off‑by‑one errors for scheduling and reporting.
- **`DateOnly`** surface: no time‑zone surprises.
- **Extensions over inheritance** keeps the value type small and JIT/AOT‑friendly.
- JSON converter avoids custom binders at app edges.

------

## License

This project is under the **MIT License**. See LICENSE. ([GitHub](https://github.com/omarbesiso/BigO.Types))