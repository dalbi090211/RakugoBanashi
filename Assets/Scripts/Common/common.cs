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
    const int newLineLimit = 30;
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

    private static int GetCharWeight(char c)
    {
        // 한국어, 한자 등 전각 문자 → 2
        if (c >= 0xAC00 && c <= 0xD7A3) return 2; // 한글 음절
        if (c >= 0x3000 && c <= 0x9FFF) return 2; // CJK / 전각기호

        // 작은 문자 (. , ! ? : ;) → 1
        if (".,!?:;".IndexOf(c) >= 0) return 1;

        // 일반 ASCII → 1
        return 1;
    }

    public static void AddNewLine(ref string text)
    {
        int curCount = 0;
        var result = new System.Text.StringBuilder();

        foreach (char c in text)
        {
            // \n을 만나면 카운트 초기화
            if (c == '\n')
            {
                curCount = 0;
                result.Append(c);
                continue;
            }

            int weight = GetCharWeight(c);

            // 한도 초과 시 \n 삽입 후 초기화
            if (curCount + weight > newLineLimit)
            {
                result.Append('\n');
                curCount = 0;
            }

            result.Append(c);
            curCount += weight;
        }

        text = result.ToString();
    }
}