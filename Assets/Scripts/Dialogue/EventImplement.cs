using Cysharp.Threading.Tasks;
using UnityEngine;

public class ScareEnvCommand : IEvent
{
    public void Execute(string parameter)
    {
        EnvManager.Instance.SetRedEnv(2f).Forget();
        EnvManager.Instance.SetRedSpotLight(2f).Forget();
    }
}

public class NormalEnvCommand : IEvent
{
    public void Execute(string parameter)
    {
        EnvManager.Instance.SetBrightEnv(2f).Forget();
        EnvManager.Instance.SetBrightSpotLight(2f).Forget();
    }
}