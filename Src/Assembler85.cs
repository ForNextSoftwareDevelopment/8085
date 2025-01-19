using Microsoft.Win32;
using System;
using System.Collections.Generic;   
using System.Windows.Forms; 

namespace _8085
{
    class Assembler85
    {
        #region Members

        // Operator types at arithmetic operations
        public enum OPERATOR
        {
            NONE = 0,
            ADD = 1,
            SUB = 2,
            AND = 3,
            OR = 4,
            XOR = 5
        }

        // Segment types
        public enum SEGMENT
        {
            ASEG = 0,
            CSEG = 1,
            DSEG = 2
        }

        // Segment currently active
        SEGMENT segment = SEGMENT.ASEG;

        // Absolute program segment, Code program segment, Data program segment
        UInt16 ASEG = 0x0000;
        UInt16 CSEG = 0x0000;
        UInt16 DSEG = 0x0000;

        // Total RAM of 65536 bytes (0x0000 - 0xFFFF)
        public byte[] RAM = new byte[0x10000];

        // Total 256 PORTS (0x00 - 0xFF)
        public byte[] PORT = new byte[0x0100];   
        
        // Linenumber for a given byte of program
        public int[] RAMprogramLine = new int[0x10000];

        // Address Symbol Table
        public Dictionary<string, int> addressSymbolTable = new Dictionary<string, int>();

        // Processed program for running second pass
        public string[] programRun;

        // Program listing
        public string[] programView;   
        
        // Current instruction to be processed
        private byte byteInstruction = 00;

        // Start location of the program
        public int startLocation;

        // Current location of the program (during firstpass and secondpass)
        public int locationCounter;

        // Register values
        public byte registerA = 0x00;
        public byte registerB = 0x00;
        public byte registerC = 0x00;
        public byte registerD = 0x00;
        public byte registerE = 0x00;
        public byte registerH = 0x00;
        public byte registerL = 0x00;

        public UInt16 registerPC = 0x0000;
        public UInt16 registerSP = 0x0000;

        // Flag values
        public bool flagC  = false;
        public bool flagV  = false;
        public bool flagP  = false;
        public bool flagAC = false;
        public bool flagK  = false;
        public bool flagZ  = false;
        public bool flagS  = false;

        // Interrupt Mask values
        public bool intrM55 = false;
        public bool intrM65 = false;
        public bool intrM75 = false;
        public bool intrIE = false;
        public bool intrP55 = false;
        public bool intrP65 = false;
        public bool intrP75 = false;

        // Serial Input/Output data (SID/SOD)
        public bool sid = false;
        public bool sod = false;

        // Write to display address
        public bool writeToDisplay = false;

        // Clock cycles executed
        public UInt64 cycles = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor 
        /// </summary>        
        public Assembler85(string[] program)
        {
            this.programRun = program;
            this.programView = new string[program.Length];

            startLocation = 0;
            registerSP = 0x0000;

            for (int i= 0; i < RAMprogramLine.Length; i++)
            {
                RAMprogramLine[i] = -1;
            }

            for (int i= 0; i < RAM.Length; i++)
            {
                RAM[i] = 0x00;
            }
        }

        #endregion

        #region Methods (Div)

        /// <summary>
        /// Get register index from string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private int RegisterIndex(string s) 
        {
            switch (s.ToUpper())
            {
                case "0":
                case "B": return 0;

                case "1":
                case "C": return 1;

                case "2":
                case "D": return 2;

                case "3":
                case "E": return 3;

                case "4":
                case "H": return 4;

                case "5":
                case "L": return 5;

                case "6":
                case "M": return 6;

                case "7":
                case "A": return 7;

                default: return -1;
            }

        }

        /// <summary>
        /// Calculate and adjust the flags on screen
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="carry"></param>
        /// <param name="type"></param>
        private byte Calculate(byte arg1, byte arg2, byte carry, OPERATOR type)
        {
            int i,count;
            byte b1, b2;
            byte result = (byte)0x00;

            flagV = false;
            flagK = false;

            switch (type)
            {
                case OPERATOR.ADD:
                    result = (byte)(arg1 + arg2 + carry);

                    // Carry flag
                    if (arg1 + arg2 + carry > 0xFF)
                    {
                        flagC = true;
                    } else 
                    {
                        flagC = false;
                    }

                    // Auxiliary carry flag
                    b1 = (byte)(arg1 & 0x0F);  // Masking upper 4 bits
                    b2 = (byte)(arg2 & 0x0F);  // Masking upper 4 bits

                    if (b1 + b2 + carry > 0x0F)
                    {
                        flagAC = true;
                    } else
                    {
                        flagAC = false;
                    }

                    // Signed overflow flag
                    if ((arg1 >= 0x80) && (arg2 >= 0x80) && (result < 0x80)) flagV = true;
                    if ((arg1 >= 0x80) && (arg2 < 0x80)) flagV = false;
                    if ((arg1 < 0x80) && (arg2 >= 0x80)) flagV = false;
                    if ((arg1 < 0x80) && (arg2 < 0x80) && (result >= 0x80)) flagV = true;

                    break;

                case OPERATOR.SUB:
                    result = (byte)(arg1 - arg2 - carry);

                    // Carry flag
                    if (arg1 - arg2 - carry < 0x00)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }

                    // Auxiliary carry flag
                    b1 = (byte)(arg1 & 0x0F);  // Masking upper 4 bits
                    b2 = (byte)(arg2 & 0x0F);  // Masking upper 4 bits

                    if (b1 - b2 - carry < 0x00)
                    {
                        flagAC = true;
                    } else
                    {
                        flagAC = false;
                    }

                    // Signed overflow flag
                    if ((arg1 >= 0x80) && (arg2 >= 0x80)) flagV = false;
                    if ((arg1 >= 0x80) && (arg2 < 0x80) && (result < 0x80)) flagV = true;
                    if ((arg1 < 0x80) && (arg2 >= 0x80) && (result >= 0x80)) flagV = true;
                    if ((arg1 < 0x80) && (arg2 < 0x80)) flagV = false;

                    break;

                case OPERATOR.AND:
                    result = (byte)(arg1 & arg2);

                    flagC = false;
                    flagAC = true;

                    break;

                case OPERATOR.OR:
                    result = (byte)(arg1 | arg2);

                    flagC = false;
                    flagAC = false;

                    break;

                case OPERATOR.XOR:
                    result = (byte)(arg1 ^ arg2);

                    flagC = false;
                    flagAC = false;

                    break;
            }

            string strResult = Convert.ToString(Convert.ToInt32(result.ToString("X2"), 16), 2).PadLeft(8, '0');

            // Sign flag
            if (strResult[0] == '1')
            {
                flagS = true;
            } else
            {
                flagS = false;
            }

            // Zero flag
            if (strResult == "00000000")
            {
                flagZ = true;
            } else
            {
                flagZ = false;
            }

            // Parity flag
            count = 0;
            for (i= 0; i < 8; i++)
            {
                if (strResult[i] == '1') count++;
            }

            if (count % 2 == 0)
            {
                flagP = true;
            } else
            {
                flagP = false;
            }

            return (result);    
        }

        /// <summary>
        /// Calculate and adjust the flags on screen
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="carry"></param>
        /// <param name="type"></param>
        private UInt16 Calculate(UInt16 arg1, UInt16 arg2, UInt16 carry, OPERATOR type)
        {
            int i, count;
            UInt16 result = (UInt16)0x0000;

            flagV = false;
            flagK = false;

            switch (type)
            {
                case OPERATOR.ADD:
                    result = (UInt16)(arg1 + arg2 + carry);

                    // Carry flag
                    if (arg1 + arg2 + carry > 0xFFFF)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }

                    break;

                case OPERATOR.SUB:
                    result = (UInt16)(arg1 - arg2 - carry);
                    string strResult = Convert.ToString(Convert.ToInt32(result.ToString("X4"), 16), 2).PadLeft(16, '0');

                    // Carry flag
                    if (arg1 - arg2 - carry < 0x0000)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }

                    // Sign flag
                    if (strResult[0] == '1')
                    {
                        flagS = true;
                    } else
                    {
                        flagS = false;
                    }

                    // Zero flag
                    if (strResult == "0000000000000000")
                    {
                        flagZ = true;
                    } else
                    {
                        flagZ = false;
                    }

                    // Parity flag
                    count = 0;
                    for (i = 0; i < 16; i++)
                    {
                        if (strResult[i] == '1') count++;
                    }

                    if (count % 2 == 0)
                    {
                        flagP = true;
                    } else
                    {
                        flagP = false;
                    }

