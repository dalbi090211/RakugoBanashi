using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.InputSystem;

public class MakuraEvaluator
{
    private const string ANTHROPIC_URL = "https://api.anthropic.com/v1/messages";
    private const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
    // private const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    private const string OPENAI_URL = "https://api.openai.com/v1/chat/completions";
    private const string OLLAMA_URL = "http://localhost:11434/v1/chat/completions";

    private ApiKeys GetKeys() => Resources.Load<ApiKeys>("ApiKeys");

    public async UniTask<MakuraResult> Evaluate(string topic, string playerInput)
    {
        var keys = GetKeys();
        Debug.Log("Key : " + keys.provider);
        return keys.provider switch
        {
            AIProvider.Anthropic => await EvaluateAnthropic(keys.anthropicKey, topic, playerInput),
            AIProvider.Gemini => await EvaluateGemini(keys.geminiKey, topic, playerInput),
            AIProvider.OpenAI => await EvaluateOpenAI(keys.openAIKey, topic, playerInput),
            AIProvider.Ollama => await EvaluateOllama(topic, playerInput),
            AIProvider.Local => await EvaluateLocal(topic, playerInput),
            _ => null
        };
    }

    private async UniTask<MakuraResult> EvaluateLocal(string topic, string playerInput)
    {
        await BERTManager.Instance.WaitUntilReady();

        try
        {
            // 1. Sentis 모델을 통해 즉시 점수 계산
            float score = await BERTManager.Instance.PredictScore(topic, playerInput);

            // 2. 기존 MakuraResult 구조에 맞춰 결과 반환
            return new MakuraResult
            {
                score = (int)score,
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Sentis 평가 중 오류 발생: {e.Message}");
            return null;
        }
    }
    private async UniTask<MakuraResult> EvaluateOpenAI(string apiKey, string topic, string playerInput)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = BuildPrompt(topic, playerInput)
                }
            },
            max_tokens = 256
        };

        string json = JsonConvert.SerializeObject(requestBody);
        using var request = new UnityWebRequest(OPENAI_URL, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error + "\n" + request.downloadHandler.text);
            return null;
        }

        var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
        return JsonConvert.DeserializeObject<MakuraResult>(response.choices[0].message.content);
    }

    private async UniTask<MakuraResult> EvaluateAnthropic(string apiKey, string topic, string playerInput)
    {
        var requestBody = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 256,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = BuildPrompt(topic, playerInput)
                }
            }
        };

        string json = JsonConvert.SerializeObject(requestBody);
        using var request = new UnityWebRequest(ANTHROPIC_URL, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-api-key", apiKey);
        request.SetRequestHeader("anthropic-version", "2023-06-01");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error + "\n" + request.downloadHandler.text);
            return null;
        }

        var response = JsonConvert.DeserializeObject<AnthropicResponse>(request.downloadHandler.text);
        return JsonConvert.DeserializeObject<MakuraResult>(response.content[0].text);
    }

    private async UniTask<MakuraResult> EvaluateGemini(string apiKey, string topic, string playerInput)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = BuildPrompt(topic, playerInput) }
                    }
                }
            }
        };

        string json = JsonConvert.SerializeObject(requestBody);
        using var request = new UnityWebRequest($"{GEMINI_URL}?key={apiKey}", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error + "\n" + request.downloadHandler.text);
            return null;
        }

        var response = JsonConvert.DeserializeObject<GeminiResponse>(request.downloadHandler.text);
        return JsonConvert.DeserializeObject<MakuraResult>(response.candidates[0].content.parts[0].text);
    }

    private async UniTask<MakuraResult> EvaluateOllama(string topic, string playerInput)
    {
        var requestBody = new
        {
            model = "qwen3:8b",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = BuildPrompt(topic, playerInput)
                }
            },
            stream = false
        };

        string json = JsonConvert.SerializeObject(requestBody);
        using var request = new UnityWebRequest(OLLAMA_URL, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error + "\n" + request.downloadHandler.text);
            return null;
        }

        var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
        return JsonConvert.DeserializeObject<MakuraResult>(response.choices[0].message.content);
    }

    private string BuildPrompt(string topic, string playerInput)
    {
        return $"<|im_start|>user\n你是落语评分系统。请根据以下标准评分：\n1. 输入文本是否与主题相关\n2. 是否适合作为落语的开场白（枕）\n3. 内容是否有趣或引人入胜\n\n主题：{topic}\n输入文本：{playerInput}\n\n评分规则：\n- 100分：完全相关，非常适合作为落语开场白\n- 50分：部分相关，勉强可以\n- 0分：完全不相关\n\n只输出JSON，不要任何解释：{{\"score\":50}}<|im_end|>\n<|im_start|>assistant\n{{\"score\":";
    }
}

// Response 클래스들
[System.Serializable]
public class MakuraResult
{
    public int score;
}

[System.Serializable]
public class AnthropicResponse
{
    public Content[] content;
    [System.Serializable]
    public class Content { public string text; }
}

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
    [System.Serializable]
    public class Candidate { public Content content; }
    [System.Serializable]
    public class Content { public Part[] parts; }
    [System.Serializable]
    public class Part { public string text; }
}

[System.Serializable]
public class OpenAIResponse
{
    public Choice[] choices;
    [System.Serializable]
    public class Choice { public Message message; }
    [System.Serializable]
    public class Message { public string content; }
}