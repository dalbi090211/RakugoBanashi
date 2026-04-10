using System;
using System.Runtime.InteropServices;

public static class LlamaPlugin
{
    private const string DLL = "llama";

    // ── Backend ──────────────────────────────────────────────────
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_backend_init();

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_backend_free();

    // ── Model ────────────────────────────────────────────────────
    // 구버전 deprecated → 새 API 사용
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_model_load_from_file(string path_model, LlamaModelParams mparams);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_model_free(IntPtr model);

    // struct 값 반환 ABI 우회: MSVC x64는 hidden pointer를 첫 번째 인자로 넘김
    // C#에서 out 파라미터로 매핑하면 동일하게 동작
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "llama_model_default_params")]
    public static extern void llama_model_default_params(out LlamaModelParams result);

    // ── Context ──────────────────────────────────────────────────
    // deprecated llama_new_context_with_model → llama_init_from_model
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_init_from_model(IntPtr model, LlamaContextParams cparams);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "llama_context_default_params")]
    public static extern void llama_context_default_params(out LlamaContextParams result);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_free(IntPtr ctx);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_get_memory(IntPtr ctx);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void llama_memory_clear(IntPtr mem, [MarshalAs(UnmanagedType.I1)] bool data);

    // ── Vocab ────────────────────────────────────────────────────
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_model_get_vocab(IntPtr model);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_vocab_n_tokens(IntPtr vocab);

    // deprecated llama_token_eos → llama_vocab_eos
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_vocab_eos(IntPtr vocab);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool llama_vocab_is_eog(IntPtr vocab, int token);

    // ── Tokenize ─────────────────────────────────────────────────
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_tokenize(
        IntPtr vocab, string text, int text_len,
        int[] tokens, int n_tokens_max,
        bool add_special, bool parse_special);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_token_to_piece(
        IntPtr vocab, int token,
        byte[] buf, int length,
        int lstrip, bool special);

    // ── Batch ────────────────────────────────────────────────────
    // struct 반환 ABI 우회: out을 첫 번째 파라미터로
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "llama_batch_get_one")]
    public static extern void llama_batch_get_one(out LlamaBatch result, int[] tokens, int n_tokens);

    // struct 값 전달 ABI 우회: ref로 포인터 전달
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int llama_decode(IntPtr ctx, ref LlamaBatch batch);

    // ── Logits ───────────────────────────────────────────────────
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr llama_get_logits_ith(IntPtr ctx, int i);

    // ── Structs ──────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct LlamaModelParams
    {
        public IntPtr devices;
        public IntPtr tensor_buft_overrides;
        public int n_gpu_layers;
        public int split_mode;
        public int main_gpu;
        private int _pad;
        public IntPtr tensor_split;
        public IntPtr progress_callback;
        public IntPtr progress_callback_user_data;
        public IntPtr kv_overrides;
        [MarshalAs(UnmanagedType.I1)] public bool vocab_only;
        [MarshalAs(UnmanagedType.I1)] public bool use_mmap;
        [MarshalAs(UnmanagedType.I1)] public bool use_direct_io;
        [MarshalAs(UnmanagedType.I1)] public bool use_mlock;
        [MarshalAs(UnmanagedType.I1)] public bool check_tensors;
        [MarshalAs(UnmanagedType.I1)] public bool use_extra_bufts;
        [MarshalAs(UnmanagedType.I1)] public bool no_host;
        [MarshalAs(UnmanagedType.I1)] public bool no_alloc;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct LlamaContextParams
    {
        public uint n_ctx;
        public uint n_batch;
        public uint n_ubatch;
        public uint n_seq_max;
        public int n_threads;
        public int n_threads_batch;
        public int rope_scaling_type;
        public int pooling_type;
        public int attention_type;
        public int flash_attn_type;
        public float rope_freq_base;
        public float rope_freq_scale;
        public float yarn_ext_factor;
        public float yarn_attn_factor;
        public float yarn_beta_fast;
        public float yarn_beta_slow;
        public uint yarn_orig_ctx;
        public float defrag_thold;
        public IntPtr cb_eval;
        public IntPtr cb_eval_user_data;
        public int type_k;
        public int type_v;
        private int _pad1;
        private int _pad2;
        public IntPtr abort_callback;
        public IntPtr abort_callback_data;
        [MarshalAs(UnmanagedType.I1)] public bool embeddings;
        [MarshalAs(UnmanagedType.I1)] public bool offload_kqv;
        [MarshalAs(UnmanagedType.I1)] public bool no_perf;
        [MarshalAs(UnmanagedType.I1)] public bool op_offload;
        [MarshalAs(UnmanagedType.I1)] public bool swa_full;
        [MarshalAs(UnmanagedType.I1)] public bool kv_unified;
        private byte _pad3;
        private byte _pad4;
        public IntPtr samplers;
        public UIntPtr n_samplers;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaBatch
    {
        public int n_tokens;
        public IntPtr token;
        public IntPtr embd;
        public IntPtr pos;
        public IntPtr n_seq_id;
        public IntPtr seq_id;
        public IntPtr logits;
    }
}