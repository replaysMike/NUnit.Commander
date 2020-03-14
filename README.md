# NUnit.Commander
[![Build status](https://ci.appveyor.com/api/projects/status/6vxd3cq83kuo4hg1?svg=true)](https://ci.appveyor.com/project/MichaelBrown/NUnit.Commander)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/a549220ae45b459c96ed1a5572dab40c)](https://www.codacy.com/manual/replaysMike/NUnit.Commander?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=replaysMike/NUnit.Commander&amp;utm_campaign=Badge_Grade)
[![Codacy Badge](https://api.codacy.com/project/badge/Coverage/a549220ae45b459c96ed1a5572dab40c)](https://www.codacy.com/manual/replaysMike/NUnit.Commander?utm_source=github.com&utm_medium=referral&utm_content=replaysMike/NUnit.Commander&utm_campaign=Badge_Coverage)

A wrapper for running NUnit tests using NUnit-Console or dotnet test with real-time updates and analysis

## Description
NUnit.Commander provides real-time test status output for tests run via the [NUnit-Console](https://github.com/nunit/nunit-console) or `dotnet test`. It works in conjunction with [NUnit.Extension.TestMonitor](https://github.com/replaysMike/NUnit.Extension.TestMonitor) (required for Commander to function) which provides insight into your tests as they run. It is a crucial tool for projects with complicated test architecture.

## Features

* Real-Time test output to stdout / piped log
* Full final report
* Multiple test run support for repeated running
* Test Analysis - report on changes in test duration, test stability
* Centralized log generation of each test in real-time

# Installation

Download and install the [latest release](https://github.com/replaysMike/NUnit.Commander/releases).

## Requirements

[NUnit.Extension.TestMonitor](https://github.com/replaysMike/NUnit.Extension.TestMonitor) must be installed along with your [NUnit-Console](https://github.com/nunit/nunit-console) test runner, or as a nuget package on your test project when using the `dotnet test` test runner. Please refer to installation instructions for [NUnit.Extension.TestMonitor](https://github.com/replaysMike/NUnit.Extension.TestMonitor)

## Screenshots

See the [wiki](https://github.com/replaysMike/NUnit.Commander/wiki) for further examples.

![NUnit.Commander](https://github.com/replaysMike/NUnit.Commander/wiki/screenshots/NUnit.Commander.png)
Real-time output of test status

![NUnit.Commander](https://github.com/replaysMike/NUnit.Commander/wiki/screenshots/NUnit.Commander-summary.png)
Summary report

# Usage

## How it works
The [NUnit.Extension.TestMonitor](https://github.com/replaysMike/NUnit.Extension.TestMonitor) extension is an NUnit engine extension which sends test events over IPC/Named pipes. Commander connects to the IPC/Named pipe server the extension creates and receives test events in real-time. Therefore, timeouts are required to give Commander a chance to connect to the extension when NUnit engine executes the tests and ensure we do not miss any events.

## Examples

### Run using NUnit-Console

Here you can tell Commander where the NUnitConsole installation folder is. This is optional, otherwise it will use the default path  `C:\Program Files (x86)\NUnit.org\nunit-console`. You can pass the usual NUnit-Console test runner arguments using the `--args` option and escape any required quotes with `\"`
```
> NUnit.Commander.exe --timeout=15 --test-runner NUnitConsole --path=C:\Path-to-NUnit-console-installation-folder --args="--workers=16 --where \"cat == UnitTests\" C:/Path-to/MyTestProject1.dll C:/Path-to/MyTestProject2.dll" 
```
### Run using dotnet test

Dotnet test is a little more flexible as you only need to give it the path to your solution or test project(s).
```
> NUnit.Commander.exe --timeout=15 --test-runner DotNetTest --args="C:/Path-to/MyTestProject"
```



