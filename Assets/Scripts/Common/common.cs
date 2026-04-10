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

    // <delay=0.3f> → (정제된 텍스트, Dictionary<index, ms>)
    public static (string, Dictionary<int, float>) removeParamTag(string text, string tagName)
    {
        var result = new Dictionary<int, float>();
        // <delay=0.3f> or <delay=300> 둘 다 허용
        string pattern = $@"<{tagName}=(\d+\.?\d*)(f?)>";

        int offset = 0;
        var matches = Regex.Matches(text, pattern);

        foreach (Match match in matches)
        {
            float value = float.Parse(match.Groups[1].Value);
            // f 붙어있으면 초 단위 → ms 변환, 없으면 ms로 직접 사용
            bool isSeconds = match.Groups[2].Value == "f" || value < 10f;
            float ms = isSeconds ? value * 1000f : value;

            int charIndex = match.Index - offset;
            if (result.ContainsKey(charIndex)) result[charIndex] += ms;
            else result[charIndex] = ms;

            offset += match.Length;
        }

        text = Regex.Replace(text, pattern, "");
        return (text, result);
    }

    public static (string, SpeedRange[]) removeParamRangeTag(string text, string tagName)
    {
        var ranges = new List<SpeedRange>();
        var sb = new System.Text.StringBuilder();
        var tagStack = new Stack<(int sbIndex, float multiplier)>();

        string openPattern = $@"^<{tagName}=(\d+\.?\d*)(f?)>";
        string closeTag = $"</{tagName}>";

        int i = 0;
        while (i < text.Length)
        {
            // 오프닝 태그 체크
            var openMatch = Regex.Match(text.Substring(i), openPattern);
            if (openMatch.Success)
            {
                float value = float.Parse(openMatch.Groups[1].Value,
                                    System.Globalization.CultureInfo.InvariantCulture);
                float multiplier = openMatch.Groups[2].Value == "f" || value < 10f
                                ? value : value / 1000f;

                tagStack.Push((sb.Length, multiplier));
                i += openMatch.Length;
                continue;
            }

            // 클로징 태그 체크
            if (text.Substring(i).StartsWith(closeTag))
            {
                if (tagStack.Count > 0)
                {
                    var (startIdx, multiplier) = tagStack.Pop();
                    ranges.Add(new SpeedRange
                    {
                        start = startIdx,
                        end = sb.Length - 1,
                        multiplier = multiplier
                    });
                }
                i += closeTag.Length;
                continue;
            }

            sb.Append(text[i]);
            i++;
        }

        return (sb.ToString(), ranges.ToArray());
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