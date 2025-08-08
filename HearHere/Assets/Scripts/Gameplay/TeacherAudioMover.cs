using UnityEngine;

namespace HH
{
    /// <summary>
    /// 선생님 오디오만 좌우로 움직이게 하는 간단한 스크립트
    /// </summary>
    public class TeacherAudioMover : MonoBehaviour
    {
        [Header("Teacher Audio Settings")]
        public AudioSource teacherAudioSource; // Inspector에서 teacher GameObject의 AudioSource 할당
        
        [Header("Movement Settings")]
        public float moveSpeed = 0.8f;       // 이동 속도 (더 느리게 조정)
        public float moveRange = 4f;         // 좌우 이동 범위
        public float pauseTime = 4f;         // 좌우 끝에서 멈춰있는 시간 (초)
        public bool enableMovement = true;   // 움직임 활성화/비활성화
        
        [Header("Audio Settings")]
        public bool setup3DAudio = true;     // 3D 오디오 자동 설정
        
        // 내부 변수들
        private Vector3 originalPosition;    // 원래 위치 저장
        private float timeOffset;            // 랜덤 시작 시간
        
        void Start()
        {
            InitializeTeacherAudio();
        }
        
        /// <summary>
        /// 선생님 오디오 초기화
        /// </summary>
        void InitializeTeacherAudio()
        {
            // AudioSource가 할당되지 않았으면 자동으로 찾기
            if (teacherAudioSource == null)
            {
                GameObject teacherObject = GameObject.Find("teacher");
                if (teacherObject != null)
                {
                    teacherAudioSource = teacherObject.GetComponent<AudioSource>();
                    Debug.Log("[TeacherMover] teacher GameObject를 자동으로 찾았습니다!");
                }
                else
                {
                    Debug.LogError("[TeacherMover] teacher GameObject를 찾을 수 없습니다!");
                    return;
                }
            }
            
            if (teacherAudioSource != null)
            {
                // 원래 위치 저장
                originalPosition = teacherAudioSource.transform.position;
                
                // 랜덤 시작 시간 설정 (자연스러운 움직임을 위해)
                timeOffset = Random.Range(0f, 2f * Mathf.PI);
                
                // 3D 오디오 설정
                if (setup3DAudio)
                {
                    Setup3DAudioSettings();
                }
                
                Debug.Log($"[TeacherMover] 선생님 오디오 초기화 완료! 원래 위치: {originalPosition}");
            }
        }
        
        /// <summary>
        /// 3D 오디오 설정
        /// </summary>
        void Setup3DAudioSettings()
        {
            if (teacherAudioSource == null) return;
            
            // 3D 사운드 설정
            teacherAudioSource.spatialBlend = 1.0f;           // 완전한 3D 사운드
            teacherAudioSource.rolloffMode = AudioRolloffMode.Linear;
            teacherAudioSource.minDistance = 1f;             // 최소 거리
            teacherAudioSource.maxDistance = 15f;            // 최대 거리
            teacherAudioSource.dopplerLevel = 0.3f;          // 도플러 효과 (약간만)
            
            Debug.Log("[TeacherMover] 3D 오디오 설정 완료");
        }
        
        void Update()
        {
            // 움직임이 활성화되어 있고 AudioSource가 있을 때만 이동
            if (enableMovement && teacherAudioSource != null)
            {
                MoveTeacherLeftRight();
            }
        }
        
        /// <summary>
        /// 선생님을 좌우로 이동시키는 함수
        /// </summary>
        void MoveTeacherLeftRight()
        {
            // 시간 기반 사인파를 이용한 부드러운 좌우 이동
            float time = Time.time * moveSpeed + timeOffset;
            float horizontalOffset = Mathf.Sin(time) * moveRange;
            
            // 새로운 위치 계산 (X축만 변경, Y와 Z는 원래 위치 유지)
            Vector3 newPosition = new Vector3(
                originalPosition.x + horizontalOffset,
                originalPosition.y,
                originalPosition.z
            );
            
            // 위치 업데이트
            teacherAudioSource.transform.position = newPosition;
        }
        
