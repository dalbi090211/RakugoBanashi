using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnvManager : Singleton<EnvManager>
{
    private const int PRIOIRTY_NUM = 10;

    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Light2D middleLight;
    [SerializeField] private Light2D audience1stLight;
    [SerializeField] private Light2D audience2stLight;
    [SerializeField] private CinemachineCamera envVcam;
    [SerializeField] private CinemachineCamera leftVcam;
    [SerializeField] private CinemachineCamera rightVcam;
    [SerializeField] private CinemachineCamera middleVcam;
    [SerializeField] private Animator playerAnimator;

    // ── Impulse ───────────────────────────────────────────────────
    // Inspector에서 ImpulseSource 컴포넌트 연결
    // (EnvManager 오브젝트에 CinemachineImpulseSource 추가)
    [Header("카메라 흔들림")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private static readonly Color redEnvColor = new Color32(203, 113, 113, 255);
    private static readonly Color darkEnvColor = new Color32(87, 87, 87, 255);
    private static readonly Color offEnvColor = new Color32(0, 0, 0, 255);
    private static readonly Color brightEnvColor = new Color32(200, 200, 200, 255);

    private static readonly Vector3 leftOriginOffset = new Vector3(-2.49f, -0.52f, -4.53f);
    private static readonly Vector3 rightOriginOffset = new Vector3(3.06f, -0.66f, -3.9f);
    private static readonly Vector3 leftChangeOffset = new Vector3(-0.35f, 0f, 0f);
    private static readonly Vector3 rightChangeOffset = new Vector3(0.2f, 0f, 0f);

    private ColorAdjustments colorAdjustments;
    private Color curColor = new Color32(87, 87, 87, 255);

    // 현재 활성 vcam 추적
    private CinemachineCamera curVcam;

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        CleanVCam();
        curVcam = envVcam;
        envVcam.Priority = PRIOIRTY_NUM;
        postProcessVolume.profile.TryGet(out colorAdjustments);
        SetDarkEnv(0f).Forget();
        SetOffSpotLight(0f).Forget();
    }

    #endregion

    #region Public API

    public async UniTask SetRedEnv(float time) => await SetEnvColor(redEnvColor, time);
    public async UniTask SetDarkEnv(float time) => await SetEnvColor(darkEnvColor, time);
    public async UniTask SetBrightEnv(float time) => await SetEnvColor(brightEnvColor, time);

    public async UniTask SetRedSpotLight(float time) => await SetSpotLightColor(redEnvColor, time);
    public async UniTask SetDarkSpotLight(float time) => await SetSpotLightColor(darkEnvColor, time);
    public async UniTask SetBrightSpotLight(float time) => await SetSpotLightColor(brightEnvColor, time);
    public async UniTask SetOffSpotLight(float time) => await SetSpotLightColor(offEnvColor, time);

    public async UniTask SetLeftVCam(float time)
    {
        CleanVCam();
        curVcam = leftVcam;
        leftVcam.Priority = PRIOIRTY_NUM;
        await UniTask.CompletedTask;
    }

    public async UniTask SetRightVCam(float time)
    {
        CleanVCam();
        curVcam = rightVcam;
        rightVcam.Priority = PRIOIRTY_NUM;
        await UniTask.CompletedTask;
    }

    public async UniTask SetEnvVCam(float time)
    {
        CleanVCam();
        curVcam = envVcam;
        envVcam.Priority = PRIOIRTY_NUM;
        await UniTask.CompletedTask;
    }

    public async UniTask SetMiddleVCam(float time)
    {
        CleanVCam();
        curVcam = middleVcam;
        middleVcam.Priority = PRIOIRTY_NUM;
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// cramble 태그 트리거 → 현재 활성 vcam 흔들기
    /// </summary>
    /// <param name="duration">흔들림 지속 시간(초)</param>
    /// <param name="intensity">흔들림 강도</param>
    public void CameraShake(float duration, float intensity)
    {
        if (impulseSource == null)
        {
            Debug.LogWarning("ImpulseSource가 연결되지 않았습니다.");
            return;
        }

        // ImpulseDefinition에서 duration 조정 후 impulse 발생
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;
        impulseSource.GenerateImpulse(intensity);
    }

    #endregion

    #region 실제 구현

    private async UniTask SetEnvColor(Color targetColor, float time)
    {
        Color start = curColor;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            colorAdjustments.colorFilter.value = Color.Lerp(start, targetColor, elapsed / time);
            await UniTask.Yield();
        }

        colorAdjustments.colorFilter.value = targetColor;
        curColor = targetColor;
    }

    private async UniTask SetSpotLightColor(Color targetColor, float time)
    {
        Color startMiddle = middleLight.color;
        Color startAudience1 = audience1stLight.color;
        Color startAudience2 = audience2stLight.color;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time;
            middleLight.color = Color.Lerp(startMiddle, targetColor, t);
            audience1stLight.color = Color.Lerp(startAudience1, targetColor, t);
            audience2stLight.color = Color.Lerp(startAudience2, targetColor, t);
            await UniTask.Yield();
        }

        middleLight.color = targetColor;
        audience1stLight.color = targetColor;
        audience2stLight.color = targetColor;
    }

    #endregion

    #region 초기화

    private void CleanVCam()
    {
        leftVcam.Priority = 0;
        rightVcam.Priority = 0;
        middleVcam.Priority = 0;
        envVcam.Priority = 0;
    }

    #endregion
}