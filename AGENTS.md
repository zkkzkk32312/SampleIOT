# Agents Guide

## Known Issues & Workarounds

### Test Execution Issue

**Problem:** Running `dotnet test` from the API project directory fails with:
```
An assembly specified in the application dependencies manifest (testhost.deps.json) was not found:  
package: 'Newtonsoft.Json', version: '13.0.3'  
path: 'lib/net6.0/Newtonsoft.Json.dll'
```

**Root Cause:** Moq 4.20.72 depends on Castle.Core which targets .NET 6.0, but the test project targets .NET 9.0. When running from the API directory, .NET creates a `testhost.deps.json` file that references packages at incorrect paths.

**Workaround:** Always run tests from the test project directory:
```bash
dotnet test "D:\Visual Studio Projects\SampleIOT\SampleIOT.API.Tests\SampleIOT.API.Tests.csproj"
```

Or simply `cd` into the test project folder before running tests.
