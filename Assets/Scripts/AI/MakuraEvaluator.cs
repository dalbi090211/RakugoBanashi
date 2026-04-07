using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

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
        return keys.provider switch
        {
            AIProvider.Anthropic => await EvaluateAnthropic(keys.anthropicKey, topic, playerInput),
            AIProvider.Gemini => await EvaluateGemini(keys.geminiKey, topic, playerInput),
            AIProvider.OpenAI => await EvaluateOpenAI(keys.openAIKey, topic, playerInput),
            AIProvider.Ollama => await EvaluateOllama(topic, playerInput),
            _ => null
        };
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

    private string BuildPrompt(string topic, string playerInput) => $@"당신은 라쿠고 마쿠라 평가자입니다.
        주제: {topic}
        플레이어 입력: {playerInput}

    아래 기준으로 평가하고 반드시 JSON만 반환하세요. 다른 텍스트 없이 JSON만.
    {{
        ""score"": 0~100,
        ""isRelated"": true또는false,
        ""feedback"": ""한줄 피드백""
    }}";
}

// Response 클래스들
[System.Serializable]
public class MakuraResult
{
    public int score;
    public bool isRelated;
    public string feedback;
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