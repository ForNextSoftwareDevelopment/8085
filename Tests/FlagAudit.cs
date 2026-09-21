using System;
using System.Linq;
using _8085;

static class FlagAudit
{
    static int bad, cases; static string group;
    static int F(Assembler85 c){return(c.flagS?128:0)|(c.flagZ?64:0)|(c.flagK?32:0)|(c.flagAC?16:0)|(c.flagP?4:0)|(c.flagV?2:0)|(c.flagC?1:0);}
    static void Set(Assembler85 c,int f){c.flagS=(f&128)!=0;c.flagZ=(f&64)!=0;c.flagK=(f&32)!=0;c.flagAC=(f&16)!=0;c.flagP=(f&4)!=0;c.flagV=(f&2)!=0;c.flagC=(f&1)!=0;}
    static bool Par(int n){int b=0;for(int i=0;i<8;i++)b+=(n>>i)&1;return b%2==0;}
    static int ResultFlags(int n,bool cy,bool ac,bool v){n&=255;return(n&128)|(n==0?64:0)|(((n>=128)^v)?32:0)|(ac?16:0)|(Par(n)?4:0)|(v?2:0)|(cy?1:0);}
    static void Step(Assembler85 c,int op){c.registerPC=0x2000;c.RAM[0x2000]=(byte)op;ushort next=c.registerPC;string e=c.RunInstruction(next,ref next);if(e!="")throw new Exception(e);}
    static void Check(bool ok,string info){cases++;if(!ok){if(bad++==0)Console.WriteLine("FAIL "+group+" "+info);}}
    public static int Run(string[] args)
    {
        foreach(string name in new[]{"alu","inx-dcx","dad","rotates","psw","rim-sim","ei"})
        {
            if(args.Length>1&&!args.Skip(1).Contains(name))continue;
            group=name;int start=bad;var c=new Assembler85(new string[0]);
            if(name=="alu")for(int op=0;op<8;op++)for(int a=0;a<256;a++)for(int b=0;b<256;b++)for(int cy=0;cy<2;cy++)
            {
                int carry=(op==1||op==3)?cy:0;bool sub=op==2||op==3||op==7;
                int q=op<=3||op==7?(sub?a-b-carry:a+b+carry):op==4?a&b:op==5?a^b:a|b;
                int signed=(sbyte)a+(sub?-(sbyte)b:(sbyte)b)+(sub?-carry:carry);
                bool v=(op<=3||op==7)&&(signed<-128||signed>127);
                bool ac=op<=3||op==7?(sub?(a&15)>=(b&15)+carry:(a&15)+(b&15)+carry>15):op==4;
                int expected=ResultFlags(q,(op<=3||op==7)&&(q<0||q>255),ac,v);
                Set(c,0xf6|cy);c.registerA=(byte)a;c.RAM[0x2001]=(byte)b;Step(c,0xc6+8*op);
                Check(c.registerA==(op==7?a:q&255)&&F(c)==expected,"op="+op+" A="+a+" B="+b+" carry="+cy+" f="+F(c)+" expected="+expected);
            }
            if(name=="inx-dcx")for(int pair=0;pair<4;pair++)foreach(int n in new[]{0,1,0x7fff,0x8000,0xfffe,0xffff})foreach(int f in new[]{0,0xf7})foreach(bool dec in new[]{false,true})
            {
                c.registerB=c.registerD=c.registerH=(byte)(n>>8);c.registerC=c.registerE=c.registerL=(byte)n;c.registerSP=(ushort)n;Set(c,f);
                Step(c,(dec?0x0b:3)+16*pair);int result=pair==0?(c.registerB<<8)|c.registerC:pair==1?(c.registerD<<8)|c.registerE:pair==2?(c.registerH<<8)|c.registerL:c.registerSP;
                int ef=(f&~32)|((dec?n==0:n==65535)?32:0);
                Check(result==((n+(dec?-1:1))&65535)&&F(c)==ef,"pair="+pair+" value="+n+" dec="+dec+" f="+F(c)+" expected="+ef);
            }
            if(name=="dad")foreach(int a in new[]{0,1,0x7fff,0x8000,0xffff})foreach(int b in new[]{0,1,0x7fff,0x8000,0xffff})foreach(int f in new[]{0,0xf7})
            {
                c.registerH=(byte)(a>>8);c.registerL=(byte)a;c.registerB=(byte)(b>>8);c.registerC=(byte)b;Set(c,f);Step(c,9);
                int sum=(short)a+(short)b,expected=(f&~3)|(a+b>65535?1:0)|((sum<-32768||sum>32767)?2:0);
                Check(F(c)==expected,"a="+a+" b="+b+" f="+F(c)+" expected="+expected);
            }
            if(name=="rotates")foreach(int op in new[]{7,15,23,31})for(int a=0;a<256;a++)foreach(int f in new[]{0,0xf7})
            {
                c.registerA=(byte)a;Set(c,f);Step(c,op);bool left=op==7||op==23;bool v=left&&(((a^(a<<1))&128)!=0);
                int expected=(f&~3)|(left?a>>7:a&1)|(v?2:0);
                Check(F(c)==expected,"op="+op+" A="+a+" f="+F(c)+" expected="+expected);
            }
            if(name=="psw")for(int f=0;f<256;f++)
            {
                c.registerSP=0xffff;c.RAM[0xffff]=(byte)f;c.RAM[0]=0xa5;Step(c,0xf1);
                Check(F(c)==(f&0xf7)&&c.registerA==0xa5&&c.registerSP==1,"POP "+f);
                Step(c,0xf5);Check(c.RAM[0xffff]==(f&0xf7)&&c.RAM[0]==0xa5,"PUSH "+f);
            }
            if(name=="rim-sim")for(int a=0;a<256;a++)foreach(bool prior in new[]{false,true})
            {
                c.intrM55=c.intrM65=c.intrM75=c.intrP75=c.sod=prior;c.registerA=(byte)a;Set(c,0xf7);Step(c,0x30);
                bool masks=(a&8)!=0;
                Check(c.intrM55==(masks?(a&1)!=0:prior)&&c.intrM65==(masks?(a&2)!=0:prior)&&c.intrM75==(masks?(a&4)!=0:prior)&&
                    c.intrP75==((a&16)!=0?false:prior)&&c.sod==((a&64)!=0?(a&128)!=0:prior)&&F(c)==0xf7,"SIM "+a);
                c.intrM55=(a&1)!=0;c.intrM65=(a&2)!=0;c.intrM75=(a&4)!=0;c.intrIE=(a&8)!=0;
                c.intrP55=(a&16)!=0;c.intrP65=(a&32)!=0;c.intrP75=(a&64)!=0;c.sid=(a&128)!=0;Step(c,0x20);
                Check(c.registerA==a&&F(c)==0xf7,"RIM "+a);
            }
            if(name=="ei"){
                c.intrIE=false;Step(c,0xfb);Check(!c.intrIE,"EI must defer enabling until the following instruction completes");
                Step(c,0);Check(c.intrIE,"EI NOP enables");Step(c,0xf3);Check(!c.intrIE,"DI disables");
                Step(c,0xfb);Step(c,0xf3);Step(c,0);Check(!c.intrIE,"DI cancels pending EI");
            }
            Console.WriteLine(name+": "+(bad-start)+" failures");
        }
        Console.WriteLine("Flag audit cases="+cases+" failures="+bad);return bad==0?0:1;
    }
}
