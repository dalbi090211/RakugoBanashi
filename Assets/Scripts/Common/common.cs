using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using FMOD;

public enum escapeType
{
    csv,
    comma
}

public class Common
{
    static char[] commaEscapeStr = { ',', '.', '!', '?' };     // static안쓸수는 없나?
    public const int skipNum = 999;

    public static string rich2normal(string richText)
    {
        return Regex.Replace(richText, "<.*?>", "");
    }

    public static (string, int[]) removeTag(string text, string pattern)
    {
        int realIndex = 0;
        List<int> tagIndex = new List<int>();

        for (int i = 0; i < text.Length; i++)
        {
            if (i + pattern.Length <= text.Length && text[i] == '<')
            {
                int endIndex = text.IndexOf('>', i);
                if (text.Substring(i, pattern.Length) == pattern)
                {
                    tagIndex.Add(realIndex);
                }
                i = endIndex;
            }
            else
            {
                realIndex++;
            }
        }
        return (Regex.Replace(text, pattern, ""), tagIndex.ToArray());
    }

    public static (string, TextRange[]) removeTag(string text, string patternStart, string patternEnd)
    {
        int realIndex = 0;
        List<TextRange> tagIndex = new List<TextRange>();
        TextRange curPos = new TextRange(-1, -1);

        for (int i = 0; i < text.Length; i++)
        {
            if (i + patternEnd.Length <= text.Length && text[i] == '<')
            {
                int endIndex = text.IndexOf('>', i);
                if (text.Substring(i, patternStart.Length) == patternStart)
                {
                    curPos.startIndex = realIndex;
                }
                else if (text.Substring(i, patternEnd.Length) == patternEnd)
                {
                    curPos.endIndex = realIndex;
                    tagIndex.Add(curPos);
                }
                i = endIndex;
            }
            else
            {
                realIndex++;
            }
        }
        return (Regex.Replace(Regex.Replace(text, patternStart, ""), patternEnd, ""), tagIndex.ToArray());
    }

    public static Queue<int> nextEscape(string text, escapeType type)
    {
        string origin = text;
        Queue<int> curIndex = new Queue<int>();
        int index = 0;

        switch (type)
        {
            case escapeType.comma:
                while (index < text.Length)
                {
                    for (int i = 0; i < commaEscapeStr.Length; i++)
                    {
                        if (text[index] == commaEscapeStr[i])
                        {
                            curIndex.Enqueue(index);
                            i = skipNum;
                        }
                    }
                    index++;
                }
                break;

            case escapeType.csv:
                // while(csvEscapeStr.Any(text.Contains)){

                // }
                break;
        }
        return curIndex;
    }
}