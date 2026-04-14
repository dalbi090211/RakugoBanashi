using FMODUnity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum CamDir
{
    left,
    middle,
    right
}

public enum BGMArea
{
    Unset = -1,
    GrayTown = 0,
    Casino = 1,
    Boss_Greed = 2
}

public enum eventType
{
    Dial,
    Zoom,
    Anim,
    SFX,
    BGM,
    Event,
    ChoiceDial,
    Emotion,
    AppendDial,
    EndingChoice,
}

[System.Serializable]
public struct choiceRes
{
    public string textName;
    public string appendDialPath;
}

[System.Serializable]
public abstract class GameEvent
{
    public eventType Type { get; protected set; }
    public float delayBefore = 0f;
}

[System.Serializable]
public class Dialogue : GameEvent
{
    public bool checkInput = true;
    public CamDir direction;
    public string Text;

    public Dialogue() { Type = eventType.Dial; }
}

[System.Serializable]
public class ChoiceDialogue : GameEvent
{
    public CamDir direction;
    public choiceRes next1;
    public choiceRes next2;
    public choiceRes next3;

    public ChoiceDialogue() { Type = eventType.ChoiceDial; }
}

[System.Serializable]
public class Animation : GameEvent
{
    public string animTargetName; // Manager의 ActorMap에서 런타임 조회
    public string animationName;

    public Animation() { Type = eventType.Anim; }
}

[System.Serializable]
public class Zoom : GameEvent
{
    public string followTargetName;
    public float ZoomSize;
    public float ZoomSpeed;

    public Zoom() { Type = eventType.Zoom; }
}

[System.Serializable]
public class SFX : GameEvent
{
    public EventReference SFXSound;

    public SFX()
    {
        Type = eventType.SFX;
    }
}

[System.Serializable]
public class BGM : GameEvent
{
    public BGMArea BGMSound;

    public BGM()
    {
        Type = eventType.BGM;
    }
}


[System.Serializable]
public class MethodTrigger : GameEvent
{
    public EventCommandType commandName;
    public string commandParameter;

    public MethodTrigger()
    {
        Type = eventType.Event;
    }
}

[System.Serializable]
public class Emotion : GameEvent
{
    public int score;
    public bool clear;

    public Emotion()
    {
        Type = eventType.Emotion;
    }
}

[Serializable]
public class ChoiceOption
{
    public string textName;
    public List<GameEvent> events = new List<GameEvent>();
}

[Serializable]
public class AppendDial : GameEvent
{
    public List<GameEvent> events = new List<GameEvent>();
    public AppendDial()
    {
        Type = eventType.AppendDial;
    }
}

[System.Serializable]
public struct endingRes
{
    public string title;        // "이름을 되묻는다"
    public string branchId;     // appendDialPath 역할
    public int requiredFlow;    // 분위기 조건 (0이면 조건 없음)
}

[System.Serializable]
public class EndingChoice : GameEvent
{
    public endingRes ending1;
    public endingRes ending2;
    public endingRes ending3;
    public endingRes ending4;

    public EndingChoice() { Type = eventType.EndingChoice; }
}