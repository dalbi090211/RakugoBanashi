using System.Collections.Generic;
using UnityEngine;

public enum EventCommandType
{
    SetScareEnv,
    SetNormalEnv
}

public class EventCommandInvoker
{
    private Dictionary<EventCommandType, IEvent> commandMap = new Dictionary<EventCommandType, IEvent>
        {
            { EventCommandType.SetScareEnv, new ScareEnvCommand() },
            { EventCommandType.SetNormalEnv, new NormalEnvCommand() },
        };

    public void InvokeCommand(string commandLine)
    {
        var split = commandLine.Split(':');
        var commandName = split[0];
        var parameter = split.Length > 1 ? split[1] : null;

        if (System.Enum.TryParse(commandName, out EventCommandType commandType) && commandMap.TryGetValue(commandType, out var command))
        {
            command.Execute(parameter);
        }
        else
        {
            Debug.LogWarning($"명령 '{commandName}'을(를) 찾을 수 없습니다.");
        }
    }
}