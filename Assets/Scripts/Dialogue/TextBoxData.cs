using System.Collections.Generic;

/// <summary>텍스트 특수효과 적용 구간</summary>
public struct TextRange
{
    public int startIndex;
    public int endIndex;

    public TextRange(int startIndex, int endIndex)
    {
        this.startIndex = startIndex;
        this.endIndex = endIndex;
    }
}

/// <summary>
/// Manager가 파싱 완료 후 TextBox에 넘겨주는 데이터 묶음
/// </summary>
public struct TextBoxData
{
    /// <summary>TMP 리치텍스트 적용된 최종 출력용 텍스트</summary>
    public string parsedText;

    /// <summary>index → 대기 시간(ms). &lt;delay&gt; 태그 파싱 결과</summary>
    public Dictionary<int, float> delayAt;

    public SpeedRange[] speedRanges;

    /// <summary>효과음을 끊어야 할 글자 인덱스 목록 (쉼표, 마침표 등)</summary>
    public Queue<int> soundBreaks;

    /// <summary>shake 효과 적용 구간 목록</summary>
    public TextRange[] shakeRanges;

    /// <summary>upwn 효과 적용 구간 목록</summary>
    public TextRange[] upwnRanges;

    /// <summary>화면 흔들림 발생 인덱스 목록</summary>
    public int[] crambleTiming;
}

public struct SpeedRange
{
    public int start;
    public int end;
    public float multiplier; // 0.5f = 절반 속도, 2f = 두 배 속도
}