        /// <summary>
        /// 움직임 시작
        /// </summary>
        public void StartMovement()
        {
            enableMovement = true;
            
            // 오디오가 재생되고 있지 않으면 재생
            if (teacherAudioSource != null && !teacherAudioSource.isPlaying)
            {
                teacherAudioSource.Play();
            }
            
            Debug.Log("[TeacherMover] 선생님 움직임 시작!");
        }
        
        /// <summary>
        /// 움직임 정지
        /// </summary>
        public void StopMovement()
        {
            enableMovement = false;
            
            // 원래 위치로 복원
            if (teacherAudioSource != null)
            {
                teacherAudioSource.transform.position = originalPosition;
            }
            
            Debug.Log("[TeacherMover] 선생님 움직임 정지!");
        }
        
        /// <summary>
        /// 선생님이 플레이어의 어느 쪽에 있는지 확인
        /// </summary>
        public string GetTeacherDirection()
        {
            if (teacherAudioSource == null) return "unknown";
            
            // AudioListener(플레이어) 찾기
            AudioListener listener = FindObjectOfType<AudioListener>();
            if (listener == null) return "unknown";
            
            // 플레이어에서 선생님으로의 방향 벡터
            Vector3 directionToTeacher = teacherAudioSource.transform.position - listener.transform.position;
            
            // 플레이어의 오른쪽 벡터와 내적 계산
            float dotProduct = Vector3.Dot(listener.transform.right, directionToTeacher.normalized);
            
            // 방향 판정 (임계값: 0.1)
            if (dotProduct > 0.1f)
                return "right";
            else if (dotProduct < -0.1f)
                return "left";
            else
                return "center";
        }
        
        /// <summary>
        /// 현재 선생님과 플레이어 사이의 거리
        /// </summary>
        public float GetDistanceToPlayer()
        {
            if (teacherAudioSource == null) return -1f;
            
            AudioListener listener = FindObjectOfType<AudioListener>();
            if (listener == null) return -1f;
            
            return Vector3.Distance(teacherAudioSource.transform.position, listener.transform.position);
        }
        
        /// <summary>
        /// 디버그 정보 표시 (게임 실행 중 화면에 표시)
        /// </summary>
        void OnGUI()
        {
            if (!Application.isPlaying || teacherAudioSource == null) return;
            
            // 화면 오른쪽 상단에 디버그 정보 표시
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 200));
            
            GUILayout.Label("=== Teacher Audio Debug ===");
            GUILayout.Label($"움직임 활성화: {enableMovement}");
            GUILayout.Label($"오디오 재생 중: {teacherAudioSource.isPlaying}");
            GUILayout.Label($"현재 방향: {GetTeacherDirection()}");
            GUILayout.Label($"플레이어와 거리: {GetDistanceToPlayer():F1}m");
            
            GUILayout.Space(10);
            
            // 제어 버튼들
            if (enableMovement)
            {
                if (GUILayout.Button("움직임 정지"))
                {
                    StopMovement();
                }
            }
            else
            {
                if (GUILayout.Button("움직임 시작"))
                {
                    StartMovement();
                }
            }
            
            if (GUILayout.Button("오디오 재생/정지"))
            {
                if (teacherAudioSource.isPlaying)
                    teacherAudioSource.Stop();
                else
                    teacherAudioSource.Play();
            }
            
            // 설정 조절
            GUILayout.Label("이동 속도:");
            moveSpeed = GUILayout.HorizontalSlider(moveSpeed, 0.5f, 5f);
            
            GUILayout.Label("이동 범위:");
            moveRange = GUILayout.HorizontalSlider(moveRange, 1f, 10f);
            
            GUILayout.EndArea();
        }
    }
}