using System.Collections;
using System.Text.RegularExpressions;

namespace DBF.Helpers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// *         Matcher 0+ tegn (som i VB)
/// ?         Matcher 1 tegn (som i VB)
/// **        Matcher rekursivt på tværs af path‑segmenter
///           Eksempel:
///             src/**/test/*.cs
/// {a,b,c}   Alternativer
///           Eksempel:
///             *.{jpg,png,gif}
/// [abc] og 
/// [a-z]    Tegnklasser
///          Eksempel:
///            file[0-9].txt
/// !pattern Matcher alt undtagen mønsteret
///          Eksempel:
///            !*.tmp
/// </summary>
public sealed class LikeFilterCollection : IEnumerable<string>
{
    private readonly List<string>                      _filters  = new();
    private readonly List<(Regex regex, bool negated)> _compiled = new();

    public void Add(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return;

        pattern = pattern.Trim();

        if (_filters.Contains(pattern, StringComparer.OrdinalIgnoreCase))
            return;

        _filters.Add(pattern);
        _compiled.Add(CompilePattern(pattern));
    }

    public bool Matches(string input)
    {
        if (input == null)
            return false;

        bool matched = false;

        foreach (var (regex, negated) in _compiled)
        {
            bool isMatch = regex.IsMatch(input);

            if (negated)
            {
                if (isMatch)
                    return false; // negation wins immediately
            }
            else
            {
                if (isMatch)
                    matched = true;
            }
        }

        return matched;
    }

    private static (Regex regex, bool negated) CompilePattern(string pattern)
    {
        bool negated = pattern.StartsWith("!");

        if (negated)
            pattern = pattern.Substring(1);

        string regex = "^" + GlobToRegex(pattern) + "$";

        return (new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled), negated);
    }

    private static string GlobToRegex(string pattern)
    {
        var  sb      = new System.Text.StringBuilder();
        bool inGroup = false;

        for (int i = 0; i <  pattern.Length; i++)
        {
            char c = pattern[i];

            switch (c)
            {
                case '*':
                    if (i + 1 <  pattern.Length && pattern[i + 1] == '*')
                    {
                        sb.Append(".*");
                        i++;
                    }
                    else
                        sb.Append("[^/]*");
                    break;

                case '?':
                    sb.Append(".");
                    break;

                case '{':
                    sb.Append("(?:");
                    inGroup = true;
                    break;

                case '}':
                    sb.Append(")");
                    inGroup = false;
                    break;

                case ',':
                    sb.Append(inGroup ? "|" : ",");
                    break;

                case '[':
                    sb.Append("[");
                    break;

                case ']':
                    sb.Append("]");
                    break;

                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        return sb.ToString();
    }

    public   IEnumerator<string> GetEnumerator() => _filters.GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
