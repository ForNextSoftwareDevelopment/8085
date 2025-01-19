using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8085
{
    class DisAssembler85
    {
        #region Members

        // Type of the decoded instruction: (un)conditional jump/call/restart or none
        public enum TYPE
        {
            NONE, 
            CONDITIONALJUMP, 
            CONDITIONALCALL, 
            UNCONDITIONALJUMP, 
            UNCONDITIONALCALL, 
            CONDITIONALRETURN, 
            UNCONDITIONALRETURN, 
            CONDITIONALRESTART, 
            UNCONDITIONALRESTART,
            PCHL
        }

        // Binary file buffer
        private byte[] bytes;

        // Instruction has been decoded already
        private bool[] lineDecoded;

        // All start addresses to be decoded, will be filled along the way after conditional jumps
        private Dictionary<UInt16, bool> addresses;

        // Load address of the code
        private UInt16 loadAddress;

        // Use labels for jump/call adresses
        private bool useLabels;

        // Address Symbol Table
        private Dictionary<UInt16, string> addressSymbolTable = new Dictionary<UInt16, string>();

        // Datatable with addresses and instructions of resulting program 
        private DataTable dtProgram;

        // Resulting (lined) program text
        public string program = "";
        public string linedprogram = "";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public DisAssembler85()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="loadAddress"></param>
        /// <param name="startAddress"></param>
        public DisAssembler85(byte[] bytes, UInt16 loadAddress, UInt16 startAddress, bool useLabels)
        {
            this.bytes = bytes;
            this.loadAddress = loadAddress;
            this.useLabels = useLabels;

            lineDecoded = new bool[0xFFFF];
            addresses = new Dictionary<ushort, bool>();

            for (int i=0; i<lineDecoded.Length; i++)
            {
                lineDecoded[i] = false;
            }

            // Add first address to start disassemble from
            addresses.Add(startAddress, false);

            // Check if address range falls into hardware interrupts and/or restarts (add those addresses too)
            UInt16 lowestAddress = loadAddress;
            UInt16 highestAddress = Convert.ToUInt16(loadAddress + Convert.ToUInt16(bytes.Length));

            // RST 4.5 (TRAP, Non-Maskable), SDK-85: Timer Interrupt entry point 
            if ((lowestAddress <= 0x0024) && (highestAddress >= 0x00024))
            {
                if (!addresses.ContainsKey(0x0024)) addresses.Add(0x0024, false);
            }

            // RST 5.5, SDK-85: Input Interrupt entry point 
            if ((lowestAddress <= 0x002C) && (highestAddress >= 0x0002C))
            {
                if (!addresses.ContainsKey(0x002C)) addresses.Add(0x002C, false);
            }

            // RST 6.5, SDK-85: Hard Wired User Interrupt entry point 
            if ((lowestAddress <= 0x0034) && (highestAddress >= 0x00034))
            {
                if (!addresses.ContainsKey(0x0034)) addresses.Add(0x0034, false);
            }

            // RST 7.5, SDK-85: Keyboard Interrupt entry point 
            if ((lowestAddress <= 0x003C) && (highestAddress >= 0x0003C))
            {
                if (!addresses.ContainsKey(0x003C)) addresses.Add(0x003C, false);
            }

            // RST 0 
            if ((lowestAddress <= 0x0000) && (highestAddress >= 0x00000))
            {
                // Must already been added as loadaddress
            }

            // RST 1
            if ((lowestAddress <= 0x0008) && (highestAddress >= 0x00008))
            {
                if (!addresses.ContainsKey(0x0008)) addresses.Add(0x0008, false);
            }

            // RST 2
            if ((lowestAddress <= 0x0010) && (highestAddress >= 0x00010))
            {
                if (!addresses.ContainsKey(0x0010)) addresses.Add(0x0010, false);
            }

            // RST 3
            if ((lowestAddress <= 0x0018) && (highestAddress >= 0x00018))
            {
                if (!addresses.ContainsKey(0x0018)) addresses.Add(0x0018, false);
            }

            // RST 4
            if ((lowestAddress <= 0x0020) && (highestAddress >= 0x00020))
            {
                if (!addresses.ContainsKey(0x0020)) addresses.Add(0x0020, false);
            }

            // RST 5
            if ((lowestAddress <= 0x0028) && (highestAddress >= 0x00028))
            {
                if (!addresses.ContainsKey(0x0028)) addresses.Add(0x0028, false);
            }

            // RST 6
            if ((lowestAddress <= 0x0030) && (highestAddress >= 0x00030))
            {
                if (!addresses.ContainsKey(0x0030)) addresses.Add(0x0030, false);
            }

            // RST 7
            if ((lowestAddress <= 0x0038) && (highestAddress >= 0x00038))
            {
                if (!addresses.ContainsKey(0x0038)) addresses.Add(0x0038, false);
            }

            // RST8 (Used in the unspecified RSTV instruction)
            if ((lowestAddress <= 0x0040) && (highestAddress >= 0x00040))
            {
                if (!addresses.ContainsKey(0x0040)) addresses.Add(0x0040, false);
            }

            // Init program text
            dtProgram = new DataTable();
            dtProgram.Columns.Add("address", typeof(UInt16));
            dtProgram.Columns.Add("instruction", typeof(String));
            dtProgram.Columns.Add("size", typeof(UInt16));
            dtProgram.Columns.Add("type", typeof(int));

            dtProgram.Rows.Add(null, "ORG " + loadAddress.ToString("X4"), 0);
        }

        #endregion

        #region Methods

        public string Parse(UInt16 startAddress = 0xFFFF)
        {
            bool ready;

            // If startaddress has been supplied, use this 
            if (startAddress != 0xFFFF)
            {
                if (!addresses.ContainsKey(startAddress))
                {
                    addresses.Add(startAddress, false);
                }
            }

            do
            {
                ready = true;
                
                // Copy address table for the loop (otherwise we can't add to the original addresses dictionary)
                Dictionary<UInt16, bool> addresses_copy = new Dictionary<UInt16, bool>(addresses);
                
                // Check all adddresses in the dictionary for the program path to decode the instructions in this path
                foreach (KeyValuePair<UInt16, bool> keyValuePair in addresses_copy)
                {
                    UInt16 addressKeyValuePair = keyValuePair.Key;
                    UInt16 address = addressKeyValuePair;

                    // Done this program path allready ?
                    bool done = keyValuePair.Value;

                    while (((address - loadAddress) >= 0) && ((address - loadAddress) < bytes.Length) && (!lineDecoded[address]) && !done)
                    {
                        // Set address of the current instruction to be processed
                        UInt16 addressCurrentInstruction = address;

                        // Still some path's to go
                        ready = false;

                        string instruction = Decode(bytes[(UInt16)(address - loadAddress)], out uint count, out TYPE type);
                        lineDecoded[address] = true;

                        // No operands
                        if (count == 0) 
                        {
                            // Check for end of the path (RETURN, RESTART)
                            if ((type == TYPE.UNCONDITIONALRETURN) ||
                                (type == TYPE.UNCONDITIONALRESTART) ||
                                (type == TYPE.PCHL))
                            {
                                done = true;
                            }

                            // Check for conditional end of the path (RETURN ON ZERO, RESTART ON OVERFLOW etc.)
                            if ((type == TYPE.CONDITIONALRETURN) ||
                                (type == TYPE.CONDITIONALRESTART))
                            {
                                // No action
                            }

                            address++;
                        }

                        // 1 operand
                        if (count == 1)
                        {
                            address++;
                            if ((address - loadAddress) < bytes.Length)
                            {
                                string operand = " " + bytes[(UInt16)(address - loadAddress)].ToString("X2") + "H";
                                instruction += operand;
                                lineDecoded[address] = true;
                                address++;
                            }
                        }

                        // 2 operands
                        if (count == 2)
                        {
                            byte firstByte = 0x00;
                            byte secondByte = 0x00;

                            string first = "?";
                            string second = "?";

                            address++;
                            if ((address - loadAddress) < bytes.Length)
                            {
                                firstByte = bytes[(UInt16)(address - loadAddress)];
                                first = firstByte.ToString("X2");
                                lineDecoded[address] = true;

                                address++;
                                if ((address - loadAddress) < bytes.Length)
                                {
                                    secondByte = bytes[(UInt16)(address - loadAddress)];
                                    second = secondByte.ToString("X2");
                                    lineDecoded[address] = true;
                                }
                            }

                            if ((useLabels)  &&
                                ((type == TYPE.CONDITIONALJUMP) ||
                                 (type == TYPE.UNCONDITIONALJUMP) ||
                                 (type == TYPE.CONDITIONALCALL) ||
                                 (type == TYPE.UNCONDITIONALCALL)))
                            {
                                UInt16 jmpsubAddress = (UInt16)(secondByte * 0x100 + firstByte);

                                if (!addressSymbolTable.ContainsKey(jmpsubAddress))
                                {
                                    // Add to dictionary and insert
                                    int i = 0;
                                    if ((type == TYPE.CONDITIONALJUMP) || (type == TYPE.UNCONDITIONALJUMP))
                                    {
                                        while (addressSymbolTable.ContainsValue("lbl_jmp" + i.ToString("D4"))) i++;
                                        addressSymbolTable.Add(jmpsubAddress, "lbl_jmp" + i.ToString("D4"));
                                        instruction += " lbl_jmp" + i.ToString("D4");
                                    }

                                    if ((type == TYPE.CONDITIONALCALL) || (type == TYPE.UNCONDITIONALCALL))
                                    {
                                        while (addressSymbolTable.ContainsValue("lbl_sub" + i.ToString("D4"))) i++;
                                        addressSymbolTable.Add(jmpsubAddress, "lbl_sub" + i.ToString("D4"));
                                        instruction += " lbl_sub" + i.ToString("D4");
                                    }
                                } else
                                {
                                    // Just insert
                                    instruction += " " + addressSymbolTable[jmpsubAddress];
                                }
                            } else
                            {
                                // Inverted (low byte first, high byte second)
                                string operand = " " + second + first + "H";

                                instruction += operand;
                            }

                            // Next address
                            address++;

                            // Check for 'fork' of the path (JUMP ON PARITY, CALL ON ZERO etc.)
                            if ((type == TYPE.CONDITIONALJUMP) ||
                                (type == TYPE.CONDITIONALCALL) ||
                                (type == TYPE.UNCONDITIONALCALL))
                            {
                                UInt16 newAddress = (UInt16)(secondByte * 0x100 + firstByte);
                                if (!addresses.ContainsKey(newAddress)) addresses.Add(newAddress, false);
                            }

                            // Check for change of the path (JUMP)
                            if (type == TYPE.UNCONDITIONALJUMP)
                            {
                                address = (UInt16)(secondByte * 0x100 + firstByte);
                            }
                        }

                        // Add program line (address + instruction)
                        dtProgram.Rows.Add(addressCurrentInstruction, instruction, count + 1, type);
                    }

                    // Set this key has been done
                    addresses[addressKeyValuePair] = true;
                }
            } while (!ready);

            // Order program lines to address
            dtProgram.DefaultView.Sort = "address";
            dtProgram = dtProgram.DefaultView.ToTable();

            // Copy addressSymbolTable to check for use
            Dictionary<UInt16, string> addressSymbolTableNotUsed = new Dictionary<UInt16, string>(addressSymbolTable);

            program = "";
            linedprogram = "";
            UInt16 lastAddress = loadAddress;
            UInt16 lastSize = 0x0000;
            foreach (DataRow row in dtProgram.Rows)
            {
                // If this instruction is at an address which has a gap with the previous one, then fill with data (DB)
                if (row["address"] != DBNull.Value)
                {
                    UInt16 currentAddress = Convert.ToUInt16(row["address"]);

                    UInt16 currentSize = 0;
                    if (row["size"] != DBNull.Value)
                    {
                        currentSize = Convert.ToUInt16(row["size"]);
                    }

                    // Check for gap, treat this as data
                    int dbSize = currentAddress - lastAddress - lastSize;
                    if (dbSize > 0)
                    {
                        string arg = " ";
                        string argASCII = "    ; ASCII: ";
                        for (int i = lastAddress + lastSize; i < currentAddress; i++)
                        {
                            if (i != lastAddress + lastSize) arg += ", ";
                            byte dataByte = bytes[(UInt16)(i - loadAddress)];
                            arg += dataByte.ToString("X2") + "H";
                            argASCII += (dataByte > 0x20) && (dataByte < 0x80) ? ((char)(bytes[(UInt16)(i - loadAddress)])).ToString() : ".";
                        }

                        if (useLabels) program += "             ";
                        program += "DB " + arg + argASCII + "\r\n";

                        linedprogram += (lastAddress + lastSize).ToString("X4") + ": ";
                        if (useLabels) linedprogram += "         ";
                        linedprogram += "DB " + arg + argASCII + "\r\n";
                    }

                    if (currentAddress != 0) lastAddress = currentAddress;
                    if (currentSize != 0) lastSize = currentSize;
                }

                // If labels are being used, fill them into the program
                if (useLabels)
                {
                    if (row["address"] != DBNull.Value)
                    {
                        UInt16 currentAddress = Convert.ToUInt16(row["address"]);

                        if (addressSymbolTable.ContainsKey(currentAddress))
                        {
                            program += addressSymbolTable[currentAddress] + ": ";
                            program += row["instruction"] + "\r\n";

                            linedprogram += row["address"] != DBNull.Value ? Convert.ToUInt16(row["address"]).ToString("X4") + ": " : "";
                            linedprogram += addressSymbolTable[currentAddress] + ": ";
                            linedprogram += row["instruction"];

                            // Delete from (copied) table
                            if (addressSymbolTableNotUsed.ContainsKey(currentAddress))
                            {
                                addressSymbolTableNotUsed.Remove(currentAddress);
                            }
                        } else
                        {
                            program += "             ";
                            program += row["instruction"] + "\r\n";

                            linedprogram += row["address"] != DBNull.Value ? Convert.ToUInt16(row["address"]).ToString("X4") + ": " : "";
                            linedprogram += "             ";
                            linedprogram += row["instruction"];
                        }
                    } else
                    {
                        program += row["instruction"] + "\r\n";
                        linedprogram += row["address"] != DBNull.Value ? Convert.ToUInt16(row["address"]).ToString("X4") + ": " : "";
                        linedprogram += row["instruction"];
                    }
                } else
                {
                    program += row["instruction"] + "\r\n";
                    linedprogram += row["address"] != DBNull.Value ? Convert.ToUInt16(row["address"]).ToString("X4") + ": " : "";
                    linedprogram += row["instruction"];
                }

                if ((row["type"] != DBNull.Value) && (Convert.ToInt32(row["type"]) == Convert.ToInt32(TYPE.PCHL))) linedprogram += " ; Warning: Branch address cannot be determined";
                linedprogram += "\r\n";
            }

            // If any data left after last line processed, treat this as byte data (DB)
            if (loadAddress + bytes.Length - lastAddress - lastSize > 0)
            {
                string arg = " ";
                string argASCII = "    ; ASCII: ";
                for (int i = lastAddress + lastSize; i < (loadAddress + bytes.Length); i++)
                {
                    if (i != lastAddress + lastSize) arg += ", ";
                    byte dataByte = bytes[(UInt16)(i - loadAddress)];
                    arg += dataByte.ToString("X2") + "h";
                    argASCII += (dataByte > 0x20) && (dataByte < 0x80) ? ((char)(bytes[(UInt16)(i - loadAddress)])).ToString() : ".";
                }

                if (useLabels) program += "             ";
                program += "db " + arg + argASCII + "\r\n";

                linedprogram += (lastAddress + lastSize).ToString("X4") + ": ";
                if (useLabels) linedprogram += "         ";
                linedprogram += "db " + arg + argASCII + "\r\n";
            }

            // Add not used address labels as EQU statements
            if (useLabels)
            {
                string labels = "";
                foreach (KeyValuePair<UInt16, string> kvp in addressSymbolTableNotUsed)
                {
                    labels += kvp.Value + " EQU " + kvp.Key.ToString("X4") + "H\r\n";
                }

                if (labels != "")
                {
                    program = labels + "\r\n" + program;
                    linedprogram = labels + "\r\n" + linedprogram;
                }
            }

            return (program);
        }

        /// <summary>
        /// Decode a singe instruction and state the number of operand bytes
        /// </summary>
        /// <param name="hexcode"></param>
        /// <param name="count"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string Decode(byte hexcode, out uint count, out TYPE type)
        {
            string opcode = "";
            count = 0;
            type = TYPE.NONE;

            switch (hexcode)
            {
                case 0x00:
                    opcode = "NOP";
                    break;
		        case 0x01:
                    opcode = "LXI B,";
                    count = 2;
                    break;
		        case 0x02:
                    opcode = "STAX B";
                    break;
		        case 0x03:
                    opcode = "INX B";
                    break;
		        case 0x04:
                    opcode = "INR B";
                    break;
		        case 0x05:
                    opcode = "DCR B";
                    break;
		        case 0x06:
                    opcode = "MVI B,";
                    count = 1;
                    break;
                case 0x07:
                    opcode = "RLC";
                    break;
		        case 0x08:
                    opcode = "DSUB";
                    break;
		        case 0x09:
                    opcode = "DAD B";
                    break;
		        case 0x0a:
                    opcode = "LDAX B";
                    break;
		        case 0x0b:
                    opcode = "DCX B";
                    break;
		        case 0x0c:
                    opcode = "INR C";
                    break;
		        case 0x0d:
                    opcode = "DCR C";
                    break;
		        case 0x0e:
                    opcode = "MVI C,";
                    count = 1;
                    break;
		        case 0x0f:
                    opcode = "RRC";
                    break;
		        case 0x10:
                    opcode = "ARHL";
                    break;
		        case 0x11:
                    opcode = "LXI D,";
                    count = 2;
                    break;
		        case 0x12:
                    opcode = "STAX D";
                    break;
		        case 0x13:
                    opcode = "INX D";
                    break;
		        case 0x14:
                    opcode = "INR D";
                    break;
		        case 0x15:
                    opcode = "DCR D";
                    break;
		        case 0x16:
                    opcode = "MVI D,";
                    count = 1;
                    break;
		        case 0x17:
                    opcode = "RAL";
                    break;
		        case 0x18:
                    opcode = "RDEL";
                    break;
		        case 0x19:
                    opcode = "DAD D";
                    break;
		        case 0x1a:
                    opcode = "LDAX D";
                    break;
		        case 0x1b:
                    opcode = "DCX D";
                    break;
		        case 0x1c:
                    opcode = "INR E";
                    break;
		        case 0x1d:
                    opcode = "DCR E";
                    break;
		        case 0x1e:
                    opcode = "MVI E,";
                    count = 1;
                    break;
		        case 0x1f:
                    opcode = "RAR";
                    break;
		        case 0x20:
                    opcode = "RIM";
                    break;
		        case 0x21:
                    opcode = "LXI H,";
                    count = 2;
                    break;
		        case 0x22:
                    opcode = "SHLD";
                    count = 2;
                    break;
		        case 0x23:
                    opcode = "INX H";
                    break;
		        case 0x24:
                    opcode = "INR H";
                    break;
		        case 0x25:
                    opcode = "DCR H";
                    break;
		        case 0x26:
                    opcode = "MVI H,";
                    count = 1;
                    break;
		        case 0x27:
                    opcode = "DAA";
                    break;
		        case 0x28:
                    opcode = "LDHI";
                    break;
                case 0x29:
                    opcode = "DAD H";
                    break;
		        case 0x2a:
                    opcode = "LHLD";
                    count = 2;
                    break;
		        case 0x2b:
                    opcode = "DCX H";
                    break;
		        case 0x2c:
                    opcode = "INR L";
                    break;
		        case 0x2d:
                    opcode = "DCR L";
                    break;
		        case 0x2e:
                    opcode = "MVI L,";
                    count = 1;
                    break;
		        case 0x2f:
                    opcode = "CMA";
                    break;
		        case 0x30:
                    opcode = "SIM";
                    break;
		        case 0x31:
                    opcode = "LXI SP,";
                    count = 2;
                    break;
		        case 0x32:
                    opcode = "STA";
                    count = 2;
                    break;
		        case 0x33:
                    opcode = "INX SP";
                    break;
		        case 0x34:
                    opcode = "INR M";
                    break;
		        case 0x35:
                    opcode = "DCR M";
                    break;
		        case 0x36:
                    opcode = "MVI M,";
                    count = 1;
                    break;
		        case 0x37:
                    opcode = "STC";
                    break;
		        case 0x38:
                    opcode = "LDSI";
                    count = 1;
                    break;
                case 0x39:
                    opcode = "DAD SP";
                    break;
		        case 0x3a:
                    opcode = "LDA";
                    count = 2;
                    break;
		        case 0x3b:
                    opcode = "DCX SP";
                    break;
		        case 0x3c:
                    opcode = "INR A";
                    break;
		        case 0x3d:
                    opcode = "DCR A";
                    break;
		        case 0x3e:
                    opcode = "MVI A,";
                    count = 1;
                    break;
		        case 0x3f:
                    opcode = "CMC";
                    break;
		        case 0x40:
                    opcode = "MOV B,B";
                    break;
		        case 0x41:
                    opcode = "MOV B,C";
                    break;
		        case 0x42:
                    opcode = "MOV B,D";
                    break;
		        case 0x43:
                    opcode = "MOV B,E";
                    break;
		        case 0x44:
                    opcode = "MOV B,H";
                    break;
		        case 0x45:
                    opcode = "MOV B,L";
                    break;
		        case 0x46:
                    opcode = "MOV B,M";
                    break;
		        case 0x47:
                    opcode = "MOV B,A";
                    break;
		        case 0x48:
                    opcode = "MOV C,B";
                    break;
		        case 0x49:
                    opcode = "MOV C,C";
                    break;
		        case 0x4a:
                    opcode = "MOV C,D";
                    break;
		        case 0x4b:
                    opcode = "MOV C,E";
                    break;
		        case 0x4c:
                    opcode = "MOV C,H";
                    break;
		        case 0x4d:
                    opcode = "MOV C,L";
                    break;
		        case 0x4e:
                    opcode = "MOV C,M";
                    break;
		        case 0x4f:
                    opcode = "MOV C,A";
                    break;
		        case 0x50:
                    opcode = "MOV D,B";
                    break;
		        case 0x51:
                    opcode = "MOV D,C";
                    break;
		        case 0x52:
                    opcode = "MOV D,D";
                    break;
		        case 0x53:
                    opcode = "MOV D,E";
                    break;
		        case 0x54:
                    opcode = "MOV D,H";
                    break;
		        case 0x55:
                    opcode = "MOV D,L";
                    break;
		        case 0x56:
                    opcode = "MOV D,M";
                    break;
		        case 0x57:
                    opcode = "MOV D,A";
                    break;
		        case 0x58:
                    opcode = "MOV E,B";
                    break;
		        case 0x59:
                    opcode = "MOV E,C";
                    break;
		        case 0x5a:
                    opcode = "MOV E,D";
                    break;
		        case 0x5b:
                    opcode = "MOV E,E";
                    break;
		        case 0x5c:
                    opcode = "MOV E,H";
                    break;
		        case 0x5d:
                    opcode = "MOV E,L";
                    break;
		        case 0x5e:
                    opcode = "MOV E,M";
                    break;
		        case 0x5f:
                    opcode = "MOV E,A";
                    break;
		        case 0x60:
                    opcode = "MOV H,B";
                    break;
		        case 0x61:
                    opcode = "MOV H,C";
                    break;
		        case 0x62:
                    opcode = "MOV H,D";
                    break;
		        case 0x63:
                    opcode = "MOV H,E";
                    break;
		        case 0x64:
                    opcode = "MOV H,H";
                    break;
		        case 0x65:
                    opcode = "MOV H,L";
                    break;
		        case 0x66:
                    opcode = "MOV H,M";
                    break;
		        case 0x67:
                    opcode = "MOV H,A";
                    break;
		        case 0x68:
                    opcode = "MOV L,B";
                    break;
		        case 0x69:
                    opcode = "MOV L,C";
                    break;
		        case 0x6a:
                    opcode = "MOV L,D";
                    break;
		        case 0x6b:
                    opcode = "MOV L,E";
                    break;
		        case 0x6c:
                    opcode = "MOV L,H";
                    break;
		        case 0x6d:
                    opcode = "MOV L,L";
                    break;
		        case 0x6e:
                    opcode = "MOV L,M";
                    break;
		        case 0x6f:
                    opcode = "MOV L,A";
                    break;
		        case 0x70:
                    opcode = "MOV M,B";
                    break;
		        case 0x71:
                    opcode = "MOV M,C";
                    break;
		        case 0x72:
                    opcode = "MOV M,D";
                    break;
		        case 0x73:
                    opcode = "MOV M,E";
                    break;
		        case 0x74:
                    opcode = "MOV M,H";
                    break;
		        case 0x75:
                    opcode = "MOV M,L";
                    break;
		        case 0x76:
                    opcode = "HLT";
                    break;
		        case 0x77:
                    opcode = "MOV M,A";
                    break;
		        case 0x78:
                    opcode = "MOV A,B";
                    break;
		        case 0x79:
                    opcode = "MOV A,C";
                    break;
		        case 0x7a:
                    opcode = "MOV A,D";
                    break;
		        case 0x7b:
                    opcode = "MOV A,E";
                    break;
		        case 0x7c:
                    opcode = "MOV A,H";
                    break;
		        case 0x7d:
                    opcode = "MOV A,L";
                    break;
		        case 0x7e:
                    opcode = "MOV A,M";
                    break;
		        case 0x7f:
                    opcode = "MOV A,A";
                    break;
		        case 0x80:
                    opcode = "ADD B";
                    break;
		        case 0x81:
                    opcode = "ADD C";
                    break;
		        case 0x82:
                    opcode = "ADD D";
                    break;
		        case 0x83:
                    opcode = "ADD E";
                    break;
		        case 0x84:
                    opcode = "ADD H";
                    break;
		        case 0x85:
                    opcode = "ADD L";
                    break;
		        case 0x86:
                    opcode = "ADD M";
                    break;
		        case 0x87:
                    opcode = "ADD A";
                    break;
		        case 0x88:
                    opcode = "ADC B";
                    break;
		        case 0x89:
                    opcode = "ADC C";
                    break;
		        case 0x8a:
                    opcode = "ADC D";
                    break;
		        case 0x8b:
                    opcode = "ADC E";
                    break;
		        case 0x8c:
                    opcode = "ADC H";
                    break;
		        case 0x8d:
                    opcode = "ADC L";
                    break;
		        case 0x8e:
                    opcode = "ADC M";
                    break;
		        case 0x8f:
                    opcode = "ADC A";
                    break;
		        case 0x90:
                    opcode = "SUB B";
                    break;
		        case 0x91:
                    opcode = "SUB C";
                    break;
		        case 0x92:
                    opcode = "SUB D";
                    break;
		        case 0x93:
                    opcode = "SUB E";
                    break;
		        case 0x94:
                    opcode = "SUB H";
                    break;
		        case 0x95:
                    opcode = "SUB L";
                    break;
		        case 0x96:
                    opcode = "SUB M";
                    break;
		        case 0x97:
                    opcode = "SUB A";
                    break;
		        case 0x98:
                    opcode = "SBB B";
                    break;
		        case 0x99:
                    opcode = "SBB C";
                    break;
		        case 0x9a:
                    opcode = "SBB D";
                    break;
		        case 0x9b:
                    opcode = "SBB E";
                    break;
		        case 0x9c:
                    opcode = "SBB H";
                    break;
		        case 0x9d:
                    opcode = "SBB L";
                    break;
		        case 0x9e:
                    opcode = "SBB M";
                    break;
		        case 0x9f:
                    opcode = "SBB A";
                    break;
		        case 0xa0:
                    opcode = "ANA B";
                    break;
		        case 0xa1:
                    opcode = "ANA C";
                    break;
		        case 0xa2:
                    opcode = "ANA D";
                    break;
		        case 0xa3:
                    opcode = "ANA E";
                    break;
		        case 0xa4:
                    opcode = "ANA H";
                    break;
		        case 0xa5:
                    opcode = "ANA L";
                    break;
		        case 0xa6:
                    opcode = "ANA M";
                    break;
		        case 0xa7:
                    opcode = "ANA A";
                    break;
		        case 0xa8:
                    opcode = "XRA B";
                    break;
		        case 0xa9:
                    opcode = "XRA C";
                    break;
		        case 0xaa:
                    opcode = "XRA D";
                    break;
		        case 0xab:
                    opcode = "XRA E";
                    break;
		        case 0xac:
                    opcode = "XRA H";
                    break;
		        case 0xad:
                    opcode = "XRA L";
                    break;
		        case 0xae:
                    opcode = "XRA M";
                    break;
		        case 0xaf:
                    opcode = "XRA A";
                    break;
		        case 0xb0:
                    opcode = "ORA B";
                    break;
		        case 0xb1:
                    opcode = "ORA C";
                    break;
		        case 0xb2:
                    opcode = "ORA D";
                    break;
		        case 0xb3:
                    opcode = "ORA E";
                    break;
		        case 0xb4:
                    opcode = "ORA H";
                    break;
		        case 0xb5:
                    opcode = "ORA L";
                    break;
		        case 0xb6:
                    opcode = "ORA M";
                    break;
		        case 0xb7:
                    opcode = "ORA A";
                    break;
		        case 0xb8:
                    opcode = "CMP B";
                    break;
		        case 0xb9:
                    opcode = "CMP C";
                    break;
		        case 0xba:
                    opcode = "CMP D";
                    break;
		        case 0xbb:
                    opcode = "CMP E";
                    break;
		        case 0xbc:
                    opcode = "CMP H";
                    break;
		        case 0xbd:
                    opcode = "CMP L";
                    break;
		        case 0xbe:
                    opcode = "CMP M";
                    break;
		        case 0xbf:
                    opcode = "CMP A";
                    break;
		        case 0xc0:
                    opcode = "RNZ";
                    break;
		        case 0xc1:
                    opcode = "POP B";
                    break;
		        case 0xc2:
                    opcode = "JNZ";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xc3:
                    opcode = "JMP";
                    count = 2;
                    type = TYPE.UNCONDITIONALJUMP;
                    break;
		        case 0xc4:
                    opcode = "CNZ";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xc5:
                    opcode = "PUSH B";
                    break;
		        case 0xc6:
                    opcode = "ADI";
                    count = 1;
                    break;
		        case 0xc7:
                    opcode = "RST 0";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xc8:
                    opcode = "RZ";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xc9:
                    opcode = "RET";
                    type = TYPE.UNCONDITIONALRETURN;
                    break;
		        case 0xca:
                    opcode = "JZ";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xcb:
                    opcode = "RSTV";
                    type = TYPE.CONDITIONALRESTART;
                    break;
		        case 0xcc:
                    opcode = "CZ";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xcd:
                    opcode = "CALL";
                    count = 2;
                    type = TYPE.UNCONDITIONALCALL;
                    break;
		        case 0xce:
                    opcode = "ACI";
                    count = 1;
                    break;
		        case 0xcf:
                    opcode = "RST 1";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xd0:
                    opcode = "RNC";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xd1:
                    opcode = "POP D";
                    break;
		        case 0xd2:
                    opcode = "JNC";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xd3:
                    opcode = "OUT";
                    count = 1;
                    break;
		        case 0xd4:
                    opcode = "CNC";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xd5:
                    opcode = "PUSH D";
                    break;
		        case 0xd6:
                    opcode = "SUI";
                    count = 1;
                    break;
		        case 0xd7:
                    opcode = "RST 2";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xd8:
                    opcode = "RC";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xd9:
                    opcode = "SHLX";
                    break;
		        case 0xda:
                    opcode = "JC";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xdb:
                    opcode = "IN";
                    count = 1;
                    break;
		        case 0xdc:
                    opcode = "CC";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xdd:
                    opcode = "JNK";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xde:
                    opcode = "SBI";
                    count = 1;
                    break;
		        case 0xdf:
                    opcode = "RST 3";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xe0:
                    opcode = "RPO";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xe1:
                    opcode = "POP H";
                    break;
		        case 0xe2:
                    opcode = "JPO";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xe3:
                    opcode = "XTHL";
                    break;
		        case 0xe4:
                    opcode = "CPO";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xe5:
                    opcode = "PUSH H";
                    break;
		        case 0xe6:
                    opcode = "ANI";
                    count = 1;
                    break;
		        case 0xe7:
                    opcode = "RST 4";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xe8:
                    opcode = "RPE";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xe9:
                    opcode = "PCHL";
                    type = TYPE.PCHL;
                    break;
		        case 0xea:
                    opcode = "JPE";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xeb:
                    opcode = "XCHG";
                    break;
		        case 0xec:
                    opcode = "CPE";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xed:
                    opcode = "LHLX";
                    break;
		        case 0xee:
                    opcode = "XRI";
                    count = 1;
                    break;
		        case 0xef:
                    opcode = "RST 5";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xf0:
                    opcode = "RP";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xf1:
                    opcode = "POP PSW";
                    break;
		        case 0xf2:
                    opcode = "JP";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xf3:
                    opcode = "DI";
                    break;
		        case 0xf4:
                    opcode = "CP";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xf5:
                    opcode = "PUSH PSW";
                    break;
		        case 0xf6:
                    opcode = "ORI";
                    count = 1;
                    break;
		        case 0xf7:
                    opcode = "RST 6";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;
		        case 0xf8:
                    opcode = "RM";
                    type = TYPE.CONDITIONALRETURN;
                    break;
		        case 0xf9:
                    opcode = "SPHL";
                    break;
                case 0xfa:
                    opcode = "JM";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xfb:
                    opcode = "EI";
                    break;
		        case 0xfc:
                    opcode = "CM";
                    count = 2;
                    type = TYPE.CONDITIONALCALL;
                    break;
		        case 0xfd:
                    opcode = "JK";
                    count = 2;
                    type = TYPE.CONDITIONALJUMP;
                    break;
		        case 0xfe:
                    opcode = "CPI";
                    count = 1;
                    break;
		        case 0xff:
                    opcode = "RST 7";
                    type = TYPE.UNCONDITIONALRESTART;
                    break;

                default:
                    opcode = "UNKNOWN";
                    break;
            }

            return (opcode);
        }

        #endregion
    }
}
