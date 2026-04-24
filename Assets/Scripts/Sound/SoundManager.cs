using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;

public enum BGMArea
{
    Unset = -1,
    GrayTown = 0,
    Casino = 1,
    Boss_Greed = 2
}

public class SoundManager : Singleton<SoundManager>
{
    public static SoundManager instance;
    private List<EventInstance> eventInstances;
    private List<StudioEventEmitter> eventEmitters;
    private EventInstance ambienceEventInstance;
    private EventInstance musicEventInstance;

    protected override void Awake()
    {
        if (instance == null)
        {
            instance = this;
            eventInstances = new List<EventInstance>();
            eventEmitters = new List<StudioEventEmitter>();

            if (!musicEventInstance.isValid())
            {
                // musicEventInstance = CreateInstance(FmodEvents.TotalBGM);
                // musicEventInstance.start();
            }
            else
            {
                FMOD.Studio.PLAYBACK_STATE state;
                musicEventInstance.getPlaybackState(out state);
                if (state != FMOD.Studio.PLAYBACK_STATE.PLAYING)
                {
                    musicEventInstance.start();
                }
            }
            DontDestroyOnLoad(gameObject);
        }
    }

    // private void Start()
    // {
    //     if (GameSceneManager.instance != null)
    //     {
    //         if (GameSceneManager.instance.GetCurArea() != BGMArea.Unset)
    //             SetBGM(GameSceneManager.instance.GetCurArea());
    //     }
    // }

    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public void initializeAmbience(EventReference ambienceEventReference)
    {
        ambienceEventInstance = CreateInstance(ambienceEventReference);
        ambienceEventInstance.start();
    }

    private EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstances.Add(eventInstance);
        return eventInstance;
    }

    public void CreateInstance(EventReference eventReference, float pitch)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstance.setPitch(pitch);
        eventInstance.start();
        eventInstance.release();
    }

    private StudioEventEmitter InitializeEventEmitter(EventReference eventReference, GameObject emitterGameObject)
    {
        StudioEventEmitter emitter = emitterGameObject.GetComponent<StudioEventEmitter>();
        emitter.EventReference = eventReference;
        eventEmitters.Add(emitter);
        return emitter;
    }

    public void SetAmbienceParameter(string parameterName, float parameterValue)
    {
        ambienceEventInstance.setParameterByName(parameterName, parameterValue);
    }

    public void SetBGM(BGMArea area)
    {
        Debug.Log("isValid : " + musicEventInstance.isValid());
        Debug.Log("curMusic : " + musicEventInstance.isValid());

        // 기존 BGM 정리
        if (musicEventInstance.isValid())
        {
            musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicEventInstance.release();
        }

        // 새 인스턴스 생성 및 재생
        // musicEventInstance = CreateInstance(FmodEvents.instance.TotalBGM);
        musicEventInstance.setParameterByName("area", (float)area);
        musicEventInstance.start();
    }

    public BGMArea GetCurBGM()
    {
        float areaValue;
        musicEventInstance.getParameterByName("area", out areaValue);
        return (BGMArea)areaValue;
    }

    // void OnDestroy() {
    //     Debug.Log("SoundManager Destroyed: " + gameObject.GetInstanceID());
    //     if (musicEventInstance.isValid())
    //     {
    //         musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    //         musicEventInstance.release();
    //     }
    // }

}