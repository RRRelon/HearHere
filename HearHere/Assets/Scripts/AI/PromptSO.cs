using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "AI/PromptSO")]
public class PromptSO : ScriptableObject
{
    public string Prompt = "5.0, 0.0): 새가 지저귀는 소리\n" +
                           "(4.05, 2.94): 바람 부는 소리\n" +
                           "(1.55, 4.76): 나뭇잎 바스락거리는 소리\n" +
                           "(-1.55, 4.76): 시냇물 흐르는 소리\n" +
                           "(-4.05, 2.94): 귀뚜라미 우는 소리\n" +
                           "(-5.0, 0.0): 부엉이 우는 소리\n" +
                           "(-4.05, -2.94): 딱따구리가 나무 쪼는 소리\n" +
                           "(-1.55, -4.76): 나무 삐걱이는 소리\n" +
                           "(1.55, -4.76): 다람쥐 소리\n" +
                           "(4.05, -2.94): 개구리 우는 소리\n";
}

#if UNITY_EDITOR
// 이 속성은 Unity 에디터에게 PromptSO 클래스를 편집할 때 이 스크립트를 사용하라고 알려줍니다.
[CustomEditor(typeof(PromptSO))]
public class PromptSOEditor : Editor
{
    private SerializedProperty promptContentProperty;

    // 에디터가 활성화될 때 호출됩니다.
    private void OnEnable()
    {
        promptContentProperty = serializedObject.FindProperty("Prompt");
    }

    public override void OnInspectorGUI()
    {
        // 변경 사항을 감지하고 기록
        serializedObject.Update();
        
        // 스크립트 설정
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((PromptSO)target), typeof(PromptSO), false);
        GUI.enabled = true;

        // 요청하신 대로 "Header"라는 이름의 라벨을 굵은 글씨로 표시합니다.
        EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);

        // 여러 줄을 입력할 수 있는 텍스트 상자(TextArea)를 생성합니다.
        // 사용자가 입력한 내용은 promptContentProperty에 저장됩니다.
        // GUILayout.MinHeight를 사용하여 텍스트 상자의 최소 높이를 지정합니다.
        promptContentProperty.stringValue = EditorGUILayout.TextArea(promptContentProperty.stringValue, GUILayout.MinHeight(300));

        // GUI에서 변경된 내용을 실제 객체에 적용합니다.
        serializedObject.ApplyModifiedProperties();
    }
}
#endif