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
    public Boolean checkInput = true;
    [SerializeField] private string textTargetPath;  // GameObject 경로를 저장
    public CamDir direction;
    private GameObject _textTarget;
    public GameObject TextTarget    //경로를 저장하다 런타임에 참조를 찾는 방식
    {
        get
        {
            if (_textTarget == null && !string.IsNullOrEmpty(textTargetPath))
            {
                _textTarget = GameObject.Find(textTargetPath);
            }
            return _textTarget;
        }
        set
        {
            _textTarget = value;
            textTargetPath = value != null ? value.name : string.Empty;
        }
    }
    public string Text;

    public Dialogue()
    {
        Type = eventType.Dial;
    }
}

[System.Serializable]
public class ChoiceDialogue : GameEvent
{
    [SerializeField] private string textTargetPath;  // GameObject 경로를 저장
    public CamDir direction;
    private GameObject _textTarget;
    public GameObject TextTarget    //경로를 저장하다 런타임에 참조를 찾는 방식
    {
        get
        {
            if (_textTarget == null && !string.IsNullOrEmpty(textTargetPath))
            {
                _textTarget = GameObject.Find(textTargetPath);
            }
            return _textTarget;
        }
        set
        {
            _textTarget = value;
            textTargetPath = value != null ? value.name : string.Empty;
        }
    }
    public choiceRes next1;
    public choiceRes next2;
    public choiceRes next3;

    public ChoiceDialogue()
    {
        Type = eventType.ChoiceDial;
    }
}

[System.Serializable]
public class Animation : GameEvent
{
    [SerializeField] private string animTargetPath;  // GameObject 경로를 저장
    private GameObject _animTarget;
    public GameObject AnimTarget    //경로를 저장하다 런타임에 참조를 찾는 방식
    {
        get
        {
            if (_animTarget == null && !string.IsNullOrEmpty(animTargetPath))
            {
                _animTarget = GameObject.Find(animTargetPath);
            }
            return _animTarget;
        }
        set
        {
            _animTarget = value;
            animTargetPath = value != null ? value.name : string.Empty;
        }
    }
    public string animationName;

    public Animation()
    {
        Type = eventType.Anim;
    }
}

[System.Serializable]
public class Zoom : GameEvent
{
    [SerializeField] private string followTargetPath;  // GameObject 경로를 저장
    private GameObject _followTarget;
    public GameObject FollowTarget
    {
        get
        {
            if (_followTarget == null && !string.IsNullOrEmpty(followTargetPath))
            {
                _followTarget = GameObject.Find(followTargetPath);
            }
            return _followTarget;
        }
        set
        {
            _followTarget = value;
            followTargetPath = value != null ? value.name : string.Empty;
        }
    }
    public float ZoomSize;
    public float ZoomSpeed;

    public Zoom()
    {
        Type = eventType.Zoom;
    }
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