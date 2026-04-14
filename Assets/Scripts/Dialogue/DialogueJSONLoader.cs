using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class DialogueJsonLoader
{
    // ── 외부에서 쓰는 진입점 ──────────────────────────
    public static async UniTask<RuntimeDialogueData> LoadAsync(string sceneName)
    {
        string path = Path.Combine(
            Application.streamingAssetsPath, "Dialogues", sceneName + ".json");

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android는 UnityWebRequest 필요
        var req = UnityEngine.Networking.UnityWebRequest.Get(path);
        await req.SendWebRequest();
        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"JSON 로드 실패: {path}");
            return null;
        }
        string json = req.downloadHandler.text;
#else
        if (!File.Exists(path))
        {
            Debug.LogError($"JSON 파일 없음: {path}");
            return null;
        }
        string json = await File.ReadAllTextAsync(path);
#endif

        return Parse(json);
    }

    // ── 파싱 ──────────────────────────────────────────
    public static RuntimeDialogueData Parse(string json)
    {
        var root = JObject.Parse(json);
        var result = new RuntimeDialogueData();

        result.sceneName = root["sceneName"]?.ToString() ?? "";
        result.dialogues = ParseEvents(root["dialogues"] as JArray);

        // branches 딕셔너리
        if (root["branches"] is JObject branchObj)
        {
            foreach (var kv in branchObj)
                result.branches[kv.Key] = ParseEvents(kv.Value as JArray);
        }

        return result;
    }

    private static List<GameEvent> ParseEvents(JArray arr)
    {
        var list = new List<GameEvent>();
        if (arr == null) return list;

        foreach (var token in arr)
        {
            var ev = ParseEvent(token as JObject);
            if (ev != null) list.Add(ev);
        }
        return list;
    }

    private static GameEvent ParseEvent(JObject obj)
    {
        if (obj == null) return null;

        string typeStr = obj["type"]?.ToString() ?? "";
        float delay = obj["delayBefore"]?.Value<float>() ?? 0f;

        if (!System.Enum.TryParse<eventType>(typeStr, out var type))
        {
            Debug.LogWarning($"알 수 없는 이벤트 타입: {typeStr}");
            return null;
        }

        switch (type)
        {
            case eventType.Dial:
                return new Dialogue
                {
                    delayBefore = delay,
                    Text = obj["text"]?.ToString() ?? "",
                    direction = ParseDir(obj["direction"]),
                    checkInput = obj["checkInput"]?.Value<bool>() ?? true
                };

            case eventType.ChoiceDial:
                var choiceDial = new ChoiceDialogue { delayBefore = delay };
                choiceDial.direction = ParseDir(obj["direction"]);

                var choices = obj["choices"] as JArray;
                if (choices != null)
                {
                    if (choices.Count > 0) choiceDial.next1 = ParseChoiceRes(choices[0] as JObject);
                    if (choices.Count > 1) choiceDial.next2 = ParseChoiceRes(choices[1] as JObject);
                    if (choices.Count > 2) choiceDial.next3 = ParseChoiceRes(choices[2] as JObject);
                }
                return choiceDial;

            case eventType.Anim:
                return new Animation
                {
                    delayBefore = delay,
                    animationName = obj["animationName"]?.ToString() ?? ""
                    // animTarget은 ActorRegistry에서 런타임 해석
                };

            case eventType.Zoom:
                return new Zoom
                {
                    delayBefore = delay,
                    ZoomSize = obj["zoomSize"]?.Value<float>() ?? 1f,
                    ZoomSpeed = obj["zoomSpeed"]?.Value<float>() ?? 1f
                    // followTarget은 ActorRegistry에서 런타임 해석
                };

            case eventType.SFX:
                // FMOD path는 별도 매핑 필요 - 지금은 name만 저장
                return new SFX { delayBefore = delay };

            case eventType.BGM:
                var bgm = new BGM { delayBefore = delay };
                if (System.Enum.TryParse<BGMArea>(obj["bgm"]?.ToString(), out var bgmArea))
                    bgm.BGMSound = bgmArea;
                return bgm;

            case eventType.Event:
                var trigger = new MethodTrigger { delayBefore = delay };
                if (System.Enum.TryParse<EventCommandType>(
                    obj["command"]?.ToString(), out var cmd))
                    trigger.commandName = cmd;
                trigger.commandParameter = obj["parameter"]?.ToString() ?? "";
                return trigger;

            case eventType.Emotion:
                return new Emotion
                {
                    delayBefore = delay,
                    score = obj["score"]?.Value<int>() ?? 0,
                    clear = obj["clear"]?.Value<bool>() ?? false
                };

            case eventType.AppendDial:
                return new AppendDial
                {
                    delayBefore = delay,
                    events = ParseEvents(obj["events"] as JArray)
                };

            case eventType.EndingChoice:
                var ec = new EndingChoice { delayBefore = delay };
                var endings = obj["endings"] as JArray;
                if (endings != null)
                {
                    if (endings.Count > 0) ec.ending1 = ParseEndingRes(endings[0] as JObject);
                    if (endings.Count > 1) ec.ending2 = ParseEndingRes(endings[1] as JObject);
                    if (endings.Count > 2) ec.ending3 = ParseEndingRes(endings[2] as JObject);
                    if (endings.Count > 3) ec.ending4 = ParseEndingRes(endings[3] as JObject);
                }
                return ec;

            default:
                return null;
        }
    }

    private static CamDir ParseDir(JToken token)
    {
        if (System.Enum.TryParse<CamDir>(
            token?.ToString(), ignoreCase: true, out var dir))
            return dir;
        return CamDir.middle;
    }

    private static choiceRes ParseChoiceRes(JObject obj)
    {
        return new choiceRes
        {
            textName = obj?["text"]?.ToString() ?? "",
            appendDialPath = obj?["branchId"]?.ToString() ?? ""
        };
    }

    private static endingRes ParseEndingRes(JObject obj)
    {
        return new endingRes
        {
            title = obj?["title"]?.ToString() ?? "",
            branchId = obj?["branchId"]?.ToString() ?? "",
            requiredFlow = obj?["requiredFlow"]?.Value<int>() ?? 0
        };
    }
}