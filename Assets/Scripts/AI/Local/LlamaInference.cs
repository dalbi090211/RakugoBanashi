using System;
using System.Runtime.InteropServices;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;


public class LlamaInference : IDisposable
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string lpPathName);
    private static System.Random rng = new System.Random();
    private IntPtr model;
    private IntPtr ctx;
    private const int MaxNewTokens = 256;
    private static string logPath = @"C:\workspace\llama_log.txt";

    private static void Log(string msg)
    {
        string line = $"[{System.DateTime.Now:HH:mm:ss.fff}] {msg}";
        System.IO.File.AppendAllText(logPath, line + "\n");
        Debug.Log(msg);
    }

    public bool Load(string modelPath, int nGpuLayers = 20)
    {
        if (!System.IO.File.Exists(modelPath))
        {
            Debug.LogError($"파일 없음: {modelPath}");
            return false;
        }

        // 에디터와 빌드 둘 다 대응
        string dllPath;
#if UNITY_EDITOR
        dllPath = System.IO.Path.Combine(Application.dataPath, "Plugins/Windows/x86_64");
#else
        dllPath = System.IO.Path.Combine(Application.dataPath, "Plugins/x86_64");
#endif
        SetDllDirectory(dllPath);

        Log("backend_init 호출");
        LlamaPlugin.llama_backend_init();
        Log("backend_init 완료");

        LlamaPlugin.llama_model_default_params(out var mparams);
        // 기본값 무시하고 직접 설정
        mparams.n_gpu_layers = nGpuLayers;
        mparams.use_mmap = true;
        mparams.use_mlock = false;
        mparams.vocab_only = false;
        mparams.devices = IntPtr.Zero;
        mparams.tensor_buft_overrides = IntPtr.Zero;
        mparams.tensor_split = IntPtr.Zero;
        mparams.progress_callback = IntPtr.Zero;
        mparams.progress_callback_user_data = IntPtr.Zero;
        mparams.kv_overrides = IntPtr.Zero;
        mparams.split_mode = 1; // LLAMA_SPLIT_MODE_LAYER
        mparams.main_gpu = 0;
        Log($"mparams 설정 완료: gpu_layers={mparams.n_gpu_layers}");

        Log("model load 시작");
        model = LlamaPlugin.llama_model_load_from_file(modelPath, mparams);
        Log($"model: {model}");

        if (model == IntPtr.Zero)
        {
            Log("모델 로드 실패");
            return false;
        }

        LlamaPlugin.llama_context_default_params(out var cparams);
        cparams.n_ctx = 2048;
        cparams.n_batch = 512;
        cparams.n_ubatch = 512;
        cparams.n_threads = 8;

        ctx = LlamaPlugin.llama_init_from_model(model, cparams);
        return ctx != IntPtr.Zero;
    }

    public async UniTask<string> GenerateAsync(string prompt)
    {
        return await UniTask.RunOnThreadPool(() => Generate(prompt));
    }

    private string Generate(string prompt)
    {
        IntPtr mem = LlamaPlugin.llama_get_memory(ctx);
        if (mem != IntPtr.Zero)
            LlamaPlugin.llama_memory_clear(mem, false);

        Log("KV 캐시 초기화 완료");
        IntPtr vocab = LlamaPlugin.llama_model_get_vocab(model);
        int[] tokens = new int[2048];
        int nTokens = LlamaPlugin.llama_tokenize(vocab, prompt, prompt.Length, tokens, tokens.Length, true, true);
        Log($"토크나이즈 완료: {nTokens}개");

        int[] promptTokens = new int[nTokens];
        Array.Copy(tokens, promptTokens, nTokens);

        LlamaPlugin.llama_batch_get_one(out var batch, promptTokens, nTokens);
        Log($"배치 완료, n_tokens={batch.n_tokens}");

        var result = new StringBuilder();
        int nVocab = LlamaPlugin.llama_vocab_n_tokens(vocab);
        int n_cur = nTokens;

        for (int i = 0; i < MaxNewTokens; i++)
        {
            Log($"디코드 {i}");
            if (LlamaPlugin.llama_decode(ctx, ref batch) != 0) break;
            Log("디코드 완료");

            IntPtr logitsPtr = LlamaPlugin.llama_get_logits_ith(ctx, batch.n_tokens - 1);
            if (logitsPtr == IntPtr.Zero) { Log("logits NULL"); break; }

            int lastToken = SampleWithTemperature(logitsPtr, nVocab, 1.0f);
            Log($"샘플: {lastToken}");

            if (LlamaPlugin.llama_vocab_is_eog(vocab, lastToken)) break;

            result.Append(TokenToPiece(vocab, lastToken));

            int[] next = new int[] { lastToken };
            LlamaPlugin.llama_batch_get_one(out batch, next, 1);
            n_cur++;
        }

        return result.ToString();
    }

    private string TokenToPiece(IntPtr vocab, int token)
    {
        byte[] buf = new byte[128];
        int n = LlamaPlugin.llama_token_to_piece(vocab, token, buf, buf.Length, 0, false);
        if (n <= 0) return "";
        return System.Text.Encoding.UTF8.GetString(buf, 0, n);
    }

    private int SampleWithTemperature(IntPtr logits, int nVocab, float temperature = 1.0f)
    {
        float[] logitArray = new float[nVocab];
        Marshal.Copy(logits, logitArray, 0, nVocab);

        // Temperature 적용
        float maxLogit = float.MinValue;
        for (int i = 0; i < nVocab; i++)
            if (logitArray[i] > maxLogit) maxLogit = logitArray[i];

        float sum = 0f;
        float[] probs = new float[nVocab];
        for (int i = 0; i < nVocab; i++)
        {
            probs[i] = Mathf.Exp((logitArray[i] - maxLogit) / temperature);
            sum += probs[i];
        }
        for (int i = 0; i < nVocab; i++)
            probs[i] /= sum;

        // 샘플링
        float rand = (float)rng.NextDouble();
        float cumulative = 0f;
        for (int i = 0; i < nVocab; i++)
        {
            cumulative += probs[i];
            if (rand < cumulative) return i;
        }
        return nVocab - 1;
    }

    public void Dispose()
    {
        if (ctx != IntPtr.Zero) LlamaPlugin.llama_free(ctx);
        if (model != IntPtr.Zero) LlamaPlugin.llama_model_free(model);
        LlamaPlugin.llama_backend_free();
        ctx = IntPtr.Zero;
        model = IntPtr.Zero;
    }
}