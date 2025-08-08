using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HH
{
    /// <summary>
    /// 플레이어를 중심으로 여러 소리가 뛰어다니는 시스템
    /// </summary>
    public class RunningAroundPlayerSystem : MonoBehaviour
    {
        [Header("Player Reference")]
        public Transform playerTransform; // 플레이어 위치 (AudioListener)
        
        [Header("Running Sound Settings")]
        public List<RunningSoundSource> runningSounds = new List<RunningSoundSource>();
        
        [Header("Movement Settings")]
        public float runningSpeed = 3f;        // 뛰어다니는 속도
        public float minDistance = 2f;         // 플레이어로부터 최소 거리
        public float maxDistance = 8f;         // 플레이어로부터 최대 거리
        public int numberOfRunners = 1;        // 뛰어다니는 소리 개수 (running만)
        
        [Header("Advanced Settings")]
        public bool randomizeSpeed = true;     // 속도 랜덤화
        public float speedVariation = 1f;      // 속도 변화 범위
        public bool avoidCollisions = true;    // 서로 충돌 방지
        public float collisionAvoidDistance = 1.5f; // 충돌 회피 거리
        
        [System.Serializable]
        public class RunningSoundSource
        {
            public string name;
            public AudioSource audioSource;
            public PatternType movementPattern;
            public float currentSpeed;
            public Vector3 targetPosition;
            public Vector3 currentVelocity;
            public float patternTime;
            public bool isActive = true;
            
            [HideInInspector] public float timeOffset;
            [HideInInspector] public Vector3 lastDirection;
        }
        
        public enum PatternType
        {
            Circle,          // 원형 패턴
            Random,          // 랜덤 이동
            Figure8,         // 8자 패턴
            Spiral,          // 나선형 패턴
            Zigzag          // 지그재그 패턴
        }
        
        void Start()
        {
            FindPlayerTransform();
            SetupRunningSounds();
            InitializeRunners();
        }
        
        /// <summary>
        /// 플레이어 Transform 찾기
        /// </summary>
        void FindPlayerTransform()
        {
            if (playerTransform == null)
            {
                // AudioListener 찾기
                AudioListener listener = FindObjectOfType<AudioListener>();
                if (listener != null)
                {
                    playerTransform = listener.transform;
                    Debug.Log($"[RunningSystem] AudioListener 발견: {listener.name}");
                }
                else
                {
                    // Main Camera 사용
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        playerTransform = mainCamera.transform;
                        Debug.LogWarning("[RunningSystem] AudioListener가 없어 Main Camera 사용");
                    }
                }
            }
        }
        
        /// <summary>
        /// "running" AudioSource만 찾아서 뛰어다니는 소리로 설정
        /// </summary>
        void SetupRunningSounds()
        {
            // 기존에 설정된 소리가 없으면 "running"만 찾기
            if (runningSounds.Count == 0)
            {
                // "running" AudioSource만 찾기
                GameObject runningObj = GameObject.Find("running");
                if (runningObj != null)
                {
                    AudioSource audioSource = runningObj.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        RunningSoundSource runningSound = new RunningSoundSource
                        {
                            name = "running",
                            audioSource = audioSource,
                            movementPattern = PatternType.Random, // 자연스러운 랜덤 이동
                            currentSpeed = runningSpeed
                        };
                        runningSounds.Add(runningSound);
                        Debug.Log("[RunningSystem] 'running' AudioSource만 추가됨! 다른 소리들은 고정 위치 유지.");
                    }
                    else
                    {
                        Debug.LogError("[RunningSystem] 'running' GameObject에 AudioSource가 없습니다!");
                    }
                }
                else
                {
                    Debug.LogError("[RunningSystem] 'running' GameObject를 찾을 수 없습니다!");
                    return;
                }
            }
            
            Debug.Log($"[RunningSystem] 총 {runningSounds.Count}개의 러너 설정됨 (running만 움직임)");
        }
        
        /// <summary>
        /// 추가 러너 생성
        /// </summary>
        void CreateAdditionalRunner(int index)
        {
            // 새 GameObject 생성
            GameObject runnerObj = new GameObject($"Runner_{index + 1}");
            AudioSource audioSource = runnerObj.AddComponent<AudioSource>();
            
            // 3D 오디오 설정
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 15f;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            
            // 기본 오디오 클립 설정 (있다면)
            // audioSource.clip = defaultRunningClip; // 필요시 설정
            
            RunningSoundSource runningSound = new RunningSoundSource
            {
                name = $"Runner_{index + 1}",
                audioSource = audioSource,
                movementPattern = GetRandomPattern(),
                currentSpeed = runningSpeed
            };
            
            runningSounds.Add(runningSound);
            Debug.Log($"[RunningSystem] 추가 러너 생성: {runningSound.name}");
        }
        
        /// <summary>
        /// 랜덤 움직임 패턴 반환
        /// </summary>
        PatternType GetRandomPattern()
        {
            PatternType[] patterns = {PatternType.Circle, PatternType.Random, PatternType.Figure8, PatternType.Spiral, PatternType.Zigzag};
            return patterns[Random.Range(0, patterns.Length)];
        }
        
        /// <summary>
        /// 러너들 초기화
        /// </summary>
        void InitializeRunners()
        {
            for (int i = 0; i < runningSounds.Count; i++)
            {
                var runner = runningSounds[i];
                
                // 랜덤 시작 위치 설정 (플레이어 주변)
                float angle = (360f / runningSounds.Count) * i; // 균등 분배
                float distance = Random.Range(minDistance, maxDistance);
                
                Vector3 startPos = playerTransform.position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * distance
                );
                
                runner.audioSource.transform.position = startPos;
                runner.targetPosition = startPos;
                runner.timeOffset = Random.Range(0f, 2f * Mathf.PI);
                
                // 속도 랜덤화
                if (randomizeSpeed)
                {
                    runner.currentSpeed = runningSpeed + Random.Range(-speedVariation, speedVariation);
                }
                
                // 3D 오디오 설정
                Setup3DAudio(runner.audioSource);
                
                Debug.Log($"[RunningSystem] {runner.name} 초기화 완료 - 패턴: {runner.movementPattern}");
            }
        }
        
        /// <summary>
        /// 3D 오디오 설정
        /// </summary>
        void Setup3DAudio(AudioSource audioSource)
        {
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.dopplerLevel = 0.8f; // 도플러 효과 강화 (뛰어다니는 느낌)
        }
        
        void Update()
        {
            if (playerTransform == null) return;
            
            foreach (var runner in runningSounds)
            {
                if (runner.isActive && runner.audioSource != null)
                {
                    UpdateRunnerMovement(runner);
                }
            }
        }
        
        /// <summary>
        /// 러너 움직임 업데이트
        /// </summary>
        void UpdateRunnerMovement(RunningSoundSource runner)
        {
            runner.patternTime += Time.deltaTime * runner.currentSpeed;
            Vector3 newTargetPosition = CalculatePatternPosition(runner);
            
            // 충돌 회피
            if (avoidCollisions)
            {
                newTargetPosition = AvoidCollisions(runner, newTargetPosition);
            }
            
            // 플레이어 거리 제한
            newTargetPosition = ClampToPlayerDistance(newTargetPosition);
            
            // 부드러운 이동
            runner.audioSource.transform.position = Vector3.SmoothDamp(
                runner.audioSource.transform.position,
                newTargetPosition,
                ref runner.currentVelocity,
                0.3f
            );
            
            runner.targetPosition = newTargetPosition;
        }
        
        /// <summary>
        /// 패턴에 따른 위치 계산
        /// </summary>
        Vector3 CalculatePatternPosition(RunningSoundSource runner)
        {
            Vector3 playerPos = playerTransform.position;
            float time = runner.patternTime + runner.timeOffset;
            Vector3 offset = Vector3.zero;
            
            switch (runner.movementPattern)
            {
                case PatternType.Circle:
                    float radius = (minDistance + maxDistance) * 0.5f;
                    offset = new Vector3(
                        Mathf.Cos(time) * radius,
                        0f,
                        Mathf.Sin(time) * radius
                    );
                    break;
                    
                case PatternType.Random:
                    // Perlin Noise를 이용한 자연스러운 랜덤 이동
                    float scale = 0.5f;
                    offset = new Vector3(
                        (Mathf.PerlinNoise(time * scale, 0) - 0.5f) * maxDistance * 2f,
                        0f,
                        (Mathf.PerlinNoise(0, time * scale) - 0.5f) * maxDistance * 2f
                    );
                    break;
                    
                case PatternType.Figure8:
                    float radius8 = (minDistance + maxDistance) * 0.4f;
                    offset = new Vector3(
                        Mathf.Sin(time) * radius8,
                        0f,
                        Mathf.Sin(time * 2) * radius8
                    );
                    break;
                    
                case PatternType.Spiral:
                    float spiralRadius = minDistance + (Mathf.Sin(time * 0.3f) + 1) * (maxDistance - minDistance) * 0.5f;
                    offset = new Vector3(
                        Mathf.Cos(time) * spiralRadius,
                        0f,
                        Mathf.Sin(time) * spiralRadius
                    );
                    break;
                    
                case PatternType.Zigzag:
                    float zigzagRadius = (minDistance + maxDistance) * 0.5f;
                    offset = new Vector3(
                        Mathf.PingPong(time, 1f) * zigzagRadius * 2f - zigzagRadius,
                        0f,
                        Mathf.Sin(time * 2f) * zigzagRadius
                    );
                    break;
            }
            
            return playerPos + offset;
        }
        
        /// <summary>
        /// 다른 러너들과의 충돌 회피
        /// </summary>
        Vector3 AvoidCollisions(RunningSoundSource currentRunner, Vector3 targetPos)
        {
            Vector3 avoidanceForce = Vector3.zero;
            
            foreach (var otherRunner in runningSounds)
            {
                if (otherRunner == currentRunner || !otherRunner.isActive) continue;
                
                Vector3 toOther = otherRunner.audioSource.transform.position - targetPos;
                float distance = toOther.magnitude;
                
                if (distance < collisionAvoidDistance && distance > 0.1f)
                {
                    // 다른 러너로부터 멀어지는 힘 추가
                    avoidanceForce -= toOther.normalized * (collisionAvoidDistance - distance);
                }
            }
            
            return targetPos + avoidanceForce;
        }
        
        /// <summary>
        /// 플레이어 거리 제한
        /// </summary>
        Vector3 ClampToPlayerDistance(Vector3 position)
        {
            Vector3 toPosition = position - playerTransform.position;
            float distance = toPosition.magnitude;
            
            if (distance < minDistance)
            {
                return playerTransform.position + toPosition.normalized * minDistance;
            }
            else if (distance > maxDistance)
            {
                return playerTransform.position + toPosition.normalized * maxDistance;
            }
            
            return position;
        }
        
        /// <summary>
        /// 모든 러너 시작
        /// </summary>
        public void StartAllRunners()
        {
            foreach (var runner in runningSounds)
            {
                if (runner.audioSource != null)
                {
                    runner.isActive = true;
                    if (!runner.audioSource.isPlaying)
                    {
                        runner.audioSource.Play();
                    }
                }
            }
            Debug.Log("[RunningSystem] 모든 러너 시작!");
        }
        
        /// <summary>
        /// 모든 러너 정지
        /// </summary>
        public void StopAllRunners()
        {
            foreach (var runner in runningSounds)
            {
                runner.isActive = false;
                if (runner.audioSource != null)
                {
                    runner.audioSource.Stop();
                }
            }
            Debug.Log("[RunningSystem] 모든 러너 정지!");
        }
        
        /// <summary>
        /// 특정 러너의 방향 확인
        /// </summary>
        public string GetRunnerDirection(string runnerName)
        {
            var runner = runningSounds.Find(r => r.name == runnerName);
            if (runner != null && runner.audioSource != null && playerTransform != null)
            {
                Vector3 directionToRunner = runner.audioSource.transform.position - playerTransform.position;
                float dotProduct = Vector3.Dot(playerTransform.right, directionToRunner.normalized);
                
                if (dotProduct > 0.3f)
                    return "right";
                else if (dotProduct < -0.3f)
                    return "left";
                else
                    return "center";
            }
            return "unknown";
        }
        
        /// <summary>
        /// 디버그 정보 표시 (running만 표시)
        /// </summary>
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 300, 300, 250));
            GUILayout.Label("=== Running Sound Control ===");
            
            // running 소리만 표시
            if (runningSounds.Count > 0 && runningSounds[0].audioSource != null)
            {
                var runner = runningSounds[0];
                string direction = GetRunnerDirection(runner.name);
                float distance = Vector3.Distance(runner.audioSource.transform.position, playerTransform.position);
                
                GUILayout.Label($"🏃 {runner.name}:");
                GUILayout.Label($"  패턴: {runner.movementPattern}");
                GUILayout.Label($"  방향: {direction}");
                GUILayout.Label($"  거리: {distance:F1}m");
                GUILayout.Label($"  활성: {runner.isActive}");
                GUILayout.Label($"  재생 중: {runner.audioSource.isPlaying}");
            }
            else
            {
                GUILayout.Label("❌ 'running' AudioSource를 찾을 수 없음");
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Running 시작"))
            {
                StartAllRunners();
            }
            
            if (GUILayout.Button("Running 정지"))
            {
                StopAllRunners();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("설정:");
            GUILayout.Label("뛰는 속도:");
            runningSpeed = GUILayout.HorizontalSlider(runningSpeed, 1f, 6f);
            
            GUILayout.Label("최소 거리:");
            minDistance = GUILayout.HorizontalSlider(minDistance, 1f, 5f);
            
            GUILayout.Label("최대 거리:");
            maxDistance = GUILayout.HorizontalSlider(maxDistance, 5f, 15f);
            
            GUILayout.Label("📝 다른 소리들(chattering, teacher 등)은 고정 위치");
            
            GUILayout.EndArea();
        }
    }
}