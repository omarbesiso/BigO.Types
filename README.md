# BigO.Types

<p align="center">
  <img src="src/BigO.Types/Resources/bigo.png" alt="BigO logo" width="140"/>
</p>

Allocation-conscious types for .NET.

## Features

- **DateRange** – inclusive `DateOnly` range with helpers for contains, duration, enumeration and JSON serialization.
- **EmailAddress** – validated and normalized address representation.
- **Utility bases** – `DisposableObject` and `ObservableObject` for common patterns.

## Install

BigO.Types relies on [`BigO.Validation`](https://www.nuget.org/packages/BigO.Validation).

### .NET CLI

```bash
dotnet add package BigO.Validation
```

### Visual Studio

1. Right-click the project and choose **Manage NuGet Packages...**
2. Search for `BigO.Validation` and install the latest version.

Or use the Package Manager Console:

```powershell
Install-Package BigO.Validation
```

## Usage

```csharp
using BigO.Types;

// Date ranges
var range = new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 31));
bool containsMidMonth = range.Contains(new DateOnly(2025, 1, 15));

// Email addresses
if (EmailAddress.TryParse("user@example.com", out var email))
{
    Console.WriteLine(email); // normalized
}
```

## Build, test and pack

Run these commands from the repository root:

```bash
dotnet restore
dotnet build src/BigO.Types.sln -c Release
dotnet test src/BigO.Types.sln -c Release
dotnet pack src/BigO.Types/BigO.Types.csproj -c Release -o ./artifacts
```

## License

Licensed under the [MIT License](LICENSE).

