using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8085
{
    internal static class Calculator
    {
        /// <summary>
        /// Calculates the value of a mathematical expression
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte CalculateByte(string str, out string result)
        {
            try
            {
                Queue<string> queue = CreateRPN(str);

                int res = ParseRPN(queue);

                result = "OK";
                return (byte)res;
            } catch (Exception ex)
            {
                result = ex.Message;
                return 0;
            }
        }

        public static UInt16 Calculate2Bytes(string str, out string result)
        {
            try
            {
                Queue<string> queue = CreateRPN(str);

                int res = ParseRPN(queue);

                result = "OK";
                return (UInt16)res;
            } catch (Exception ex)
            {
                result = ex.Message;
                return 0;
            }
        }

        /// <summary>
        /// Generate suffix expression
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Queue<string> CreateRPN(string str)
        {
            // Stack of temporary storage + - * / (symbols)
            Stack<char> stack = new Stack<char>();

            // A queue that stores suffix expressions
            Queue<string> queue = new Queue<string>();

            // Convert to lowercase, replace tabs with spaces
            str = str.Replace('\t', ' ').ToLower();

            // Convert hex values to decimal
            while (str.Contains("0x"))
            {
                int start = str.IndexOf("0x");
                int end = 0;
                for (int i=start+2; i<str.Length; i++)
                {
                    if (i == str.Length - 1) start = i;
                    if (str[i] == ' ') { end = i; break; }
                    if (str[i] == '+') { end = i; break; }
                    if (str[i] == '-') { end = i; break; }
                    if (str[i] == '*') { end = i; break; }
                    if (str[i] == '/') { end = i; break; }
                    if (str[i] == '&') { end = i; break; }
                    if (str[i] == '|') { end = i; break; }
                }

                if (end > start)
                {
                    string hex = str.Substring(start, end - start);
                    string dec = Convert.ToInt32(hex, 16).ToString();

                    str = str.Replace("0x" + hex, dec);
                } else
                {
                    Exception ex = new Exception("Error in hexadecimal value in: " + str);
                    throw (ex);
                }
            }

            while (str.Contains("h"))
            {
                int end = str.IndexOf("h");
                int start = str.Length -1;
                for (int i = end - 1; i >= 0; i--)
                {
                    if (i == 0) start = i;
                    if (str[i] == ' ') { start = i + 1; break; }
                    if (str[i] == '+') { start = i + 1; break; }
                    if (str[i] == '-') { start = i + 1; break; }
                    if (str[i] == '*') { start = i + 1; break; }
                    if (str[i] == '/') { start = i + 1; break; }
                    if (str[i] == '&') { start = i + 1; break; }
                    if (str[i] == '|') { start = i + 1; break; }
                }

                if (end > start)
                {
                    string hex = str.Substring(start, end - start);
                    string dec = Convert.ToInt32(hex, 16).ToString();

                    str = str.Replace(hex + "h", dec);
                } else
                {
                    Exception ex = new Exception("Error in hexadecimal value in: " + str);
                    throw (ex);
                }
            }

            for (int i = 0; i < str.Length;)
            {
                // If it is a space, skip 
                if (str[i] == ' ')
                {
                    i++;
                    continue;
                } else if ((str[i] >= '0') && (str[i] <= '9'))
                {
                    // Current digit
                    int cur = 0;

                    // Check value
                    while (i < str.Length && ((str[i] >= '0') && (str[i] <= '9')))
                    {
                        cur = cur * 10 + str[i] - '0';
                        i++;
                    }

                    queue.Enqueue(cur.ToString());
                } else if (str[i] == ')')
                {
                    // If it is ')', you need to pop up the operation symbol in the stack and add it to the queue of suffix expression
                    // Until '(' in the symbol stack is encountered
                    while (stack.Count != 0 && stack.Peek() != '(')
                    {
                        queue.Enqueue(stack.Pop() + "");
                    }

                    stack.Pop();
                    i++;
                } else
                {
                    // It may be + - * / & | these symbols or left parentheses
                    // At this time, you need to determine the priority of the top element of the symbol stack and the currently traversed character
                    while (stack.Count != 0 && Compare(stack.Peek(), str[i]) < 0)
                    {
                        queue.Enqueue(stack.Pop() + "");
                    }

                    stack.Push(str[i]);
                    i++;
                }
            } while (stack.Count != 0)
            {
                queue.Enqueue(stack.Pop() + "");
            }

            return queue;
        }

        /// <summary>
        /// Processing symbol priority
        /// </summary>
        /// <param name="peek"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int Compare(char peek, char c)
        {
            if (peek == '(' || c == '(') return 1;
            if (c == '+' || c == '-') return -1;
            if (c == '&' || c == '|') return -1;
            if (c == '*' && (peek == '*' || peek == '/')) return -1;
            if (c == '/' && (peek == '*' || peek == '/')) return -1;
            return 1;
        }

        /// <summary>
        /// Resolve suffix expression
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        private static int ParseRPN(Queue<string> queue)
        {
            // Result stack
            Stack<int> res = new Stack<int>();

            while (queue.Count != 0)
            {
                String t = queue.Dequeue();
                if (t.Equals("+") || t.Equals("-") || t.Equals("*") || t.Equals("/") || t.Equals("&") || t.Equals("|"))
                {
                    int a = res.Pop();
                    int b = res.Pop();
                    int result = Calculate(b, a, t);
                    res.Push(result);
                } else
                {
                    res.Push(int.Parse(t));
                }
            }

            return res.Pop();
        }

        /// <summary>
        /// Basic arithmetic unit
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static int Calculate(int a, int b, String t)
        {
            // Calculate
            if (t.Equals("+"))
            {
                return a + b;
            } else if (t.Equals("-"))
            {
                return a - b;
            } else if (t.Equals("*"))
            {
                return a * b;
            } else if (t.Equals("/"))
            {
                return a / b;
            } else if (t.Equals("&"))
            {
                return a & b;
            } else if (t.Equals("|"))
            {
                return a | b;
            }

            return 0;
        }
    }
}
