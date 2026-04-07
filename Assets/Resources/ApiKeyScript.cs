using UnityEngine;

public enum AIProvider
{
    Anthropic,
    Gemini,
    OpenAI,
    Ollama
}

[CreateAssetMenu(fileName = "ApiKeys", menuName = "Config/ApiKeys")]
public class ApiKeys : ScriptableObject
{
    public AIProvider provider;
    public string anthropicKey;
    public string geminiKey;
    public string openAIKey;
}