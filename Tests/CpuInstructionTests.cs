using System;
using _8085;

static class CpuInstructionTests
{
    static int failures;
    static void Check(bool ok, string name)
    {
        if (!ok) { failures++; Console.Error.WriteLine("FAIL " + name); }
    }
    static void Step(Assembler85 cpu)
    {
        ushort pc = cpu.registerPC;
        string error = cpu.RunInstruction(pc, ref pc);
        if (error != "") throw new Exception(error);
    }
    static byte Bcd(int value) { return (byte)((value / 10 << 4) | value % 10); }
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--audit") return OpcodeAudit.Run(args);
        if (args.Length > 0 && args[0] == "--flags") return FlagAudit.Run(args);
        for (int condition = 0; condition < 8; condition++)
        for (int flags = 0; flags < 16; flags++)
        {
            var cpu = new Assembler85(new string[0]);
            cpu.flagZ = (flags & 1) != 0; cpu.flagC = (flags & 2) != 0;
            cpu.flagP = (flags & 4) != 0; cpu.flagS = (flags & 8) != 0;
            bool taken = new[] { !cpu.flagZ, cpu.flagZ, !cpu.flagC, cpu.flagC,
                !cpu.flagP, cpu.flagP, !cpu.flagS, cpu.flagS }[condition];
            cpu.RAM[0] = (byte)(0xC4 + condition * 8); cpu.RAM[1] = 0x34; cpu.RAM[2] = 0x12;
            cpu.registerSP = 0x6040; cpu.registerA = 0x55; Step(cpu);
            Check(cpu.registerPC == (taken ? 0x1234 : 3) && cpu.registerSP == (taken ? 0x603E : 0x6040) &&
                cpu.registerA == 0x55 && cpu.cycles == (taken ? 18UL : 9UL) &&
                (!taken || (cpu.RAM[0x603E] == 3 && cpu.RAM[0x603F] == 0)), "CALL condition=" + condition + " flags=" + flags);
            cpu.registerPC = 0; cpu.cycles = 0; cpu.registerSP = 0x603E;
            cpu.RAM[0] = (byte)(0xC0 + condition * 8); cpu.RAM[0x603E] = 0x34; cpu.RAM[0x603F] = 0x12;
            Step(cpu);
            Check(cpu.registerPC == (taken ? 0x1234 : 1) && cpu.registerSP == (taken ? 0x6040 : 0x603E) &&
                cpu.cycles == (taken ? 12UL : 6UL), "RET condition=" + condition + " flags=" + flags);
        }
        var decimalCpu = new Assembler85(new string[0]);
        decimalCpu.RAM[0] = 0xCE; // ACI immediate, then DAA
        decimalCpu.RAM[2] = 0x27;
        for (int a = 0; a < 100; a++) for (int b = 0; b < 100; b++) for (int carry = 0; carry < 2; carry++)
        {
            decimalCpu.registerPC = 0; decimalCpu.registerA = Bcd(a); decimalCpu.RAM[1] = Bcd(b);
            decimalCpu.flagC = carry != 0; Step(decimalCpu); Step(decimalCpu);
            int sum = a + b + carry; byte expected = Bcd(sum % 100);
            int bits = 0; for (int bit = 0; bit < 8; bit++) bits += (expected >> bit) & 1;
            Check(decimalCpu.registerA == expected && decimalCpu.flagC == (sum >= 100) &&
                decimalCpu.flagZ == (expected == 0) && decimalCpu.flagS == ((expected & 128) != 0) &&
                decimalCpu.flagP == (bits % 2 == 0), "BCD " + a + "+" + b + "+" + carry);
        }
        var single = new Assembler85(new string[0]);
        single.RAM[0] = 0x2F; single.registerA = 0x55; single.flagC = true; single.flagZ = true; Step(single);
        Check(single.registerA == 0xAA && single.cycles == 4 && single.flagC && single.flagZ, "CMA result/timing/flags");
        single.RAM[1] = single.RAM[2] = 0x30;
        single.intrP75 = true; single.registerA = 0; Step(single);
        Check(single.intrP75, "SIM preserves latch without bit 4");
        single.registerA = 0x10; Step(single);
        Check(!single.intrP75, "SIM clears RST7.5 latch independently of mask enable");
        Console.WriteLine("256 conditional CALL/RET cases, 20000 BCD sums, CMA and SIM: " + failures + " failures");
        return failures == 0 ? 0 : 1;
    }
}
