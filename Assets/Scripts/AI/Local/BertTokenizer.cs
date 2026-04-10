using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

public static class CharExtensions
{
    public static bool IsBertWhiteSpace(this char c)
    {
        return char.IsWhiteSpace(c);
    }

    public static bool IsBertControl(this char c)
    {
        // \t, \n, \r 제외한 control 문자 제거
        if (c == '\t' || c == '\n' || c == '\r')
            return false;

        return char.IsControl(c);
    }

    public static bool IsBertShouldBeRemoved(this char c)
    {
        // 일반적으로 null 문자 제거
        return c == '\0' || c == '\uFFFD';
    }

    public static bool IsBertPunctuation(this char c)
    {
        return char.IsPunctuation(c);
    }
}

public class BertTokenizer
{


    private Dictionary<string, int> _vocabulary = new Dictionary<string, int>();

    // 1. vocab.txt 파일을 읽어 딕셔너리로 만듭니다.
    public void LoadVocabulary(TextAsset vocabAsset)
    {
        _vocabulary.Clear();
        string[] lines = vocabAsset.text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            _vocabulary[lines[i].Trim()] = i;
        }
        Debug.Log($"보카 로드 완료: {_vocabulary.Count} 단어");
    }

    // 2. 텍스트를 정수 ID 배열로 변환합니다. (Sentis 입력용)
    public int[] TokenizeToIds(string text, int maxLength)
    {
        // 기본 토큰화 실행
        string[] tokens = Fulltokenize(text, _vocabulary);

        List<int> ids = new List<int>();

        // [CLS] 토큰 추가 (KLUE 기준 2번)
        ids.Add(_vocabulary.ContainsKey("[CLS]") ? _vocabulary["[CLS]"] : 2);

        // 텍스트 토큰을 ID로 변환
        foreach (var token in tokens)
        {
            if (ids.Count >= maxLength - 1) break; // [SEP] 자리를 위해 남겨둠

            if (_vocabulary.ContainsKey(token))
                ids.Add(_vocabulary[token]);
            else
                ids.Add(_vocabulary["[UNK]"]); // 모르는 단어는 [UNK]
        }

        // [SEP] 토큰 추가 (KLUE 기준 3번)
        ids.Add(_vocabulary.ContainsKey("[SEP]") ? _vocabulary["[SEP]"] : 3);

        // Padding (0번으로 채움)
        while (ids.Count < maxLength)
        {
            ids.Add(0);
        }

        return ids.ToArray();
    }

    // --- 이하 제공해주신 알고리즘 (수정 없이 포함) ---
    public static string[] Fulltokenize(string text, Dictionary<string, int> table)
    {
        return BasicTokenize(text).SelectMany(word => WordPieceTokenize(word, table)).ToArray();
    }

    public static string[] BasicTokenize(string text)
    {
        var sb = new StringBuilder();
        text = text.Normalize(NormalizationForm.FormC);
        foreach (char c in text)
        {
            if (c.IsBertWhiteSpace()) { sb.Append(' '); continue; }
            if (c.IsBertShouldBeRemoved() || c.IsBertControl()) continue;
            sb.Append(c);
        }
        string processedText = sb.ToString().ToLower();
        return processedText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                           .SelectMany(TokenizeWithPunctuation).ToArray();
    }

    public static string[] WordPieceTokenize(string text, Dictionary<string, int> table)
    {
        const string UNKNOWN_TOKEN = "[UNK]";
        var tokens = new List<string>();
        int start = 0;
        while (start < text.Length)
        {
            int end = text.Length;
            string curSubstr = null;
            while (start < end)
            {
                string substr = text.Substring(start, end - start);
                if (start > 0) substr = "##" + substr;
                if (table.ContainsKey(substr)) { curSubstr = substr; break; }
                end -= 1;
            }
            if (curSubstr == null) { tokens.Add(UNKNOWN_TOKEN); break; }
            tokens.Add(curSubstr);
            start = end;
        }
        return tokens.ToArray();
    }

    private static string[] TokenizeWithPunctuation(string text)
    {
        var sb = new StringBuilder();
        var tokens = new List<string>();
        foreach (var c in text)
        {
            if (c.IsBertPunctuation())
            {
                if (sb.Length > 0) tokens.Add(sb.ToString());
                tokens.Add(c.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        if (sb.Length > 0) tokens.Add(sb.ToString());
        return tokens.ToArray();
    }
}

// CharExtension 클래스 등은 동일하게 유지 (생략)