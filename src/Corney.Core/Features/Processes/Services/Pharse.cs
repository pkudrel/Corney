﻿using System;
using System.Collections;
using System.Text;
using Corney.Core.Features.Cron.Models;

namespace Corney.Core.Features.Processes.Services
{
    public class Pharse
    {
        public static string Tokenize(ref string source, char[] separators, char[] commentchars, bool escape,
            bool quote)
        {
            if (source == null)
                return null;


            var result = new StringBuilder();
            var max = source.Length - 1;
            var inQuote = false;

            int i;
            for (i = 0; i <= max; i++)
            {
                var last = i == max;
                var c = source[i];

                if (c == '"' && quote)
                {
                    if (!inQuote)
                    {
                        inQuote = true; // Not currently in quote, found quote, start handling quoted strings
                    }
                    else
                    {
                        // Currently in quote
                        if (!last && source[i + 1] == '"')
                            i++; // Double-quote, skip one
                        else
                            inQuote = false; // Just a normal quote, turn off quote processing
                    }
                }
                else if (c == '\\' && !last && escape)
                {
                    c = source[++i]; // Found escape character, step forward one
                    if (c == 'x') // Found hex sequence
                    {
                        if (i > max - 2) // Not enough room, exit
                            break;

                        var hex = source[i + 1].ToString() + source[i + 2]; // Decode hex
                        c = (char)Convert.ToByte(hex, 16);
                        i += 2;
                    }
                    else if (c == '0')
                    {
                        c = '\0'; // Found NULL byte
                    }
                    else if (c == 'n')
                    {
                        c = '\n'; // Found New Line
                    }
                    else if (c == 'r')
                    {
                        c = '\r'; // Found Carriage Return
                    }
                    else if (c == 't')
                    {
                        c = '\t'; // Found Tab
                    }
                }
                else if (!inQuote && ((IList)separators).Contains(c))
                {
                    source = source.SafeSubstring(i + 1); // Found separator outside of quote, cut and return
                    return result.ToString();
                }
                else if (!inQuote && ((IList)commentchars).Contains(c))
                {
                    break; // Found comment, break
                }

                // Normal character, copy
                result.Append(c);
            }

            // This is the end... beautiful friend, the end
            source = null;

            return result.ToString();
        }

        public static ExecuteItem Tokenize(string source)
        {
            var res = new ExecuteItem();
            var whitespace = new[] { ' ', '\t', '\r', '\n' };

            if (source == null)
                return res;

            var command = new StringBuilder();
            var max = source.Length - 1;
            var inQuote = false;

            int i;
            for (i = 0; i <= max; i++)
            {
                var last = i == max;
                var c = source[i];

                if (c == '"')
                {
                    if (!inQuote)
                    {
                        inQuote = true;
                    }
                    else
                    {
                        if (!last && source[i + 1] == '"')
                            i++;
                        else
                            inQuote = false;
                    }
                }

                else if (!inQuote && ((IList)whitespace).Contains(c))
                {
                    source = source.SafeSubstring(i + 1);
                    res.Program = command.ToString();
                    res.Arguments = source;
                    return res;
                    ;
                }
                else if (!inQuote && ((IList)whitespace).Contains(c))
                {
                    break;
                }

                // Normal character, copy
                command.Append(c);
            }


            res.Program = command.ToString();
            res.Arguments = source;
            return res;
        }

        public static object Tokenize(ref string source, char[] whitespace, bool commentchars, bool escape)
        {
            throw new NotImplementedException();
        }
    }
}