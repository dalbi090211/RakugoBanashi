using Cysharp.Threading.Tasks;
using UnityEngine;

public class LLMManager : Singleton<LLMManager>
{
    private string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Bert/model.gguf");
    private LlamaInference llama;
    private bool isReady = false;
    private UniTaskCompletionSource readyTcs = new();

    // protected override void Awake()
    // {
    //     Initialize().Forget();
    // }

    private async UniTaskVoid Initialize()
    {
        llama = new LlamaInference();

        // 메인 스레드에서 Load 호출
        bool loaded = llama.Load(modelPath);

        if (!loaded)
        {
            Debug.LogError("LLaMA 로드 실패");
            return;
        }

        isReady = true;
        readyTcs.TrySetResult();
        Debug.Log("LLaMA 준비 완료");
    }

    public async UniTask WaitUntilReady()
    {
        if (isReady) return;
        await readyTcs.Task;
    }

    public async UniTask<string> Generate(string prompt)
    {
        await WaitUntilReady();
        Debug.Log("Generate 시작: " + prompt);
        var result = await llama.GenerateAsync(prompt);
        Debug.Log("Generate 완료: " + result);
        return result;
    }

    private void OnDestroy()
    {
        llama?.Dispose();
    }
}