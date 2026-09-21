# CPU instruction regressions

**Update 2026-09-21:** The complete opcode audit and additional fixes are documented
in [CPU-AUDIT.md](CPU-AUDIT.md). Run all three suites:

```powershell
MSBuild Tests/CpuInstructionTests.csproj /t:Rebuild
& ./Tests/bin/CpuInstructionTests.exe
& ./Tests/bin/CpuInstructionTests.exe --audit
& ./Tests/bin/CpuInstructionTests.exe --flags
MSBuild Src/8085.sln /t:Build /p:Configuration=Release
```

Targeted runs: `--audit DA E3`, `--flags alu`, `--flags ei dsub`.
All commands return nonzero on failure. EI tests require the new
`CanAcceptMaskableInterrupt` API. DSUB AC/P remain explicitly unverified.
The SDK interrupt acceptance checks now use that API; no PKW code is included.

## Initial test package (historical, 2026-09-20)

This branch is based on upstream `c46000a` (2025-10-16). Upstream already fixes CZ
and CPO. This change adds the remaining instruction corrections without PKW hardware,
UI, serial, realtime, memory-mapping or interrupt-delivery changes:

- CNZ: push the three-byte instruction's return address and jump; preserve A.
- DAA: preserve the upper digit, apply decimal corrections, update arithmetic flags.
- Untaken conditional CALL/RET: 9/6 T-states (taken remains 18/12).
- CMA: account for its four T-states.
- SIM: reset the RST7.5 pending latch when bit 4 is set, independently of mask enable.

The console harness compiles the actual CPU and expression evaluator sources into
its own executable; no changes to application visibility or assembly metadata are needed.
Build from a Visual Studio Developer PowerShell with .NET Framework 4.7.2 installed:

```powershell
MSBuild Tests/CpuInstructionTests.csproj /t:Build
& ./Tests/bin/CpuInstructionTests.exe
```

Coverage: all eight conditional CALLs and RETs across 16 combinations of Z/C/P/S
(target, stack, accumulator and timing), all 20,000 valid packed-BCD additions with
and without carry (result and S/Z/P/C), CMA and SIM latch behavior.

Validation on 2026-09-20: zero failures with this branch; 20,038 failed assertions
when compiled against unmodified upstream Assembler85.cs using the optional
`/p:CpuSource=<absolute source path>` property. The Release application also builds.
These checks do not claim exhaustive coverage of every 8085 instruction or arbitrary
non-BCD DAA inputs. Generated executables are not included in the commit.
