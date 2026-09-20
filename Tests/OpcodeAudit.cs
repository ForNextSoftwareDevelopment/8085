using System;
using System.Collections.Generic;
using System.Linq;
using _8085;

// Independent, table/group-based architectural model, rather than a copy of the
// production if/else decoder. Intel 8085 instruction descriptions and timings.
static class OpcodeAudit
{
    static readonly Dictionary<int, int> errors = new Dictionary<int, int>();
    static readonly HashSet<int> covered = new HashSet<int>();
    static int checks;
    static bool Even(int n) { int bits=0; for(int b=0;b<8;b++) bits+=(n>>b)&1; return bits%2==0; }
    static int Szp(int n) { n&=255; return (n&128)|(n==0?64:0)|(Even(n)?4:0); }
    static int Flags(Assembler85 c) { return (c.flagS?128:0)|(c.flagZ?64:0)|(c.flagK?32:0)|(c.flagAC?16:0)|(c.flagP?4:0)|(c.flagV?2:0)|(c.flagC?1:0); }
    static void SetFlags(Assembler85 c,int f) { c.flagS=(f&128)!=0;c.flagZ=(f&64)!=0;c.flagK=(f&32)!=0;c.flagAC=(f&16)!=0;c.flagP=(f&4)!=0;c.flagV=(f&2)!=0;c.flagC=(f&1)!=0; }
    static int[] Registers(Assembler85 c) { return new[]{(int)c.registerB,c.registerC,c.registerD,c.registerE,c.registerH,c.registerL,0,c.registerA}; }
    sealed class Expected
    {
        public int[] r; public int pc,sp,f,t; public bool ie; public string error="";
        public Dictionary<int,byte> writes=new Dictionary<int,byte>(); public Dictionary<int,byte> ports=new Dictionary<int,byte>();
        public byte[] ram; public int Word(int a){return Byte(a)|(Byte(a+1)<<8);}
        public int Byte(int a){byte v;return writes.TryGetValue(a&65535,out v)?v:ram[a&65535];}
        public void Write(int a,int v){writes[a&65535]=(byte)v;}
        public int Pair(int n){return n==3?sp:(r[n*2]<<8)|r[n*2+1];}
        public void Pair(int n,int v){v&=65535;if(n==3)sp=v;else{r[n*2]=v>>8;r[n*2+1]=v&255;}}
        public int Reg(int n){return n==6?Byte(Pair(2)):r[n];}
        public void Reg(int n,int v){if(n==6)Write(Pair(2),v);else r[n]=v&255;}
        public bool Cond(int n){bool on=(f & new[]{64,1,4,128}[n/2])!=0;return n%2==1?on:!on;}
        public void Push(int v){sp=(sp-1)&65535;Write(sp,v>>8);sp=(sp-1)&65535;Write(sp,v);}
        public int Pop(){int v=Word(sp);sp=(sp+2)&65535;return v;}
        public void Alu(int op,int value)
        {
            int a=r[7],carry=(op==1||op==3)?f&1:0,q;
            if(op<=3||op==7) {
                bool sub=op==2||op==3||op==7;
                q=sub?a-value-carry:a+value+carry;
                f=Szp(q)|((q<0||q>255)?1:0)|((sub?(a&15)>=(value&15)+carry:(a&15)+(value&15)+carry>15)?16:0);
            } else {q=op==4?a&value:op==5?a^value:a|value;f=Szp(q)|(op==4?16:0);}
            if(op!=7)r[7]=q&255;
        }
        public void Execute(int op,Assembler85 c)
        {
            int at=pc, imm=Byte(at+1), word=Word(at+1),pair=(op>>4)&3;
            pc=(at+1)&65535;t=4;
            if(op>=0x40&&op<=0x7f) {if(op==0x76){t=5;error="System Halted";}else{Reg((op>>3)&7,Reg(op&7));t=((op&7)==6||((op>>3)&7)==6)?7:4;}return;}
            if(op>=0x80&&op<=0xbf){Alu((op>>3)&7,Reg(op&7));t=(op&7)==6?7:4;return;}
            if(op<0x40&&(op&7)==6){Reg((op>>3)&7,imm);pc=(at+2)&65535;t=((op>>3)&7)==6?10:7;return;}
            if(op<0x40&&((op&7)==4||(op&7)==5)){
                int n=(op>>3)&7,v=Reg(n);bool dec=(op&7)==5;int q=(v+(dec?-1:1))&255;
                f=(f&1)|Szp(q)|((dec?(v&15)!=0:(v&15)==15)?16:0);Reg(n,q);t=n==6?10:4;return;}
            if(op<0x40&&(op&15)==1){Pair(pair,word);pc=(at+3)&65535;t=10;return;}
            if(op<0x40&&((op&15)==3||(op&15)==11)){Pair(pair,Pair(pair)+((op&15)==3?1:-1));t=6;return;}
            if(op<0x40&&(op&15)==9){int q=Pair(2)+Pair(pair);Pair(2,q);f=(f&~1)|(q>65535?1:0);t=10;return;}
            if(op>=0xc0&&(op&7)==6){Alu((op>>3)&7,imm);pc=(at+2)&65535;t=7;return;}
            if(op>=0xc0&&(op&7)==0){if(Cond((op>>3)&7)){pc=Pop();t=12;}else t=6;return;}
            if(op>=0xc0&&(op&7)==2){bool take=Cond((op>>3)&7);pc=take?word:(at+3)&65535;t=take?10:7;return;}
            if(op>=0xc0&&(op&7)==4){bool take=Cond((op>>3)&7);pc=(at+3)&65535;t=take?18:9;if(take){Push(pc);pc=word;}return;}
            if(op>=0xc0&&(op&7)==7){Push(pc);pc=op&0x38;t=12;return;}
            if(op>=0xc0&&(op&15)==1){int v=Pop();if(pair==3){r[7]=v>>8;f=v&255;}else Pair(pair,v);t=10;return;}
            if(op>=0xc0&&(op&15)==5){Push(pair==3?(r[7]<<8)|(f&0xf7):Pair(pair));t=12;return;}
            switch(op){
                case 0x00:break;
                case 0x02:case 0x12:Write(Pair(pair),r[7]);t=7;break;
                case 0x0a:case 0x1a:r[7]=Byte(Pair(pair));t=7;break;
                case 0x07:case 0x17:{int old=r[7];r[7]=((old<<1)|((op==7)?old>>7:f&1))&255;f=(f&~1)|(old>>7);break;}
                case 0x0f:case 0x1f:{int old=r[7];r[7]=(old>>1)|((op==15?old:f)&1)<<7;f=(f&~1)|(old&1);break;}
                case 0x20:r[7]=(c.intrM55?1:0)|(c.intrM65?2:0)|(c.intrM75?4:0)|(ie?8:0)|(c.intrP55?16:0)|(c.intrP65?32:0)|(c.intrP75?64:0)|(c.sid?128:0);break;
                case 0x22:Write(word,r[5]);Write(word+1,r[4]);pc=(at+3)&65535;t=16;break;
                case 0x2a:Pair(2,Word(word));pc=(at+3)&65535;t=16;break;
                case 0x27:{int old=r[7],adjust=0;bool cy=(f&1)!=0||old>0x99;if((old&15)>9||(f&16)!=0)adjust+=6;if(cy)adjust+=0x60;int q=(old+adjust)&255;r[7]=q;f=Szp(q)|(cy?1:0)|(((old&15)+(adjust&15)>15)?16:0);break;}
                case 0x2f:r[7]^=255;break;
                case 0x30:break; // mask/SOD behavior checked separately
                case 0x32:Write(word,r[7]);pc=(at+3)&65535;t=13;break;
                case 0x3a:r[7]=Byte(word);pc=(at+3)&65535;t=13;break;
                case 0x37:f|=1;break;case 0x3f:f^=1;break;
                case 0xc3:pc=word;t=10;break;
                case 0xc9:pc=Pop();t=10;break;
                case 0xcd:Push((at+3)&65535);pc=word;t=18;break;
                case 0xd3:ports[imm]=(byte)r[7];pc=(at+2)&65535;t=10;break;
                case 0xdb:r[7]=c.PORT[imm];pc=(at+2)&65535;t=10;break;
                case 0xe3:{int old=Pair(2);Pair(2,Word(sp));Write(sp,old);Write(sp+1,old>>8);t=16;break;}
                case 0xe9:pc=Pair(2);t=6;break;
                case 0xeb:{int old=Pair(2);Pair(2,Pair(1));Pair(1,old);break;}
                case 0xf3:ie=false;break;case 0xfb:ie=true;break;
                case 0xf9:sp=Pair(2);t=6;break;
                // Undocumented opcodes: functional path now, extended flags below.
                case 0x08:{int q=Pair(2)-Pair(0);Pair(2,q);f=Szp(q>>8)|(q<0?1:0);f=(f&~64)|((q&65535)==0?64:0);t=10;break;}
                case 0x10:{int old=Pair(2);Pair(2,(old>>1)|(old&32768));f=(f&~1)|(old&1);t=7;break;}
                case 0x18:{int old=Pair(1);Pair(1,(old<<1)|(f&1));f=(f&~1)|(old>>15);t=10;break;}
                case 0x28:Pair(1,Pair(2)+imm);pc=(at+2)&65535;t=10;break;
                case 0x38:Pair(1,sp+imm);pc=(at+2)&65535;t=10;break;
                case 0xcb:if((f&2)!=0){Push(pc);pc=0x40;t=12;}else t=6;break;
                case 0xd9:Write(Pair(1),r[5]);Write(Pair(1)+1,r[4]);t=10;break;
                case 0xed:Pair(2,Word(Pair(1)));t=10;break;
                case 0xdd:case 0xfd:{bool take=((f&32)!=0)==(op==0xfd);pc=take?word:(at+3)&65535;t=take?10:7;break;}
                default:throw new Exception("Missing reference opcode "+op.ToString("X2"));
            }
        }
    }
    static void Case(int op,int seed)
    {
        var rng=new Random(seed*257+op);var c=new Assembler85(new string[0]);
        rng.NextBytes(c.RAM);rng.NextBytes(c.PORT);
        c.registerA=(byte)rng.Next(256);c.registerB=(byte)rng.Next(256);c.registerC=(byte)rng.Next(256);
        c.registerD=(byte)rng.Next(256);c.registerE=(byte)rng.Next(256);c.registerH=(byte)rng.Next(256);c.registerL=(byte)rng.Next(256);
        c.registerPC=(ushort)new[]{0x2000,0xfffe,0xffff,0}[seed%4];c.registerSP=(ushort)new[]{0x8000,0,0xffff,1}[seed%4];
        if(seed%4==0){c.registerD=c.registerE=c.registerH=c.registerL=255;}
        SetFlags(c,seed*17);c.intrIE=(seed&1)!=0;c.sid=(seed&2)!=0;
        c.intrM55=(seed&1)!=0;c.intrM65=(seed&2)!=0;c.intrM75=(seed&4)!=0;
        c.intrP55=(seed&8)!=0;c.intrP65=(seed&16)!=0;c.intrP75=(seed&32)!=0;
        c.RAM[c.registerPC]=(byte)op;
        if(seed%8==0){c.RAM[(ushort)(c.registerPC+1)]=255;c.RAM[(ushort)(c.registerPC+2)]=255;}
        var e=new Expected{r=Registers(c),pc=c.registerPC,sp=c.registerSP,f=Flags(c),ram=(byte[])c.RAM.Clone(),ie=c.intrIE};
        e.Execute(op,c);ushort next=c.registerPC;string error=c.RunInstruction(next,ref next);
        // DSUB's undocumented AC/P are evaluated by the dedicated silicon-profile tests.
        int flagMask=op==8?0xc1:0xd5;
        bool ok=error==e.error&&c.registerPC==e.pc&&next==e.pc&&c.registerSP==e.sp&&c.cycles==(ulong)e.t&&
            Registers(c).SequenceEqual(e.r)&&((Flags(c)^e.f)&flagMask)==0&&c.intrIE==e.ie;
        foreach(var w in e.writes)e.ram[w.Key]=w.Value;
        ok &= c.RAM.SequenceEqual(e.ram);
        for(int p=0;p<256;p++)if(e.ports.ContainsKey(p))ok&=c.PORT[p]==e.ports[p];
        covered.Add(op);checks++;
        if(!ok){if(!errors.ContainsKey(op)){errors[op]=0;Console.WriteLine("FAIL "+op.ToString("X2")+" seed="+seed+" PC="+c.registerPC.ToString("X4")+"/"+e.pc.ToString("X4")+" flags="+Flags(c).ToString("X2")+"/"+e.f.ToString("X2")+" cycles="+c.cycles+"/"+e.t+" "+error);}errors[op]++;}
    }
    public static int Run(string[] args)
    {
        foreach(int op in args.Length>1?args.Skip(1).Select(x=>Convert.ToInt32(x,16)):Enumerable.Range(0,256))
            for(int seed=0;seed<32;seed++)Case(op,seed);
        Console.WriteLine("Opcodes="+covered.Count+" vectors="+checks+" failing="+string.Join(",",errors.Select(x=>x.Key.ToString("X2")+":"+x.Value)));
        return errors.Count==0?0:1;
    }
}
