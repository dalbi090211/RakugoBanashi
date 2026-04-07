using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    private DialogueData data;
    private SerializedProperty dialoguesProp;

    private void OnEnable()
    {
        data = (DialogueData)target;
        dialoguesProp = serializedObject.FindProperty("dialogues");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Scene Name 필드
        data.sceneName = EditorGUILayout.TextField("Scene Name", data.sceneName);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dialogue Timeline", EditorStyles.boldLabel);

        for (int i = 0; i < data.dialogues.Count; i++)
        {
            GameEvent evt = data.dialogues[i];
            if (evt == null)
            {
                EditorGUILayout.LabelField("Null Event");
                continue;
            }

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"[{i}] {evt.Type}", EditorStyles.boldLabel);
            evt.delayBefore = EditorGUILayout.FloatField("Delay Before", evt.delayBefore);

            switch (evt.Type)
            {
                case eventType.Dial:
                    Dialogue dial = evt as Dialogue;
                    dial.TextTarget = (GameObject)EditorGUILayout.ObjectField("Text Target", dial.TextTarget, typeof(GameObject), true);
                    dial.Text = EditorGUILayout.TextField("Text", dial.Text);
                    dial.isLeft = EditorGUILayout.Toggle("Is Left", dial.isLeft);
                    dial.checkInput = EditorGUILayout.Toggle("Check Input", dial.checkInput);
                    break;

                case eventType.ChoiceDial:
                    ChoiceDialogue choiceDial = evt as ChoiceDialogue;
                    choiceDial.TextTarget = (GameObject)EditorGUILayout.ObjectField("Text Target", choiceDial.TextTarget, typeof(GameObject), true);
                    choiceDial.isLeft = EditorGUILayout.Toggle("Is Left", choiceDial.isLeft);

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Choice 1", EditorStyles.boldLabel);
                    choiceDial.next1.textName = EditorGUILayout.TextField("Text", choiceDial.next1.textName);
                    choiceDial.next1.appendDialPath = EditorGUILayout.TextField("Dialogue Path", choiceDial.next1.appendDialPath);

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Choice 2", EditorStyles.boldLabel);
                    choiceDial.next2.textName = EditorGUILayout.TextField("Text", choiceDial.next2.textName);
                    choiceDial.next2.appendDialPath = EditorGUILayout.TextField("Dialogue Path", choiceDial.next2.appendDialPath);

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Choice 3", EditorStyles.boldLabel);
                    choiceDial.next3.textName = EditorGUILayout.TextField("Text", choiceDial.next3.textName);
                    choiceDial.next3.appendDialPath = EditorGUILayout.TextField("Dialogue Path", choiceDial.next3.appendDialPath);
                    break;

                case eventType.Anim:
                    Animation anim = evt as Animation;
                    anim.AnimTarget = (GameObject)EditorGUILayout.ObjectField("Anim Target", anim.AnimTarget, typeof(GameObject), true);
                    anim.animationName = EditorGUILayout.TextField("Animation Name", anim.animationName);
                    break;

                case eventType.Zoom:
                    Zoom zoom = evt as Zoom;
                    zoom.FollowTarget = (GameObject)EditorGUILayout.ObjectField("Follow Target", zoom.FollowTarget, typeof(GameObject), true);
                    zoom.ZoomSize = EditorGUILayout.FloatField("Zoom Size", zoom.ZoomSize);
                    zoom.ZoomSpeed = EditorGUILayout.FloatField("Zoom Speed", zoom.ZoomSpeed);
                    break;

                case eventType.SFX:
                    SFX sfx = evt as SFX;
                    SerializedProperty sfxProp = serializedObject.FindProperty($"dialogues.Array.data[{i}].SFXSound");
                    EditorGUILayout.PropertyField(sfxProp, new GUIContent("SFX Sound"));
                    break;

                case eventType.BGM:
                    BGM bgm = evt as BGM;
                    SerializedProperty bgmProp = serializedObject.FindProperty($"dialogues.Array.data[{i}].BGMSound");
                    EditorGUILayout.PropertyField(bgmProp, new GUIContent("BGM Sound"));
                    break;

                case eventType.Event:
                    MethodTrigger methodTrigger = evt as MethodTrigger;
                    methodTrigger.commandName = (EventCommandType)EditorGUILayout.EnumPopup("Command", methodTrigger.commandName);
                    methodTrigger.commandParameter = EditorGUILayout.TextField("Command Parameter", methodTrigger.commandParameter);
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▲") && i > 0)
            {
                var temp = data.dialogues[i];
                data.dialogues[i] = data.dialogues[i - 1];
                data.dialogues[i - 1] = temp;
            }
            if (GUILayout.Button("▼") && i < data.dialogues.Count - 1)
            {
                var temp = data.dialogues[i];
                data.dialogues[i] = data.dialogues[i + 1];
                data.dialogues[i + 1] = temp;
            }
            if (GUILayout.Button("X"))
            {
                data.dialogues.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        DrawAddButtons();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }

    private void DrawAddButtons()
    {
        EditorGUILayout.LabelField("Add Event", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Dialogue"))
            data.dialogues.Add(new Dialogue());
        if (GUILayout.Button("Zoom"))
            data.dialogues.Add(new Zoom());
        if (GUILayout.Button("Anim"))
            data.dialogues.Add(new Animation());
        if (GUILayout.Button("SFX"))
            data.dialogues.Add(new SFX());
        if (GUILayout.Button("BGM"))
            data.dialogues.Add(new BGM());
        if (GUILayout.Button("Event"))
            data.dialogues.Add(new MethodTrigger());
        if (GUILayout.Button("Choice"))
            data.dialogues.Add(new ChoiceDialogue());

        EditorGUILayout.EndHorizontal();
    }
}
