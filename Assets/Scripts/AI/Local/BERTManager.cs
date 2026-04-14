using UnityEngine;
using Unity.InferenceEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

public class BERTManager : Singleton<BERTManager>
{
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private TextAsset vocabAsset;

    private Worker _worker;
    private BertTokenizer _tokenizer;
    private bool _isReady = false;
    private UniTaskCompletionSource _readyTcs = new();

    protected override void Awake()
    {
        Initialize().Forget();
    }

    private async UniTaskVoid Initialize()
    {
        var runtimeModel = ModelLoader.Load(modelAsset);
        _worker = new Worker(runtimeModel, BackendType.GPUCompute);

        _tokenizer = new BertTokenizer();
        _tokenizer.LoadVocabulary(vocabAsset);

        _isReady = true;
        _readyTcs.TrySetResult();
        Debug.Log("BERT 모델 로드 완료");
    }

    public async UniTask<float> PredictScore(string topic, string text)
    {
        if (!_isReady) return 0f;

        string inputString = $"주제: {topic} 텍스트: {text}";
        int[] ids = _tokenizer.TokenizeToIds(inputString, 128);
        int[] mask = ids.Select(x => x != 0 ? 1 : 0).ToArray();

        using var inputIdsTensor = new Tensor<int>(new TensorShape(1, 128), ids);
        using var attentionMaskTensor = new Tensor<int>(new TensorShape(1, 128), mask);

        _worker.SetInput("input_ids", inputIdsTensor);
        _worker.SetInput("attention_mask", attentionMaskTensor);
        _worker.Schedule();

        var output = _worker.PeekOutput("logits") as Tensor<float>;
        var result = await output.ReadbackAndCloneAsync();

        float score = result[0];
        result.Dispose();

        return Mathf.Clamp(score * 100f, 0f, 100f);
    }

    public async UniTask WaitUntilReady()
    {
        if (_isReady) return;
        await _readyTcs.Task;
    }

    private void OnDestroy()
    {
        _worker?.Dispose();
    }
}