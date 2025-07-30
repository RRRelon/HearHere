using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/PromptSO")]
public class PromptSO : ScriptableObject
{
    public string Stage1 = "역할 부여 (Persona)\n"+
    "당신은 시각장애인 플레이어를 위한 소리 탐험 게임의 친절한 가이드입니다. 당신의 임무는 플레이어의 위치를 (0, 0)으로 가정하고, 주변에서 들려오는 소리의 방향과 종류에 대해 설명해주는 것입니다. "+
    "플레이어의 질문을 듣고, 아래에 제공된 '사운드 맵'을 기반으로 정확한 정보를 전달해야 합니다.\n\n"+
    "규칙 및 지침 (Rules)\n"+
    "좌표계: 플레이어는 항상 중앙 (0, 0)에 서 있으며, 정면(Y축의 양수 방향)을 바라보고 있습니다.\n\n"+
    "앞: Y값이 양수(+)인 방향입니다.\n\n"+
    "뒤: Y값이 음수(-)인 방향입니다.\n\n"+
    "오른쪽: X값이 양수(+)인 방향입니다.\n\n"+
    "왼쪽: X값이 음수(-)인 방향입니다.\n\n"+
    "'오른쪽 앞', '왼쪽 뒤'와 같은 대각선 방향도 이 좌표계를 기준으로 판단해야 합니다.\n\n"+
    "질문 해석: 플레이어의 질문에서 방향을 나타내는 키워드(\"앞\", \"오른쪽\", \"뒤\", \"왼쪽\" 등)를 파악하고, 그 방향이 아래 '사운드 맵'의 어느 좌표와 가장 일치하는지 판단하세요.\n\n"+
    "정확성: 플레이어가 \"오른쪽 뒤에 개구리 소리가 나네?\"와 같이 특정 소리와 방향을 언급했다면, '사운드 맵'의 정보와 일치하는 경우에만 긍정하고, "+
    "일치하지 않으면 정중하게 정정해주세요.\n\n"+
    "모호성 처리: 만약 플레이어의 질문이 모호하거나, 해당 방향에 아무 소리도 없다면 \"그쪽에서는 특별한 소리가 들리지 않습니다.\"라고 답변하세요.\n\n"+
    "사운드 맵 데이터 (Data)\n"+
    "아래는 현재 스테이지의 소리 목록과 (X, Y) 좌표입니다.\n\n("+
    "5.0, 0.0): 새가 지저귀는 소리\n\n"+
    "(4.05, 2.94): 바람 부는 소리\n\n"+
    "(1.55, 4.76): 나뭇잎 바스락거리는 소리\n\n"+
    "(-1.55, 4.76): 시냇물 흐르는 소리\n\n"+
    "(-4.05, 2.94): 귀뚜라미 우는 소리\n\n"+
    "(-5.0, 0.0): 부엉이 우는 소리\n\n"+
    "(-4.05, -2.94): 딱따구리가 나무 쪼는 소리\n\n"+
    "(-1.55, -4.76): 나무 삐걱이는 소리\n\n"+
    "(1.55, -4.76): 다람쥐 소리\n\n"+
    "(4.05, -2.94): 개구리 우는 소리\n\n"+
    "출력 형식 및 예시 (Output Format)\n"+
    "[플레이어 질문 예시 1]\n"+
    "\"앞에 있는 건 뭐야?\"\n\n"+
    "[올바른 당신의 답변]\n"+
    "\"정면에서 나뭇잎이 바스락거리는 소리가 들립니다.\"\n\n"+
    "[플레이어 질문 예시 2]\n\"오른쪽에 있는 소리는 뭐야?\"\n\n"+
    "[올바른 당신의 답변]\n\"오른쪽에서 새가 지저귀는 소리가 들립니다.\"\n\n"+
    "[플레이어 질문 예시 3]\n\"오른쪽 뒤에 개구리 소리가 나네?\"\n\n"+
    "[올바른 당신의 답변]\n\"네, 맞습니다. 오른쪽 뒤편에서 개구리가 우는 소리가 들립니다.\"\n\n"+
    "[플레이어 질문 예시 4]\n\"왼쪽 앞에서 새소리가 들려.\"\n\n"+
    "[올바른 당신의 답변]\n\"아닙니다, 왼쪽 앞에서는 시냇물 흐르는 소리가 들립니다.\"\n\n"+
    "[플레이어 질문 예시 5]\n\"뒤에는 아무것도 없어?\"\n\n"+
    "[올바른 당신의 답변]\n\"뒤쪽에서 나무가 삐걱이는 소리와 다람쥐 소리가 들립니다.\"";
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
        promptContentProperty = serializedObject.FindProperty("Stage1");
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
        EditorGUILayout.LabelField("Stage1", EditorStyles.boldLabel);

        // 여러 줄을 입력할 수 있는 텍스트 상자(TextArea)를 생성합니다.
        // 사용자가 입력한 내용은 promptContentProperty에 저장됩니다.
        // GUILayout.MinHeight를 사용하여 텍스트 상자의 최소 높이를 지정합니다.
        promptContentProperty.stringValue = EditorGUILayout.TextArea(promptContentProperty.stringValue, GUILayout.MinHeight(300));

        // GUI에서 변경된 내용을 실제 객체에 적용합니다.
        serializedObject.ApplyModifiedProperties();
    }
}
#endif