                    break;
            }

            return (result);
        }

        private byte GetByte(string arg, out string result)
        {
            // Replace $ with location counter
            if (arg.Length == 1) arg = arg.Replace("$", locationCounter.ToString());
            arg = arg.Replace("$ ", locationCounter.ToString() + " ");
            arg = arg.Replace("$+", locationCounter.ToString() + "+");
            arg = arg.Replace("$-", locationCounter.ToString() + "-");

            /// Split arguments
            string[] args = arg.Split(new char[] { ' ', '(', ')', '+', '-', '*', '/' });

            // Sort by size, longest string first to avoid partial replacements
            Array.Sort(args, (x, y) => y.Length.CompareTo(x.Length));

            // Replace all symbols from symbol table
            foreach (string str in args)
            {
                foreach (KeyValuePair<string, int> keyValuePair in addressSymbolTable)
                {
                    if (str.ToUpper().Trim() == keyValuePair.Key.ToUpper().Trim())
                    {
                        arg = arg.Replace(keyValuePair.Key, keyValuePair.Value.ToString());
                    }
                }
            }

            // Process low order byte of argument
            if (arg.ToUpper().Contains("LOW("))
            {
                int start = arg.IndexOf('(') + 1;
                int end = arg.IndexOf(')', start);
            
                if (end - start < 2)
                {
                    result = "Illegal argument for LOW(arg)";
                    return (0);
                }

                string argLow = arg.Substring(start, end - start);
                argLow = Convert.ToInt32(argLow).ToString("X4").Substring(2, 2);

                arg = Convert.ToInt32(argLow, 16).ToString() + arg.Substring(end + 1, arg.Length - 1 - end).Trim();
            }

            // Process high order byte of argument
            if (arg.ToUpper().Contains("HIGH("))
            {
                int start = arg.IndexOf('(') + 1;
                int end = arg.IndexOf(')', start);

                if (end - start < 2)
                {
                    result = "Illegal argument for HIGH(arg)";
                    return (0);
                }

                string argHigh = arg.Substring(start, end - start);
                argHigh = Convert.ToInt32(argHigh).ToString("X4").Substring(0,2);

                arg = Convert.ToInt32(argHigh,16).ToString() + arg.Substring(end + 1, arg.Length - 1 - end).Trim();
            }

            // Replace AND with & as token
            arg = arg.Replace("AND", "&");

            // Replace OR with | as token
            arg = arg.Replace("OR", "|");

            // Calculate expression
            byte calc = Calculator.CalculateByte(arg, out string res);

            // result string of the expression ("OK" or error message)
            result = res;

            return(calc);
        }

        private UInt16 Get2Bytes(string arg, out string result)
        {
            // Replace $ with location counter
            if (arg.Length == 1) arg = arg.Replace("$", locationCounter.ToString());
            arg = arg.Replace("$ ", locationCounter.ToString() + " ");
            arg = arg.Replace("$+", locationCounter.ToString() + "+");
            arg = arg.Replace("$-", locationCounter.ToString() + "-");

            /// Split arguments
            string[] args = arg.Split(new char[] { ' ', '(', ')', '+', '-', '*', '/' });

            // Sort by size, longest string first to avoid partial replacements
            Array.Sort(args, (x, y) => y.Length.CompareTo(x.Length));

            // Replace all symbols from symbol table
            foreach (string str in args)
            {
                foreach (KeyValuePair<string, int> keyValuePair in addressSymbolTable)
                {
                    if (str.ToUpper().Trim() == keyValuePair.Key.ToUpper().Trim())
                    {
                        arg = arg.Replace(keyValuePair.Key, keyValuePair.Value.ToString());
                    }
                }
            }

            // Replace AND with & as token
            arg = arg.Replace("AND", "&");

            // Replace OR with | as token
            arg = arg.Replace("OR", "|");

            // Calculate expression
            UInt16 calc = Calculator.Calculate2Bytes(arg, out string res);

            // result string of the expression ("OK" or error message)
            result = res;

            return (calc);
        }

        /// <summary>
        /// Convert integer to hexadecimal string representation
        /// </summary>
        /// <param name="n"></param>
        /// <param name="hi"></param>
        /// <param name="lo"></param>
        private void Get2ByteFromInt(int n, out string lo, out string hi)
        {
            string temp = n.ToString("X4");
            hi = temp.Substring(temp.Length - 4, 2);
            lo = temp.Substring(temp.Length - 2, 2);
        }

        /// <summary>
        /// Get the current content of a register
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private bool GetRegisterValue(byte arg, ref byte val)
        {
            switch (arg & 0b00001111)
            {
                case 0b0000:
                    val = registerB;
                    break;
                case 0b0001:
                    val = registerC;
                    break;
                case 0b0010:
                    val = registerD;
                    break;
                case 0b0011:
                    val = registerE;
                    break;
                case 0b0100:
                    val = registerH;
                    break;
                case 0b0101:
                    val = registerL;
                    break;
                case 0b0110:
                    val = RAM[registerH * 0x0100 + registerL];
                    break;
                case 0b0111:
                    val = registerA;
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Set the current content of a register
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private bool SetRegisterValue(byte arg, byte val)
        {
            switch (arg & 0b00001111)
            {
                case 0b0000:
                    registerB = val;
                    break;
                case 0b0001:
                    registerC = val;
                    break;
                case 0b0010:
                    registerD = val;
                    break;
                case 0b0011:
                    registerE = val;
                    break;
                case 0b0100:
                    registerH = val;
                    break;
                case 0b0101:
                    registerL = val;
                    break;
                case 0b0110:
                    RAM[registerH * 0x0100 + registerL] = val;
                    cycles += 1;
                    break;
                case 0b0111:
                    registerA = val;
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Clear the RAM
        /// </summary>
        public void ClearRam()
        {
            for (int i = 0; i < RAM.Length; i++)
            {
                RAM[i] = 0x00;
            }
        }

        /// <summary>
        /// Clear the Ports
        /// </summary>
        public void ClearPorts()
        {
            for (int i = 0; i < PORT.Length; i++)
            {
                PORT[i] = 0x00;
            }
        }

        #endregion

        #region Methods (FirstPass)

        /// <summary>
        /// First pass through the code, remove labels, check etc.
        /// </summary>
        /// <returns></returns>
        public string FirstPass()
        {
            // StartLocation denotes the first RAM location to which we are assembling the program
            // locationCounter is a temporary variable to traverse program for first pass
            locationCounter = startLocation;

            // Opcode in the line
            string opcode;

            // Operand(s) for the opcode 
            string[] operands;              

            char[] delimiters = new[] { ',' };

            // Process all lines
            for (int lineNumber = 0; lineNumber < programRun.Length; lineNumber ++)       
            {
                // Copy line of code to process and clear original line to rebuild
                string line = programRun[lineNumber];
                programRun[lineNumber] = "";
                programView[lineNumber] = "";

                opcode = null;
                operands = null;
                int InstructionStart = locationCounter;

                try
                {
                    // Replace all tabs with spaces
                    line = line.Replace('\t', ' ');

                    // if a comment is found, remove
                    int start_of_comment_pos = line.IndexOf(';');
                    if (start_of_comment_pos != -1)            
                    {
                        // Check if really a comment (; could be in a string or char array)
                        int num_quotes = 0;
                        for (int i = 0; i < start_of_comment_pos; i++)
                        {
                            if ((line[i] == '\'') || (line[i] == '\"')) num_quotes++;
                        }

                        if ((num_quotes % 2) == 0)
                        {
                            line = line.Remove(line.IndexOf(';')).Trim();
                            if (line == "") continue;
                        }
                    }

                    // Single or double quotes for strings
                    string temp = line;
                    line = "";
                    int index = 0;
                    while (index < temp.Length)
                    {
                        if ((temp[index] == '\"') || (temp[index] == '\''))
                        {
                            bool first = true;

                            // Char or string found
                            char endChar = temp[index];

                            if (index < temp.Length - 1) index++;
                            char processChar = temp[index];

                            // Replace until end of string found
                            while ((index < temp.Length) && (processChar != endChar))
                            {
                                processChar = temp[index];
                                if ((processChar != endChar))
                                {
                                    if (!first)
                                    {
                                        line += ",";
                                    }

                                    line += ((int)processChar).ToString("X2") + "H";
                                    first = false;
                                }
                                index++;
                            }

                            if (processChar != endChar)
                            {
                                return ("Unclosed string at line " + (lineNumber + 1));
                            }
                        } else
                        {
                            // Just copy the character
                            line += temp[index++];
                        }
                    }

                    // Make all chars uppercase, remove leading or trailing spaces
                    line = line.ToUpper().Trim();
                    if (line == "") continue;

                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:Quotes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                // Check for EQU directive 
                int equ_pos = line.IndexOf("EQU");
                if (equ_pos >= 0)
                {
                    string label = line.Split(new char[] { ' ' })[0].TrimEnd(':');

                    if (addressSymbolTable.ContainsKey(label))
                    {
                        return ("Label already used at line " + (lineNumber + 1));
                    }

                    if (equ_pos < line.Length - 4)
                    {
                        string val = line.Substring(equ_pos + 3).Trim();

                        int calc = Get2Bytes(val, out string result);
                        if (result != "OK")
                        {
                            return ("Invalid operand for EQU at line " + (lineNumber + 1));
                        }

                        // ADD the label/value
                        addressSymbolTable.Add(label, calc);

                        // Next line
                        continue;
                    } else
                    {
                        return ("Syntax: [LABEL] EQU [VALUE] at line " + (lineNumber + 1));
                    }
                }

                // Check for/get a label
                if (line.IndexOf(':') != -1)
                {
                    try
                    {
                        int end_of_label_pos = line.IndexOf(':');

                        // Check if really a LABEL (: could be in a string or char array)
                        int num_quotes = 0;
                        for (int i = 0; i < end_of_label_pos; i++)
                        {
                            if ((line[i] == '\'') || (line[i] == '\"')) num_quotes++;
                        }

                        if ((num_quotes % 2) == 0)
                        {
                            string label = line.Substring(0, end_of_label_pos).Trim();

                            // Check for spaces in label
                            if (label.Contains(" "))
                            {
                                return ("label '" + label + "' contains spaces at line " + (lineNumber + 1));
                            }

                            if (addressSymbolTable.ContainsKey(label))
                            {
                                return ("Label already used at line " + (lineNumber + 1));
                            }

                            if (line.Length > end_of_label_pos + 1)
                            {
                                line = line.Substring(end_of_label_pos + 1, line.Length - end_of_label_pos - 1).Trim();

                                // ADD the label/value
                                addressSymbolTable.Add(label, locationCounter);
                            } else
                            {
                                line = "";

                                // ADD the label/value
                                addressSymbolTable.Add(label, locationCounter);

                                // Next line
                                continue;
                            }
                        }
                    } catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, "FirstPass:Label", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                    }
                }

                // Get the opcode 
                try
                {
                    int end_of_opcode_pos = line.IndexOf(' ');
                    if ((end_of_opcode_pos == -1) && (line.Length != 0)) end_of_opcode_pos = line.Length;

                    if (end_of_opcode_pos <= 0)
                    {
                        // Next line
                        continue;
                    }

                    opcode = line.Substring(0, end_of_opcode_pos).Trim();

                    // Split the line and store the strings formed in array
                    operands = line.Substring(end_of_opcode_pos).Trim().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    // Rebuild the line
                    line = opcode;
                    while (line.Length < 6) line += " ";
                    for (int i=0; i<operands.Length; i++)
                    {
                        if (i != 0) line += ", ";
                        line += operands[i];
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:Opcode", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                try
                {
                    // Check for opcode (directive) DB
                    if (opcode.Equals("DB"))
                    {
                        line = "DB    ";

                        // Check if DB has strings 
                        for (int i=0; i < operands.Length; i++)
                        {
                            if (i != 0) line += ", ";
                            line += operands[i];
                        }
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                try
                {
                    // Check for opcode (directive) ASEG
                    if (opcode.Equals("ASEG"))
                    {
                        // Set current segment
                        segment = SEGMENT.ASEG;

                        // Set locationcounter
                        locationCounter = ASEG;

                        // Copy to program for second pass
                        programRun[lineNumber] = opcode;

                        // Copy to programView for examining
                        programView[lineNumber] = opcode;

                        // Next line
                        continue;
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:ASEG", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                try
                {
                    // Check for opcode (directive) CSEG
                    if (opcode.Equals("CSEG"))
                    {
                        // Set current segment
                        segment = SEGMENT.CSEG;

                        // Set locationcounter
                        locationCounter = CSEG;

                        // Copy to program for second pass
                        programRun[lineNumber] = opcode;

                        // Copy to programView for examining
                        programView[lineNumber] = opcode;

                        // Next line
                        continue;
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:CSEG", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                try
                {
                    // Check for opcode (directive) DSEG
                    if (opcode.Equals("DSEG"))
                    {
                        // Set current segment
                        segment = SEGMENT.DSEG;

                        // Set locationcounter
                        locationCounter = DSEG;

                        // Copy to program for second pass
                        programRun[lineNumber] = opcode;

                        // Copy to programView for examining
                        programView[lineNumber] = opcode;

                        // Next line
                        continue;
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:DSEG", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                try
                {
                    // Check for opcode (directive) ORG
                    if (opcode.Equals("ORG"))
                    {
                        // Line must have an argument after the opcode
                        if (operands.Length == 0)
                        {
                            return ("ORG directive must have an argument following at line " + (lineNumber + 1));
                        }

                        // If valid address then store in locationCounter
                        int calc = Get2Bytes(operands[0], out string result);
                        if (result == "OK")
                        {
                            locationCounter = calc;
                        } else
                        {
                            return ("Invalid operand for " + opcode + "(" + result + ") at line " + (lineNumber + 1));
                        }

                        // Copy to program for second pass
                        programRun[lineNumber] = opcode + " " + operands[0];

                        // Copy to programView for examining
                        programView[lineNumber] = opcode + " " + operands[0];

                        // Next line
                        continue;
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:ORG", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                // Count the operand(s)
                try
                { 
                    switch (opcode)    
                    {
                        case "DB":
                            if (operands.Length == 0)
                            {
                                return ("DB directive has too few operands at line " + (lineNumber + 1));
                            }

                            // Loop for traversing after DB
                            for (int pos = 0; pos < operands.Length; pos++)
                            {
                                // Check for char operand
                                if (operands[pos].Contains("'"))
                                {
                                    // Size of chars minus 2 bytes for ' 
                                    locationCounter += operands[pos].Length - 2;
                                } else if (operands[pos].Contains("\""))
                                {
                                    // Size of string minus 2 bytes for " plus one byte for end of string char (0)
                                    locationCounter += operands[pos].Length - 1; 
                                } else
                                {
                                    // get to next location by skipping location for byte
                                    locationCounter++;
                                }
                            }

                            break;

                        case "DW":

                            if (operands.Length == 0)
                            {
                                return ("DW directive has too few operands at line " + (lineNumber + 1));
                            }

                            for (int pos = 0; pos < operands.Length; pos++)
                            {
                                // Get to next location by skipping location for 2 bytes
                                locationCounter += 2;
                            }

                            break;

                        case "DS":

                            if (operands.Length == 0)
                            {
                                return ("DS directive has too few operands at line " + (lineNumber + 1));
                            }

                            // If valid address then store in locationCounter
                            int calc = Get2Bytes(operands[0], out string result);
                            if (result == "OK")
                            {
                                locationCounter += calc;
                            } else
                            {
                                return ("Invalid operand for " + opcode + "(" + result + ") at line " + (lineNumber + 1));
                            }

                            break;
                        
                        case "ARHL":
                        case "CMA":
                        case "CMC":
                        case "DAA":
                        case "DI":
                        case "DSUB":
                        case "EI":
                        case "HLT":
                        case "LHLX":
                        case "NOP":
                        case "PCHL":
                        case "RAL":
                        case "RAR":
                        case "RDEL":
                        case "RET":
                        case "RC":
                        case "RIM":
                        case "RLC":
                        case "RM":
                        case "RNC":
                        case "RNZ":
                        case "RP":
                        case "RPE":
                        case "RPO":
                        case "RRC":
                        case "RSTV":
                        case "RZ":
                        case "SIM":
                        case "SHLX":
                        case "SPHL":
                        case "STC":
                        case "XCHG":
                        case "XTHL":

                            // No operands
                            if (operands.Length != 0)
                            {
                                return("There should be no operands for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // Single byte instructions
                            locationCounter += 1;

                            if (segment == SEGMENT.ASEG) ASEG = (UInt16)locationCounter;
                            if (segment == SEGMENT.CSEG) CSEG = (UInt16)locationCounter;
                            if (segment == SEGMENT.DSEG) DSEG = (UInt16)locationCounter;

                            break;

                        case "ADC":
                        case "ADD":
                        case "ANA":
                        case "CMP":
                        case "DAD":
                        case "DCR":
                        case "DCX":
                        case "INR":
                        case "INX":
                        case "LDAX":
                        case "ORA":
                        case "POP":
                        case "PUSH":
                        case "RST":
                        case "SBB":
                        case "STAX":
                        case "SUB":
                        case "XRA":

                            // One operand
                            if (operands.Length != 1)
                            {
                                return("There should be one operand for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // Single byte instructions
                            locationCounter += 1;
                            break;

                        case "MOV":

                            // Two operands
                            if (operands.Length != 2)
                            {
                                return ("There should be two operands for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // Single byte instructions
                            locationCounter += 1;
                            break;

                        case "ACI":
                        case "ADI":
                        case "ANI":
                        case "CPI":
                        case "IN":
                        case "LDHI":
                        case "LDSI":
                        case "ORI":
                        case "OUT":
                        case "SBI":
                        case "SUI":
                        case "XRI":

                            // One operand
                            if (operands.Length != 1)
                            {
                                return("There should be one operand for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // 2 byte instructions
                            locationCounter += 2;   
                            break;

                        case "MVI":

                            // Two operands
                            if (operands.Length != 2)
                            {
                                return ("There should be two operands for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // 2 byte instructions
                            locationCounter += 2;
                            break;

                        case "CALL":
                        case "CC":
                        case "CNC":
                        case "CM":
                        case "CNZ":
                        case "CP":
                        case "CPE":
                        case "CPO":
                        case "CZ":
                        case "JC":
                        case "JK":
                        case "JM":
                        case "JMP":
                        case "JNC":
                        case "JNK":
                        case "JNX5":
                        case "JNZ":
                        case "JP":
                        case "JPE":
                        case "JPO":
                        case "JX5":
                        case "JZ":
                        case "LDA":
                        case "LHLD":
                        case "SHLD":
                        case "STA":

                            // One operand
                            if (operands.Length != 1)
                            {
                                return("There should be one operand for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // 3 byte instructions
                            locationCounter += 3;   
                            break;

                        case "LXI":

                            // Two operands
                            if (operands.Length != 2)
                            {
                                return ("There should be two operands for: '" + opcode + "' at line " + (lineNumber + 1));
                            }

                            // 3 byte instructions
                            locationCounter += 3;
                            break;

                        case "END":
                            return "OK";

                        default:
                            return("Unknown opcode/directive: '" + opcode + "' at line " + (lineNumber + 1));
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "FirstPass:OPCODE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                // Update current segment
                if (segment == SEGMENT.ASEG) ASEG = (UInt16)locationCounter;
                if (segment == SEGMENT.CSEG) CSEG = (UInt16)locationCounter;
                if (segment == SEGMENT.DSEG) DSEG = (UInt16)locationCounter;

                //  Copy the edited program (without labels and EQU) to new array of strings
                //  The new program array of strings will be used in second pass
                programRun[lineNumber] = line;

                // Copy to programView for examining
                programView[lineNumber] = InstructionStart.ToString("X4") + ": " + line;
            }

            return ("OK");
        }

        #endregion

        #region Methods (SecondPass)

        /// <summary>
        /// Second pass through the code, convert instructions etc.
        /// </summary>
        /// <returns></returns>
        public string SecondPass()
        {
            // StartLocation gives the location from which we have to start assembling
            // Using locationCounter to traverse the location of RAM during second pass
            locationCounter = startLocation; 

            // Opcode in the line
            string opcode;

            // Operand(s) for the opcode 
            string[] operands;

            // Split operands by these delimeter(s)
            char[] delimiters = new[] {','};

            // Reset segments
            ASEG = 0;
            CSEG = 0;
            DSEG = 0;

            // Temporary variables
            byte calcByte;
            UInt16 calcShort;
            int temp, k;
            string str;

            for (int lineNumber = 0; lineNumber < programRun.Length; lineNumber ++)
            {
                int locationCounterInstructionStart = locationCounter;

                try
                {
                    // Empty line
                    if ((programRun[lineNumber] == null) || (programRun[lineNumber] == ""))
                    {
                        // If line is empty, there is no need to check
                        continue;
                    }

                    int end_of_opcode_pos = programRun[lineNumber].IndexOf(' ');
                    if ((end_of_opcode_pos == -1) && (programRun[lineNumber].Length != 0)) end_of_opcode_pos = programRun[lineNumber].Length;

                    if (end_of_opcode_pos <= 0)
                    {
                        // Next line
                        continue;
                    }

                    opcode = programRun[lineNumber].Substring(0, end_of_opcode_pos).Trim();

                    if (RAMprogramLine[locationCounter] != -1)
                    {
                        return ("Allready code at 0x" + locationCounter.ToString("X4") + " (from line " + (RAMprogramLine[locationCounter] +1).ToString() + ") for " + opcode + " at line " + (lineNumber + 1));
                    }

                    // Split the line and store the strings formed in array
                    operands = programRun[lineNumber].Substring(end_of_opcode_pos).Trim().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    // Remove spaces and tabs from operands
                    for (int j=0; j<operands.Length; j++)
                    {
                        operands[j] = operands[j].Trim();
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "SECONDPASS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }

                try
                {
                    // Check instruction
                    switch (opcode)        
                    {
                        case "ASEG":                                                                                    // ASEG
                            segment = SEGMENT.ASEG;
                            locationCounter = ASEG;
                            break;
                        case "CSEG":                                                                                    // CSEG
                            segment = SEGMENT.CSEG;
                            locationCounter = CSEG;
                            break;
                        case "DSEG":                                                                                    // DSEG
                            segment = SEGMENT.DSEG;
                            locationCounter = DSEG;
                            break;
                        case "ORG":                                                                                     // ORG
                            if (operands.Length == 0)   
                            {
                                // Must have an operand
                                return ("Missing operand for " + opcode + " at line " + (lineNumber + 1));
                            } else 
                            {
                                // If valid address then store in locationCounter
                                calcShort = Get2Bytes(operands[0], out string result);
                                if (result == "OK")
                                {
                                    locationCounter = calcShort;
                                } else
                                {
                                    return ("Invalid operand for " + opcode + ": " + result + " at line " + (lineNumber + 1));
                                }
                            }
                            break;
                        case "END":                                                                                     // END
                            return ("OK");
                        case "DB":                                                                                      // DB
                            for (k = 0; k < operands.Length; k++) 
                            {
                                // Extract all DB operands
                                calcByte = GetByte(operands[k], out string resultDB);
                                if (resultDB == "OK")
                                {
                                    RAMprogramLine[locationCounter] = lineNumber;
                                    RAM[locationCounter++] = calcByte;
                                } else
                                {
                                    return ("Invalid operand for " + opcode + ": " + resultDB + " at line " + (lineNumber + 1));
                                }
                            }
                            break;
                        case "DW":                                                                                      // DN
                            for (k = 0; k < operands.Length; k++)
                            {
                                // Extract all DW operands
                                calcShort = Get2Bytes(operands[k], out string resultDW);
                                if (resultDW == "OK")
                                {
                                    str = calcShort.ToString("X4");
                                    RAMprogramLine[locationCounter] = lineNumber;
                                    RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                    RAMprogramLine[locationCounter] = lineNumber;
                                    RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                                } else
                                {
                                    return ("Invalid operand for " + opcode + ": " + resultDW + " at line " + (lineNumber + 1));
                                }
                            }
                            break;
                        case "DS":                                                                                      // DS
                            calcShort = Get2Bytes(operands[0], out string resultDS);
                            if (resultDS == "OK")
                            {
                                while (calcShort != 0)
                                {
                                    // We don't have to initialize operands for DS, just reserve space for them
                                    RAMprogramLine[locationCounter] = lineNumber;
                                    locationCounter++;
                                    calcShort--;
                                }
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultDS + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "ACI":                                                                                     // ACI
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("CE", 16);
                            calcByte = GetByte(operands[0], out string resultACI);
                            if (resultACI == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultACI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "ADC":                                                                                     // ADC
                            k = 0x88;
                            if (RegisterIndex(operands[0]) == -1) 
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);    
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte(str, 16); 
                            break;
                        case "ADD":                                                                                     // ADD
                            k = 0x80;
                            if (RegisterIndex(operands[0]) == -1) 
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]); 
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte(str, 16); 
                            break;
                        case "ADI":                                                                                     // ADI
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("C6", 16);
                            calcByte = GetByte(operands[0], out string resultADI);
                            if (resultADI == "OK")
                            {
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultADI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "ANA":                                                                                     // ANA
                            k = 0xA0;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "ANI":                                                                                     // ANI
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("E6", 16);
                            calcByte = GetByte(operands[0], out string resultANI);
                            if (resultANI == "OK")
                            {
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultANI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "ARHL":                                                                                    // ARHL
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("10", 16);
                            break;
                        case "CALL":                                                                                    // CALL
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("CD", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCALL);
                            if (resultCALL == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCALL + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CC":                                                                                      // CC
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("DC", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCC);
                            if (resultCC == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCC + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CM":                                                                                      // CM
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("FC", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCM);
                            if (resultCM == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCM + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CMA":                                                                                     // CMA
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("2F", 16);
                            break;
                        case "CMC":                                                                                     // CMC
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("3F", 16);
                            break;
                        case "CMP":                                                                                     // CMP
                            k = 0xB8;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "CNC":                                                                                     // CNC
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("D4", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCNC);
                            if (resultCNC == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCNC + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CNZ":                                                                                     // CNZ
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("C4", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCNZ);
                            if (resultCNZ == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCNZ + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CP":                                                                                      // CP
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("F4", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCP);
                            if (resultCP == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCP + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CPE":                                                                                     // CPE
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("EC", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCPE);
                            if (resultCPE == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCPE + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CPI":                                                                                     // CPI
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("FE", 16);
                            calcByte = GetByte(operands[0], out string resultCPI);
                            if (resultCPI == "OK")
                            {
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCPI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CPO":                                                                                     // CPO
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("E4", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCPO);
                            if (resultCPO == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCPO + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "CZ":                                                                                      // CZ
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("CC", 16);
                            calcShort = Get2Bytes(operands[0], out string resultCZ);
                            if (resultCZ == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultCZ + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "DAA":                                                                                     // DAA
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("27", 16);
                            break;
                        case "DAD":                                                                                     // DAD
                            k = 0x09;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "DCR":                                                                                     // DCR
                            k = 0x05;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += (RegisterIndex(operands[0]))*0x08 ;
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "DCX":                                                                                     // DCX
                            k = 0x0B;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "DI":                                                                                      // DI
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("F3", 16);
                            break;
                        case "DSUB":                                                                                    // DSUB
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("08", 16);
                            break;
                        case "EI":                                                                                      // EI
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("FB", 16);
                            break;
                        case "HLT":                                                                                     // HLT
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("76", 16);
                            break;
                        case "IN":                                                                                      // IN
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("DB", 16);
                            calcByte = GetByte(operands[0], out string resultIN);
                            if (resultIN == "OK")
                            {
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultIN);
                            }
                            break;
                        case "INR":                                                                                     // INR
                            k = 0x04;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0])*8;
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "INX":                                                                                     // INX
                            k = 0x03;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "JC":                                                                                      // JC
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("DA", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJC);
                            if (resultJC == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJC + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JK":                                                                                      // JK
                        case "JX5":                                                                                     // JX5
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("FD", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJK);
                            if (resultJK == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJK + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JM":                                                                                      // JM
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("FA", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJM);
                            if (resultJM == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJM + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JMP":                                                                                     // JMP
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("C3", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJMP);
                            if (resultJMP == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJMP + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JNC":                                                                                     // JNC
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("D2", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJNC);
                            if (resultJNC == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJNC + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JNK":                                                                                     // JNK
                        case "JNX5":                                                                                    // JNX5
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("DD", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJNK);
                            if (resultJNK == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJNK + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JNZ":                                                                                     // JNZ
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("C2", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJNZ);
                            if (resultJNZ == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJNZ + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JP":                                                                                      // JP
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("F2", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJP);
                            if (resultJP == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJP + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JPE":                                                                                     // JPE
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("EA", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJPE);
                            if (resultJPE == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJPE + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JPO":                                                                                     // JPO
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("E2", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJPO);
                            if (resultJPO == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJPO + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "JZ":                                                                                      // JZ
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("CA", 16);
                            calcShort = Get2Bytes(operands[0], out string resultJZ);
                            if (resultJZ == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultJZ + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "LDA":                                                                                     // LDA
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("3A", 16);
                            calcShort = Get2Bytes(operands[0], out string resultLDA);
                            if (resultLDA == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultLDA + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "LDAX":                                                                                    // LDAX
                            k = 0x0A;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "LDHI":                                                                                    // LDHI
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("28", 16);
                            calcByte = GetByte(operands[0], out string resultLDHI);
                            if (resultLDHI == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultLDHI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "LDSI":                                                                                    // LDSI
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter] = Convert.ToByte("38", 16);
                            calcByte = GetByte(operands[0], out string resultLDSI);
                            if (resultLDSI == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultLDSI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "LHLD":                                                                                    // LHLD
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("2A", 16);
                            calcShort = Get2Bytes(operands[0], out string resultLHLD);
                            if (resultLHLD == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultLHLD + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "LHLX":                                                                                    // LHLX
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("ED", 16);
                            break;
                        case "LXI":                                                                                     // LXI
                            k = 0x01;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte(str, 16);
                            calcShort = Get2Bytes(operands[1], out string resultLXI);
                            if (resultLXI == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultLXI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "MOV":                                                                                     // MOV
                            k = 0x40;
                            k = k + 0x08 * RegisterIndex(operands[0]) + RegisterIndex(operands[1]);
                            if ((k == 0x76) || (RegisterIndex(operands[0]) == -1) || ( RegisterIndex(operands[1]) == -1 ))
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            } else
                            {
                                str = k.ToString("X2");
                                RAMprogramLine[locationCounter] = lineNumber; 
                                RAM[locationCounter++] = Convert.ToByte(str, 16);
                            }
                            break;
                        case "MVI":                                                                                     // MVI
                            k = 0x06;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0])*0x08;
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte(str, 16);
                            calcByte = GetByte(operands[1], out string resultMVI);
                            if (resultMVI == "OK")
                            {
                                locationCounter++;
                                str = calcByte.ToString("X2");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultMVI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "NOP":                                                                                     // NOP
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("00", 16);
                            break;
                        case "ORA":                                                                                     // ORA
                            k = 0xB0;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "ORI":                                                                                     // ORI
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("F6", 16);
                            calcByte = GetByte(operands[0], out string resultORI);
                            if (resultORI == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultORI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "OUT":                                                                                     // OUT
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("D3", 16);
                            calcByte = GetByte(operands[0], out string resultOUT);
                            if (resultOUT == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultOUT + " at line " + (lineNumber + 1));
                            }
                            break;  
                        case "PCHL":                                                                                    // PCHL
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("E9", 16);
                            break;
                        case "POP":                                                                                     // POP
                            k = 0xC1;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "PSW": k += 0x30;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "PUSH":                                                                                    // PUSH
                            k = 0xC5;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "PSW": k += 0x30;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "RAL":                                                                                     // RAL
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("17", 16);
                            break;
                        case "RAR":                                                                                     // RAR
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("1F", 16);
                            break;
                        case "RC":                                                                                      // RC
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("D8", 16);
                            break;
                        case "RDEL":                                                                                    // RDEL
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("18", 16);
                            break;
                        case "RET":                                                                                     // RET
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("C9", 16);
                            break;
                        case "RIM":                                                                                     // RIM
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("20", 16);
                            break;
                        case "RLC":                                                                                     // RLC
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("07", 16);
                            break;
                        case "RNZ":                                                                                     // RNZ
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("C0", 16);
                            break;
                        case "RM":                                                                                      // RM
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("F8", 16);
                            break;
                        case "RNC":                                                                                     // RNC
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("D0", 16);
                            break;
                        case "RP":                                                                                      // RP
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("F0", 16);
                            break;
                        case "RPE":                                                                                     // RPE
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("E8", 16);
                            break;
                        case "RPO":                                                                                     // RPO
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("E0", 16);
                            break;
                        case "RRC":                                                                                     // RRC
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("0F", 16);
                            break;
                        case "RST":                                                                                     // RST
                            k = 0xC7;
                            temp = GetByte(operands[0], out string resultRST);
                            if (resultRST == "OK")
                            {
                                k = k + temp * 0x08;
                                str = k.ToString("X2");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str, 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "RSTV":                                                                                    // RSTV
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("CB", 16);
                            break;
                        case "RZ":                                                                                      // RZ
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("C8", 16);
                            break;
                        case "SBB":                                                                                     // SBB
                            k = 0x98;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "SBI":                                                                                     // SBI
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("DE", 16);
                            calcByte = GetByte(operands[0], out string resultSBI);
                            if (resultSBI == "OK")
                            {
                                locationCounter++;
                                str = calcByte.ToString("X2");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultSBI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "SHLD":                                                                                    // SHLD
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("22", 16);
                            calcShort = Get2Bytes(operands[0], out string resultSHLD);
                            if (resultSHLD == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultSHLD + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "SHLX":                                                                                    // SHLX
                            RAMprogramLine[locationCounter] = lineNumber;
                            RAM[locationCounter++] = Convert.ToByte("D9", 16);
                            break;
                        case "SIM":                                                                                     // SIM
                            {
                                RAMprogramLine[locationCounter] = lineNumber; 
                                RAM[locationCounter++] = Convert.ToByte("30", 16);
                                break;
                            }
                        case "SPHL":                                                                                    // SPHL
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("F9", 16);
                            break;
                        case "STA":                                                                                     // STA
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("32", 16);
                            calcShort = Get2Bytes(operands[0], out string resultSTA);
                            if (resultSTA == "OK")
                            {
                                locationCounter++;
                                str = calcShort.ToString("X4");
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(2, 2), 16);
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = Convert.ToByte(str.Substring(0, 2), 16);
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultSTA + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "STAX":                                                                                    // STAX
                            k = 0x02;
                            switch (operands[0])
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                default:
                                    return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "STC":                                                                                     // SIC
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("37", 16);
                            break;
                        case "SUB":                                                                                     // SUB
                            k = 0x90;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "SUI":                                                                                     // SUI
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("D6", 16);
                            calcByte = GetByte(operands[0], out string resultSUI);
                            if (resultSUI == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultSUI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "XCHG":                                                                                    // XCHG
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("EB", 16);
                            break;
                        case "XRA":                                                                                     // XRA
                            k = 0xA8;
                            if (RegisterIndex(operands[0]) == -1)
                            {
                                return ("Invalid operand for " + opcode + " at line " + (lineNumber + 1));
                            }
                            k += RegisterIndex(operands[0]);
                            str = k.ToString("X2");
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte(str, 16);
                            break;
                        case "XRI":                                                                                     // XRI
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter] = Convert.ToByte("EE", 16);
                            calcByte = GetByte(operands[0], out string resultXRI);
                            if (resultXRI == "OK")
                            {
                                locationCounter++;
                                RAMprogramLine[locationCounter] = lineNumber;
                                RAM[locationCounter++] = calcByte;
                            } else
                            {
                                return ("Invalid operand for " + opcode + ": " + resultXRI + " at line " + (lineNumber + 1));
                            }
                            break;
                        case "XTHL":                                                                                    // XTHL
                            RAMprogramLine[locationCounter] = lineNumber; 
                            RAM[locationCounter++] = Convert.ToByte("E3", 16);
                            break;
                        default:
                            return ("Invalid instruction '" + opcode + "' at line " + (lineNumber + 1));
                    }

                    // Show ascii if DB
                    if ((opcode == "DB") && (operands.Length > 0))
                    {
                        calcByte = GetByte(operands[0], out string resultDB);
                        if (resultDB == "OK")
                        {
                            if ((calcByte >= 32) && (calcByte < 127))
                            {
                                programView[lineNumber] += " ('" + Convert.ToChar(calcByte) + "')";
                            }
                        }
                    }

                    // Show ascii if MVI
                    if ((opcode == "MVI") && (operands.Length > 1))
                    {
                        calcByte = GetByte(operands[1], out string resultMVI);
                        if (resultMVI == "OK")
                        {
                            if ((calcByte >= 32) && (calcByte < 127))
                            {
                                programView[lineNumber] += " ('" + Convert.ToChar(calcByte) + "')";
                            }
                        }
                    }

                    // Show ascii if CPI
                    if ((opcode == "CPI") && (operands.Length > 0))
                    {
                        calcByte = GetByte(operands[0], out string resultCPI);
                        if (resultCPI == "OK")
                        {
                            if ((calcByte >= 32) && (calcByte < 127))
                            {
                                programView[lineNumber] += " ('" + Convert.ToChar(calcByte) + "')";
                            }
                        }
                    }

                    // Update current segment
                    if (segment == SEGMENT.ASEG) ASEG = (UInt16)locationCounter;
                    if (segment == SEGMENT.CSEG) CSEG = (UInt16)locationCounter;
                    if (segment == SEGMENT.DSEG) DSEG = (UInt16)locationCounter;

                    if ((opcode != "ORG") && (opcode != "ASEG") && (opcode != "CSEG") && (opcode != "DSEG"))
                     {
                        while (programView[lineNumber].Length < 46)
                        {
                            programView[lineNumber] += " ";
                        }

                        for (int i = locationCounterInstructionStart; i < locationCounter; i++)
                        {
                            programView[lineNumber] += " " + RAM[i].ToString("X2");
                        }
                    }
                } catch (Exception exception)
                {
                    if (locationCounter > 0xFFFF)
                    {
                        return ("MEMORY OVERRUN AT LINE " + (lineNumber + 1));
                    }

                    MessageBox.Show(exception.Message, "SECONDPASS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }
            }

            return ("OK");
        }

        #endregion

        #region Methods (RunInstruction)


        /// <summary>
        /// Run program from memory address
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="nextAddress"></param>
        /// <returns></returns>
        public string RunInstruction(UInt16 startAddress, ref UInt16 nextAddress)
        { 
            int num;
            bool result;
            byte val = 0x00;
            registerPC = startAddress;
            string lo, hi;

            byteInstruction = RAM[registerPC];

            try
            {
                if (byteInstruction == 0xCE)                                                                                // ACI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], (byte)(flagC ? 1 : 0), OPERATOR.ADD);
                    registerPC++;
                    cycles += 7;
                } else if ((byteInstruction >= 0x88) && (byteInstruction <= 0x8F))                                          // ADC
                {
                    num = byteInstruction - 0x88;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, (byte)(flagC ? 1 : 0), OPERATOR.ADD);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0x8E) cycles += 3;
                } else if ((byteInstruction >= 0x80) && (byteInstruction <= 0x87))                                          // ADD
                {
                    num = byteInstruction - 0x80;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, 0, OPERATOR.ADD);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0x86) cycles += 3;
                } else if (byteInstruction == 0xC6)                                                                         // ADI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], 0, OPERATOR.ADD);
                    registerPC++;
                    cycles += 7;
                } else if ((byteInstruction >= 0xA0) && (byteInstruction <= 0xA7))                                          // ANA
                {
                    num = byteInstruction - 0xA0;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, 0, OPERATOR.AND);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0xA6) cycles += 3;
                } else if (byteInstruction == 0xE6)                                                                         // ANI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], 0, OPERATOR.AND);
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0xCD)                                                                         // CALL
                {
                    UInt16 address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (UInt16)(0x0100 * RAM[registerPC]);
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = address;
                    cycles += 18;
                } else if (byteInstruction == 0xDC)                                                                         // CC    
                {
                    if (flagC)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = address;
                        cycles += 18;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    }
                } else if (byteInstruction == 0xFC)                                                                         // CM
                {
                    if (flagS)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = address;
                        cycles += 18;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    }
                } else if (byteInstruction == 0x2F)                                                                         // CMA
                {
                    registerA = (byte)(0xFF - registerA);
                    registerPC++;
                } else if (byteInstruction == 0x3F)                                                                         // CMC
                {
                    flagC = !flagC;
                    registerPC++;
                    cycles += 4;
                } else if ((byteInstruction >= 0xB8) && (byteInstruction <= 0xBF))                                          // CMP  
                {
                    num = byteInstruction - 0xB8;
                    byte compareValue = 0x00;
                    result = GetRegisterValue((byte)num, ref compareValue);
                    if (!result) return ("Can't get the register value");
                    Calculate(registerA, compareValue, 0, OPERATOR.SUB);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0xBE) cycles += 3;
                } else if (byteInstruction == 0xD4)                                                                         // CNC 
                {
                    if (flagC)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = address;
                        cycles += 18;
                    }
                } else if (byteInstruction == 0xC4)                                                                         // CNZ
                {
                    if (flagZ)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC++]);
                        registerPC++;
                        registerA = RAM[address];
                        cycles += 18;
                    }
                } else if (byteInstruction == 0xF4)                                                                         // CP
                {
                    if (flagS)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = address;
                        cycles += 18;
                    }
                } else if (byteInstruction == 0xEC)                                                                         // CPE
                {
                    if (flagP)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = address;
                        cycles += 18;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    }
                } else if (byteInstruction == 0xFE)                                                                         // CPI  
                {
                    registerPC++;
                    Calculate(registerA, RAM[registerPC], 0, OPERATOR.SUB);
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0xE4)                                                                         // CPO
                {
                    if (flagC)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = address;
                        cycles += 18;
                    }
                } else if (byteInstruction == 0xCC)                                                                         // CZ
                {
                    if (!flagZ)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 17;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        registerA = RAM[address];
                        cycles += 18;
                    }
                } else if (byteInstruction == 0x27)                                                                         // DAA 
                {
                    byte low = (byte)(registerA & 0x0F);
                    byte high = (byte)(registerA & 0xF0);
                    if ((low > 0x09) || flagAC)
                    {
                        low += 0x06;
                        if (low > 0x0F)
                        {
                            if (high == 0xF0) flagC = true;
                            high += 0x10;
                            low = (byte)(low & 0x0F);
                        }
                    }
                    if ((high > 0x90) || flagC)
                    {
                        flagC = true;
                        high += 0x60;
                    }
                    registerA = (byte)(high * 0x0100 + low);
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x09)                                                                         // DAD B
                {
                    UInt16 value1 = (UInt16)(0x0100 * registerB + registerC);
                    UInt16 value2 = (UInt16)(0x0100 * registerH + registerL);
                    UInt16 value = Calculate(value1, value2, 0, OPERATOR.ADD);
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x19)                                                                         // DAD D
                {
                    UInt16 value1 = (UInt16)(0x0100 * registerD + registerE);
                    UInt16 value2 = (UInt16)(0x0100 * registerH + registerL);
                    UInt16 value = Calculate(value1, value2, 0, OPERATOR.ADD);
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x29)                                                                         // DAD H
                {
                    UInt16 value1 = (UInt16)(0x0100 * registerH + registerL);
                    UInt16 value2 = (UInt16)(0x0100 * registerH + registerL);
                    UInt16 value = Calculate(value1, value2, 0, OPERATOR.ADD);
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x39)                                                                         // DAD SP
                {
                    UInt16 value1 = registerSP;
                    UInt16 value2 = (UInt16)(0x0100 * registerH + registerL);
                    UInt16 value = Calculate(value1, value2, 0, OPERATOR.ADD);
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x3D)                                                                         // DCR A
                {
                    bool save_flag = flagC;
                    registerA = Calculate(registerA, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x05)                                                                         // DCR B
                {
                    bool save_flag = flagC;
                    registerB = Calculate(registerB, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x0D)                                                                         // DCR C
                {
                    bool save_flag = flagC;
                    registerC = Calculate(registerC, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x15)                                                                         // DCR D
                {
                    bool save_flag = flagC;
                    registerD = Calculate(registerD, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x1D)                                                                         // DCR E
                {
                    bool save_flag = flagC;
                    registerE = Calculate(registerE, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x25)                                                                         // DCR H
                {
                    bool save_flag = flagC;
                    registerH = Calculate(registerH, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x2D)                                                                         // DCR L
                {
                    bool save_flag = flagC;
                    registerL = Calculate(registerL, 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x35)                                                                         // DCR M
                {
                    bool save_flag = flagC;
                    UInt16 address = (UInt16)(0x0100 * registerH + registerL);
                    RAM[address] = Calculate(RAM[address], 0x01, 0, OPERATOR.SUB);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x0B)                                                                         // DCX B
                {
                    int value = (0x0100 * registerB + registerC);
                    if (value == 0x8000) flagK = true;
                    value -= 0x01;
                    Get2ByteFromInt(value, out lo, out hi);
                    registerB = (byte)Convert.ToInt32(hi, 16);
                    registerC = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x1B)                                                                         // DCX D
                {
                    int value = (0x0100 * registerD + registerE);
                    if (value == 0x8000) flagK = true;
                    value -= 0x01;
                    Get2ByteFromInt(value, out lo, out hi);
                    registerD = (byte)Convert.ToInt32(hi, 16);
                    registerE = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x2B)                                                                         // DCX H
                {
                    int value = (0x0100 * registerH + registerL);
                    if (value == 0x8000) flagK = true;
                    value -= 0x01;
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x3B)                                                                         // DCX SP
                {
                    if (registerSP == 0x8000) flagK = true;
                    registerSP -= 0x01;
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0xF3)                                                                         // DI
                {
                    intrIE = false; 
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x76)                                                                         // HLT
                {
                    cycles += 5;
                    return ("System Halted");
                } else if (byteInstruction == 0xFB)                                                                         // EI
                {
                    intrIE = true;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0xDB)                                                                         // IN
                {
                    registerPC++;
                    registerA = PORT[RAM[registerPC]];
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x3C)                                                                         // INR A
                {
                    bool save_flag = flagC;
                    registerA = Calculate(registerA, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x04)                                                                         // INR B
                {
                    bool save_flag = flagC;
                    registerB = Calculate(registerB, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x0C)                                                                         // INR C
                {
                    bool save_flag = flagC;
                    registerC = Calculate(registerC, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x14)                                                                         // INR D
                {
                    bool save_flag = flagC;
                    registerD = Calculate(registerD, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x1C)                                                                         // INR E
                {
                    bool save_flag = flagC;
                    registerE = Calculate(registerE, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x24)                                                                         // INR H
                {
                    bool save_flag = flagC;
                    registerH = Calculate(registerH, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x2C)                                                                         // INR L
                {
                    bool save_flag = flagC;
                    registerL = Calculate(registerL, 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x34)                                                                         // INR M
                {
                    bool save_flag = flagC;
                    UInt16 address = 0;
                    address = (UInt16)(0x0100 * registerH + registerL);
                    RAM[address] = Calculate(RAM[address], 0x01, 0, OPERATOR.ADD);
                    flagC = save_flag;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x03)                                                                         // INX B
                {
                    int value = (0x0100 * registerB + registerC);
                    if (value == 0x7FFF) flagK = true;
                    value += 0x01;
                    Get2ByteFromInt(value, out lo, out hi);
                    registerB = (byte)Convert.ToInt32(hi, 16);
                    registerC = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x13)                                                                         // INX D
                {
                    int value = (0x0100 * registerD + registerE);
                    if (value == 0x7FFF) flagK = true;
                    value += 0x01;
                    Get2ByteFromInt(value, out lo, out hi);
                    registerD = (byte)Convert.ToInt32(hi, 16);
                    registerE = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x23)                                                                         // INX H
                {
                    int value = (0x0100 * registerH + registerL);
                    if (value == 0x7FFF) flagK = true;
                    value += 0x01;
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x33)                                                                         // INX SP
                {
                    if (registerSP == 0x7FFF) flagK = true;
                    registerSP += 0x01;
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0xDA)                                                                         // JC
                {
                    if (flagC)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                } else if (byteInstruction == 0xFA)                                                                         // JM
                {
                    if (flagS)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    }
                } else if (byteInstruction == 0xC3)                                                                         // JMP
                {
                    UInt16 address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (UInt16)(0x0100 * RAM[registerPC]);
                    registerPC = address;
                    cycles += 10;
                } else if (byteInstruction == 0xD2)                                                                         // JNC
                {
                    if (flagC)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    }
                } else if (byteInstruction == 0xC2)                                                                         // JNZ
                {
                    if (flagZ)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    }
                } else if (byteInstruction == 0xF2)                                                                         // JP
                {
                    if (flagS)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    }
                } else if (byteInstruction == 0xEA)                                                                         // JPE
                {
                    if (flagP)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    }
                } else if (byteInstruction == 0xE2)                                                                         // JPO
                {
                    if (flagP)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    }
                } else if (byteInstruction == 0xCA)                                                                         // JZ
                {
                    if (flagZ)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC = address;
                        cycles += 10;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    }
                } else if (byteInstruction == 0x3A)                                                                         // LDA
                {
                    UInt16 address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (UInt16)(0x0100 * RAM[registerPC]);
                    registerPC++;
                    registerA = RAM[address];
                    cycles += 13;
                } else if ((byteInstruction == 0x0A) || (byteInstruction == 0x1A))                                          // LDAX
                {
                    UInt16 address;
                    if (byteInstruction == 0x0A)
                    {
                        address = (UInt16)(registerB * 0x0100 + registerC);
                        registerA = RAM[address];
                    } else if (byteInstruction == 0x1A)
                    {
                        address = (UInt16)(registerD * 0x0100 + registerE);
                        registerA = RAM[address];
                    }
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0x2A)                                                                         // LHLD
                {
                    UInt16 address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (UInt16)(0x0100 * RAM[registerPC]);
                    registerPC++;
                    registerL = RAM[address];
                    address++;
                    registerH = RAM[address];
                    cycles += 16;
                } else if (byteInstruction == 0x01)                                                                         // LXI B
                {
                    registerPC++;
                    registerC = RAM[registerPC];
                    registerPC++;
                    registerB = RAM[registerPC];
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x11)                                                                         // LXI D
                {
                    registerPC++;
                    registerE = RAM[registerPC];
                    registerPC++;
                    registerD = RAM[registerPC];
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x21)                                                                         // LXI H
                {
                    registerPC++;
                    registerL = RAM[registerPC];
                    registerPC++;
                    registerH = RAM[registerPC];
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x31)                                                                         // LXI SP
                {
                    byte b1, b2;
                    registerPC++;
                    b1 = RAM[registerPC];
                    registerPC++;
                    b2 = RAM[registerPC];
                    registerPC++;
                    registerSP = (UInt16)(b1 + (0x0100 * b2));
                    cycles += 10;
                } else if ((byteInstruction >= 0x40) && (byteInstruction <= 0x7F) && (byteInstruction != 0x76))             // MOV
                {
                    if ((byteInstruction == 0x46) ||
                        (byteInstruction == 0x4E) ||
                        (byteInstruction == 0x56) ||
                        (byteInstruction == 0x5E) ||
                        (byteInstruction == 0x66) ||
                        (byteInstruction == 0x6E) ||
                        (byteInstruction == 0x7E))
                    {
                        UInt16 address = (UInt16)(0x0100 * registerH + registerL);
                        val = RAM[address];
                        num = byteInstruction - 0x40;
                        result = SetRegisterValue((byte)((num >> 3) & 0x07), val);
                        if (!result) return ("Can't set the register value");
                        registerPC++;
                        cycles += 7;
                    } else if ((byteInstruction >= 0x70) && (byteInstruction <= 0x77))
                    {
                        num = byteInstruction - 0x40;
                        result = GetRegisterValue((byte)(num & 0x07), ref val);
                        if (!result) return ("Can't get the register value");
                        UInt16 address = (UInt16)(0x0100 * registerH + registerL);
                        RAM[address] = val;
                        registerPC++;
                        cycles += 7;
                    } else
                    {
                        num = byteInstruction - 0x40;
                        result = GetRegisterValue((byte)(num & 0x07), ref val);
                        if (!result) return ("Can't get the register value");
                        result = SetRegisterValue((byte)((num >> 3) & 0x07), val);
                        if (!result) return ("Can't set the register value");
                        registerPC++;
                        cycles += 4;
                    }
                } else if ((byteInstruction == 0x06) ||
                           (byteInstruction == 0x0E) ||
                           (byteInstruction == 0x16) ||
                           (byteInstruction == 0x1E) ||
                           (byteInstruction == 0x26) ||
                           (byteInstruction == 0x2E) ||
                           (byteInstruction == 0x36) ||                                                                       
                           (byteInstruction == 0x3E))                                                                       // MVI

                {
                    if (byteInstruction == 0x36)
                    {
                        UInt16 address = (UInt16)(0x0100 * registerH + registerL);
                        registerPC++;
                        val = RAM[registerPC];
                        RAM[address] = val;
                        registerPC++;
                        cycles += 10;
                    } else
                    {
                        registerPC++;
                        val = RAM[registerPC];
                        num = byteInstruction;
                        result = SetRegisterValue((byte)((num >> 3) & 0x07), val);
                        if (!result) return ("Can't set the register value");
                        registerPC++;
                        cycles += 7;
                    }
                } else if (byteInstruction == 0x00)                                                                         // NOP
                {
                    registerPC++;
                    cycles += 4;
                } else if ((byteInstruction >= 0xB0) && (byteInstruction <= 0xB7))                                          // ORA
                {
                    num = byteInstruction - 0xB0;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, 0, OPERATOR.OR);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0xB6) cycles += 3;
                } else if (byteInstruction == 0xF6)                                                                         // ORI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], 0, OPERATOR.OR);
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0xD3)                                                                         // OUT
                {
                    registerPC++;
                    PORT[RAM[registerPC]] = registerA;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xE9)                                                                         // PCHL
                {
                    registerPC = registerL;
                    registerPC = (UInt16)(registerPC + (registerH * 0x0100));
                    cycles += 6;
                } else if (byteInstruction == 0xC1)                                                                         // POP B
                {
                    registerC = RAM[registerSP];
                    registerSP++;
                    registerB = RAM[registerSP];
                    registerSP++;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xD1)                                                                         // POP D
                {
                    registerE = RAM[registerSP];
                    registerSP++;
                    registerD = RAM[registerSP];
                    registerSP++;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xE1)                                                                         // POP H
                {
                    registerL = RAM[registerSP];
                    registerSP++;
                    registerH = RAM[registerSP];
                    registerSP++;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xF1)                                                                         // POP PSW 
                {
                    byte flags, b;
                    flags = RAM[registerSP];
                    registerSP++;
                    registerA = RAM[registerSP];
                    registerSP++;
                    b = (byte)(flags & 0x01);
                    if (b != 0) flagC = true; else flagC = false;
                    b = (byte)(flags & 0x04);
                    if (b != 0) flagP = true; else flagP = false;
                    b = (byte)(flags & 0x10);
                    if (b != 0) flagAC = true; else flagAC = false;
                    b = (byte)(flags & 0x40);
                    if (b != 0) flagZ = true; else flagZ = false;
                    b = (byte)(flags & 0x80);
                    if (b != 0) flagS = true; else flagS = false;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xC5)                                                                         // PUSH B
                {
                    registerSP--;
                    RAM[registerSP] = registerB;
                    registerSP--;
                    RAM[registerSP] = registerC;
                    registerPC++;
                    cycles += 12;
                } else if (byteInstruction == 0xD5)                                                                         // PUSH D
                {
                    registerSP--;
                    RAM[registerSP] = registerD;
                    registerSP--;
                    RAM[registerSP] = registerE;
                    registerPC++;
                    cycles += 12;
                } else if (byteInstruction == 0xE5)                                                                         // PUSH H
                {
                    registerSP--;
                    RAM[registerSP] = registerH;
                    registerSP--;
                    RAM[registerSP] = registerL;
                    registerPC++;
                    cycles += 12;
                } else if (byteInstruction == 0xF5)                                                                         // PUSH PSW 
                {
                    byte aflag = 00;
                    if (flagS) aflag += 0x80;
                    if (flagZ) aflag += 0x40;
                    if (flagAC) aflag += 0x10;
                    if (flagP) aflag += 0x04;
                    if (flagC) aflag += 0x01;
                    registerSP--;
                    RAM[registerSP] = registerA;
                    registerSP--;
                    RAM[registerSP] = aflag;
                    registerPC++;
                    cycles += 12;
                } else if (byteInstruction == 0x17)                                                                         // RAL 
                {
                    byte prevA;
                    byte ac = registerA;
                    byte saveC;
                    if (flagC)
                    {
                        saveC = 1;
                    } else
                    {
                        saveC = 0;
                    }
                    prevA = ac;
                    ac *= 2;
                    if (ac < prevA)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }
                    ac += saveC;
                    registerA = ac;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x1F)                                                                         // RAR   
                {
                    byte ac = registerA;
                    byte saveC;
                    if (flagC)
                    {
                        saveC = 1;
                    } else
                    {
                        saveC = 0;
                    }
                    if ((ac & 0x01) == 0x01)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }
                    ac /= 2;
                    ac += (byte)(saveC * 0x80);
                    registerA = ac;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0xD8)                                                                         // RC
                {
                    if (flagC)
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    } else
                    {
                        registerPC++;
                        cycles += 11;
                    }
                } else if (byteInstruction == 0xC9)                                                                         // RET
                {
                    UInt16 address;
                    address = RAM[registerSP];
                    registerSP++;
                    address += (UInt16)(RAM[registerSP] * 0x0100);
                    registerSP++;
                    registerPC = address;
                    cycles += 10;
                } else if (byteInstruction == 0x20)                                                                         // RIM
                {
                    registerA = 0x00;
                    registerA += intrM55 ? (byte)0x01 : (byte)0x00;
                    registerA += intrM65 ? (byte)0x02 : (byte)0x00;
                    registerA += intrM75 ? (byte)0x04 : (byte)0x00;
                    registerA += intrIE  ? (byte)0x08 : (byte)0x00;
                    registerA += intrP55 ? (byte)0x10 : (byte)0x00;
                    registerA += intrP65 ? (byte)0x20 : (byte)0x00;
                    registerA += intrP75 ? (byte)0x40 : (byte)0x00;
                    registerA += sid ? (byte)0x80 : (byte)0x00;
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0x07)                                                                         // RLC
                {
                    flagC = (registerA & 0x80) != 0 ? true : false;
                    registerA = (byte)(registerA << 1);
                    if (flagC) registerA = (byte)(registerA | 0x01);
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0xF8)                                                                         // RM
                {
                    if (flagS)
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    } else
                    {
                        registerPC++;
                        cycles += 11;
                    }
                } else if (byteInstruction == 0xD0)                                                                         // RNC
                {
                    if (flagC)
                    {
                        registerPC++;
                        cycles += 11;
                    } else
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    }
                } else if (byteInstruction == 0xC0)                                                                         // RNZ
                {
                    if (flagZ)
                    {
                        registerPC++;
                        cycles += 11;
                    } else
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    }
                } else if (byteInstruction == 0xF0)                                                                         // RP
                {
                    if (flagS)
                    {
                        registerPC++;
                        cycles += 11;
                    } else
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    }
                } else if (byteInstruction == 0xE8)                                                                         // RPE
                {
                    if (flagP)
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    } else
                    {
                        registerPC++;
                        cycles += 11;
                    }
                } else if (byteInstruction == 0xE0)                                                                         // RPO
                {
                    if (flagP)
                    {
                        registerPC++;
                        cycles += 11;
                    } else
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    }
                } else if (byteInstruction == 0x0F)                                                                         // RRC
                {
                    flagC = (registerA & 0x01) != 0 ? true : false;
                    registerA = (byte)(registerA >> 1);
                    if (flagC) registerA = (byte)(registerA | 0x80);
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0xC7)                                                                         // RST 0
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0000;
                    cycles += 12;
                } else if (byteInstruction == 0xCF)                                                                         // RST 1
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0008;
                    cycles += 12;
                } else if (byteInstruction == 0xD7)                                                                         // RST 2
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0010;
                    cycles += 12;
                } else if (byteInstruction == 0xDF)                                                                         // RST 3
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0018;
                    cycles += 12;
                } else if (byteInstruction == 0xE7)                                                                         // RST 4
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0020;
                    cycles += 12;
                } else if (byteInstruction == 0xEF)                                                                         // RST 5
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0028;
                    cycles += 12;
                } else if (byteInstruction == 0xF7)                                                                         // RST 6
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0030;
                    cycles += 12;
                } else if (byteInstruction == 0xFF)                                                                         // RST 7
                {
                    registerPC++;
                    Get2ByteFromInt(registerPC, out lo, out hi);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(hi, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(lo, 16);
                    registerPC = 0x0038;
                    cycles += 12;
                } else if (byteInstruction == 0xC8)                                                                         // RZ
                {
                    if (flagZ)
                    {
                        UInt16 address;
                        address = RAM[registerSP];
                        registerSP++;
                        address += (UInt16)(RAM[registerSP] * 0x0100);
                        registerSP++;
                        registerPC = address;
                        cycles += 12;
                    } else
                    {
                        registerPC++;
                        cycles += 11;
                    }
                } else if ((byteInstruction >= 0x98) && (byteInstruction <= 0x9F))                                          // SBB
                {
                    num = byteInstruction - 0x98;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, (byte)(flagC ? 1 : 0), OPERATOR.SUB);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0x9E) cycles += 3;
                } else if (byteInstruction == 0xDE)                                                                         // SBI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], (byte)(flagC ? 1 : 0), OPERATOR.SUB);
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0x22)                                                                         // SHLD
                {
                    UInt16 address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (UInt16)(0x0100 * RAM[registerPC]);
                    registerPC++;
                    RAM[address] = registerL;
                    address++;
                    RAM[address] = registerH;
                    cycles += 16;
                } else if (byteInstruction == 0x30)                                                                         // SIM
                {
                    if ((registerA & 0x08) == 0x08)
                    {
                        intrM55 = (registerA & 0x01) == 0x01 ? true : false;
                        intrM65 = (registerA & 0x02) == 0x02 ? true : false;
                        intrM75 = (registerA & 0x04) == 0x04 ? true : false;
                    }
                    if ((registerA & 0x40) == 0x40)
                    {
                        sod = (registerA & 0x80) == 0x80 ? true : false;
                    }
                    registerPC++;
                    cycles += 4;
                } else if (byteInstruction == 0xF9)                                                                         // SPHL
                {
                    registerSP = registerL;
                    registerSP = (UInt16)(registerSP + (0x0100 * registerH));
                    registerPC++;
                    cycles += 6;
                } else if (byteInstruction == 0x32)                                                                         // STA
                {
                    UInt16 address = 0;
                    registerPC++;
                    address = RAM[registerPC];
                    registerPC++;
                    address += (UInt16)(0x0100 * RAM[registerPC]);
                    RAM[address] = registerA;
                    registerPC++;
                    if (address == 0x1800) writeToDisplay = true;
                    cycles += 13;
                } else if (byteInstruction == 0x02)                                                                         // STAX B
                {
                    UInt16 address;
                    address = registerC;
                    address = (UInt16)(address + (0x0100 * registerB));
                    RAM[address] = registerA;
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0x12)                                                                         // STAX D
                {
                    UInt16 address;
                    address = registerE;
                    address = (UInt16)(address + (0x0100 * registerD));
                    RAM[address] = registerA;
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0x37)                                                                         // STC
                {
                    flagC = true;
                    registerPC++;
                    cycles += 4;
                } else if ((byteInstruction >= 0x90) && (byteInstruction <= 0x97))                                          // SUB
                {
                    num = byteInstruction - 0x90;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, 0, OPERATOR.SUB);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0x96) cycles += 3;
                } else if (byteInstruction == 0xD6)                                                                         // SUI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], 0, OPERATOR.SUB);
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0xEB)                                                                         // XCHG
                {
                    byte temp;
                    temp = registerD;
                    registerD = registerH;
                    registerH = temp;
                    temp = registerL;
                    registerL = registerE;
                    registerE = temp;
                    registerPC++;
                    cycles += 4;
                } else if ((byteInstruction >= 0xA8) && (byteInstruction <= 0xAF))                                          // XRA
                {
                    num = byteInstruction - 0xA8;
                    result = GetRegisterValue((byte)num, ref val);
                    if (!result) return ("Can't get the register value");
                    registerA = Calculate(registerA, val, 0, OPERATOR.XOR);
                    registerPC++;
                    cycles += 4;
                    if (byteInstruction == 0xAE) cycles += 3;
                } else if (byteInstruction == 0xEE)                                                                         // XRI
                {
                    registerPC++;
                    registerA = Calculate(registerA, RAM[registerPC], 0, OPERATOR.XOR);
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0xE3)                                                                         // XTHL
                {
                    byte t1, t2;
                    t1 = registerL;
                    t2 = registerH;
                    registerL = RAM[registerSP];
                    RAM[registerSP] = t1;
                    registerH = RAM[registerSP + 1];
                    RAM[registerSP + 1] = t2;
                    registerPC++;
                    cycles += 16;
                } else if (byteInstruction == 0x10)                                                                         // ARHL (UNDOCUMENTED)
                {
                    byte h = registerH;
                    byte l = registerL;
                    byte saveC;
                    if (flagC)
                    {
                        saveC = 1;
                    } else
                    {
                        saveC = 0;
                    }
                    if ((l & 0x01) == 0x01)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }
                    l /= 2;
                    if ((h & 0x01) == 0x01) l += 0x80;
                    h /= 2;
                    h += (byte)(saveC * 0x80);
                    registerH = h;
                    registerL = l;
                    registerPC++;
                    cycles += 7;
                } else if (byteInstruction == 0x08)                                                                         // DSUB (UNDOCUMENTED)
                {
                    UInt16 value1 = (UInt16)(0x0100 * registerB + registerC);
                    UInt16 value2 = (UInt16)(0x0100 * registerH + registerL);
                    UInt16 value = Calculate(value2, value1, 0, OPERATOR.SUB);
                    Get2ByteFromInt(value, out lo, out hi);
                    registerH = (byte)Convert.ToInt32(hi, 16);
                    registerL = (byte)Convert.ToInt32(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xFD)                                                                         // JK/JX5 (UNDOCUMENTED)
                {
                    if (flagK)
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        registerPC = address;
                        cycles += 10;
                    } else
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    }
                } else if (byteInstruction == 0xDD)                                                                         // JNK/JNX5 (UNDOCUMENTED)
                {
                    if (flagK)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                        cycles += 7;
                    } else
                    {
                        UInt16 address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (UInt16)(0x0100 * RAM[registerPC]);
                        registerPC++;
                        registerPC = address;
                        cycles += 10;
                    }
                } else if (byteInstruction == 0x28)                                                                         // LDHI (UNDOCUMENTED)
                {
                    registerPC++;
                    num = RAM[registerPC];
                    num += registerD * 0x100 + registerE;
                    Get2ByteFromInt(num, out lo, out hi);
                    registerH = Convert.ToByte(hi, 16);
                    registerL = Convert.ToByte(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x38)                                                                         // LDSI (UNDOCUMENTED)
                {
                    num = registerSP;
                    registerPC++;
                    num += RAM[registerPC]; 
                    Get2ByteFromInt(num, out lo, out hi);
                    registerD = Convert.ToByte(hi, 16);
                    registerE = Convert.ToByte(lo, 16);
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xED)                                                                         // LHLX (UNDOCUMENTED)
                {
                    UInt16 address = 0;
                    address += registerE;
                    address += (UInt16)(0x0100 * registerD);
                    registerL = RAM[address];
                    address++;
                    registerH = RAM[address];
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0x18)                                                                         // RDEL (UNDOCUMENTED)
                {
                    byte d = registerD;
                    byte e = registerE;
                    byte saveC;
                    if (flagC)
                    {
                        saveC = 1;
                    } else
                    {
                        saveC = 0;
                    }
                    if ((d & 0x08) == 0x08)
                    {
                        flagC = true;
                    } else
                    {
                        flagC = false;
                    }
                    d /= 2;
                    e /= 2;
                    e += (byte)(saveC * 0x01);
                    registerD = d;
                    registerE = e;
                    registerPC++;
                    cycles += 10;
                } else if (byteInstruction == 0xCB)                                                                         // RSTV (UNDOCUMENTED)
                {
                    if (flagV)
                    {
                        Get2ByteFromInt(registerPC, out lo, out hi);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(hi, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(lo, 16);
                        registerPC = 0x0040;
                        cycles += 12;
                    } else
                    {
                        registerPC++;
                        cycles += 6;
                    }
                } else if (byteInstruction == 0xD9)                                                                         // SHLX (UNDOCUMENTED)
                {
                    UInt16 address = 0;
                    address = (UInt16)(registerD * 0x100);
                    address += registerE;
                    RAM[address] = registerL;
                    address++;
                    RAM[address] = registerH;
                    registerPC++;
                    cycles += 10;
                } else
                {
                    return ("Unknown instruction '" + byteInstruction.ToString("X2") + "'");
                }
            } catch (Exception exception)
            {
                return ("Exception at memory location: " + registerPC.ToString("X") + ":\r\n" + exception.Message);
            }

            if (cycles > (UInt64.MaxValue - 20)) cycles = 0;

            nextAddress = registerPC;
            return "";
        }

        #endregion
    }
}
