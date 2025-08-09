using UnityEngine;

public static class GameSessionManager  
{
    // 앱이 처음 메모리에 로드될 때 단 한 번 true로 초기화
    public static bool IsFirstLaunchOfSession { get; private set; } = true;

    /// <summary>
    /// 첫 실행 관련 로직을 처리한 후, 이 함수를 호출하여
    /// 다음부터는 첫 실행이 아님을 표시
    /// </summary>
    public static void CompleteFirstLaunch()
    {
        IsFirstLaunchOfSession = false;
    }
}
