using UnityEngine;
using FMODUnity;

public static class FmodEvents
{
    private static FmodEventReferences _eventReferences;

    static FmodEvents()
    {
        _eventReferences = Resources.Load<FmodEventReferences>("FmodEventReferences");  //Asset / Resources 폴더에 있는 FmodEventReferences 애셋을 로드
        if (_eventReferences == null)
        {
            Debug.LogError("FmodEventReferences 애셋을 찾을 수 없습니다. Assets > Create > Boxflat > FMOD Event References 메뉴에서 애셋을 생성한 후, Resources 폴더로 옮겨주세요.");
        }
    }

    public static EventReference TalkTickEvent => _eventReferences.TalkTickEvent;
}
