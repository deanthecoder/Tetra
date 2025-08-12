// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Text.RegularExpressions;

namespace DTC.Preprocessor;

public class Preprocessor
{
    public string Preprocess(string code)
    {
        var lines = code.Split(Environment.NewLine);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var isDefine = line?.TrimStart().Contains("#define") == true;
            if (!isDefine)
                continue;
            
            // Remove comments
            line = Regex.Replace(line, @"\s*//.*|\s*/\*.*", string.Empty);

            // Handle defined values. (#define FOO 123)
            var match = Regex.Match(line, @"\#define\s+([\w\d_]+)\s+(.+)\s*");
            if (match.Success)
            {
                // Remove the #define line.
                lines[i] = null;

                // Inline the definition.
                var term = match.Groups[1].Value;
                var replacement = match.Groups[2].Value;
                for (var j = i + 1; j < lines.Length; j++)
                    lines[j] = Regex.Replace(lines[j]!, $@"\b{Regex.Escape(term)}\b", replacement);
            }
            
            // Handle simple macros. (#define name(x) rhs(x + 1))
            match = Regex.Match(line, @"\#define\s+([\w\d_]+)\((.+)\)\s+(.+)\s*");
            if (match.Success)
            {
                // Remove the #define line.
                lines[i] = null;

                // Inline the definition.
                var name = match.Groups[1].Value;
                var x = match.Groups[2].Value;
                var rhs = match.Groups[3].Value;
                var functionPattern = $@"\b{name}\(.*\)";
                for (var j = i + 1; j < lines.Length; j++)
                {
                    // Find each instance of the macro call.
                    var finished = false;
                    while (!finished)
                    {
                        finished = true;
                        
                        var matches = Regex.Matches(lines[j]!, functionPattern);
                        foreach (var m in matches.Reverse())
                        {
                            var found = lines[j]!.Substring(m.Index + name.Length + 1, m.Length - name.Length - 1);
                            var bracketDepth = 1;
                            var charsToTake = 0;
                            while (charsToTake < found.Length)
                            {
                                if (found[charsToTake] == '(')
                                    bracketDepth++;
                                else if (found[charsToTake] == ')')
                                    bracketDepth--;
                            
                                if (bracketDepth == 0)
                                    break;
                            
                                charsToTake++;
                            }

                            if (charsToTake == found.Length)
                                break;
                            
                            found = found[..charsToTake];
                        
                            var expr = rhs.Replace(x, found);
                            lines[j] = lines[j]!.Replace($"{name}({found})", expr);
                            
                            finished = false;
                        }
                    }
                }
            }
        }

        return string.Join(Environment.NewLine, lines.Where(o => o != null));
    }
}