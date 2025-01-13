# Plot
A graphing calculator as-a-service

## Overview
Plot is designed to be a quick and easy application that can be used as a replacement for the system calculator,
supporting a range of uses from basic calculations to advanced functions and graph plotting.

### Usage
Download a prebuilt binary from the Releases tab for a quick start. Alternatively, clone the repo, open `Plot.sln` and run `Plot.csproj`.

For plugin support, the project needs to be built from source with `-p:PublishSingleFile=false` flag when using `dotnet publish`.
This is due to the way .NET handles assembly loading when compiling as a single file, and on macOS sandboxing cannot be used (as the user must navigate to a file to allow the app to access it).

### Syntax
The syntax is similar to Python:
- No delimiter on the end of a line
- Create arrays with standard notation `array = [1,2,3,4,5]`
- Define functions with mathematical notation, with `x`, `y`, `z`... used as parameters `doubleIt = f(2*x)`
- Plot defined functions with `plot()`
- Create polynomial functions from an array `quad = polyFn([-2,3,1])`
