# Hear, Here
ICCAS 2025 시각장애인을 위한 공간 음향 분리 능력 향상 게임 제작

## TAG 정리

| 태그     | 설명                                 |
|---------|---------------------------------------|
| feat    | 새로운 코드 추가                      |
| fix     | 문제점 수정                           |
| refact  | 코드 리팩토링                         |
| comment | 주석 추가(코드 변경X) 혹은 오타 수정  |
| docs    | README와 같은 문서 수정               |
| art     | 아트 에셋 추가                       |
| merge   | merge                                 |
| rename  | 파일, 폴더명 수정 혹은 이동           |
| chore   | 그 외 패키지 추가, 설정 변경 등        |

</br>

## Branch Name Convention

```
(TAG)/(주요내용)/(있다면 ISSUE NUMBER)

ex)
feat/player/#99
chore/package
```

</br>

## Commit Convention
```
(TAG)(있다면 ISSUE NUMBER) : 제목, 이때 영어라면 제일 앞 문자는 대문자로 시작
ex)
feat(#123) : A 기능을 구현하였다.

- A.cs 수정
- 그 외 comment 들

---

chore : A 패키지 추가
```

</br>

## PR Merge Convention

```
title: (TAG)/(ISSUE NUMBER) (PR NUMBER)
ex) FEAT/35 (#40)
```

</br>


## TTS 기능 사용법 (Google Text-to-Speech API)

이 프로젝트에서는 Google TTS(Text-to-Speech) API를 사용하여 입력된 텍스트를 음성으로 변환해 재생합니다.  
다른 팀원이 TTS 기능을 사용하려면 아래의 단계를 따라 주세요:

---

### 1. Google TTS API 키 발급 방법

1. [Google Cloud Console](https://console.cloud.google.com/)에 접속 후 로그인
2. 프로젝트 생성 또는 기존 프로젝트 선택
3. "API 및 서비스" > "라이브러리" > "Cloud Text-to-Speech API" 검색 후 **사용 설정**
4. "API 및 서비스" > "사용자 인증 정보" > **API 키 만들기**
5. 생성된 **API 키 복사**

---

### 2. secret.json 파일 생성

Unity 프로젝트의 `Assets/Resources/` 폴더 안에 `secret.json` 파일을 아래 형식으로 만들어 주세요.

```json
{
  "googleTTSapiKey": "여기에_복사한_API_키를_붙여넣기"
}
```
```
# Unity TTS/STT 백엔드 서버

Unity 게임과 TTS(Text-to-Speech) 및 STT(Speech-to-Text) 기능을 연동하기 위한 백엔드 서버

## 주요 기능

- **TTS (Text-to-Speech)**: Chatterbox를 사용한 감정 조절 가능한 음성 합성
- **STT (Speech-to-Text)**: OpenAI Whisper를 사용한 음성 인식
- **AI 채팅**: OpenAI GPT를 활용한 대화 기능
- **음성 채팅**: 음성 → STT → AI → TTS → 음성의 완전한 플로우
- **Unity 연동**: CORS 설정으로 Unity에서 직접 API 호출 가능

## 설치 방법

### 1. 필요한 패키지 설치

```bash
pip install -r requirements.txt
```

### 2. OpenAI API 키 설정

환경 변수로 OpenAI API 키를 설정하거나 `.env` 파일을 생성

```bash
# 환경 변수 설정
export OPENAI_API_KEY=your-api-key-here

# 또는 .env 파일 생성
echo "OPENAI_API_KEY=your-api-key-here" > .env
```

### 3. 서버 실행

```bash
# 방법 1: 직접 실행(추천)
python3 start_server.py

# 방법 2: uvicorn 직접 실행 (비추천)
uvicorn back:app --host 0.0.0.0 --port 8000 --reload
```

서버가 시작되면 다음 URL에서 확인할 수 있습니다:

- 서버: http://localhost:8000
- API 문서: http://localhost:8000/docs

## API 엔드포인트

### 음성 채팅 (핵심 기능)

```
POST /voice-chat
Content-Type: multipart/form-data

file: 음성 파일 (audio/wav, audio/mp3, audio/m4a, audio/ogg, audio/flac)
conversation_id: 대화 ID (선택사항)
exaggeration: 0.0~1.0 (감정 강도, 기본값: 0.5)
cfg_weight: 0.0~1.0 (생성 품질, 기본값: 0.5)

응답:
{
  "success": true,
  "user_text": "사용자가 말한 내용",
  "ai_response": "AI 응답 텍스트",
  "conversation_id": "대화 ID",
  "audio_file_id": "생성된 음성 파일 ID",
  "download_url": "/download/{file_id}.wav"
}
```

### TTS (Text-to-Speech)

```
POST /tts
Content-Type: application/json

{
  "text": "변환할 텍스트",
  "exaggeration": 0.5,        // 0.0~1.0, 감정 강도
  "cfg_weight": 0.5,          // 0.0~1.0, 생성 품질
  "audio_prompt_path": null   // 음성 복제용 참조 파일 경로
}
```

### STT (Speech-to-Text)

```
POST /stt
Content-Type: multipart/form-data

파일: audio/wav, audio/mp3, audio/m4a, audio/ogg, audio/flac
```

### AI 채팅

```
POST /chat
Content-Type: application/json

{
  "message": "채팅 메시지",
  "conversation_id": "대화 ID (선택사항)"
}
```

### 파일 다운로드

```
GET /download/{file_id}
```

### 서버 상태 확인

```
GET /health
```

## Unity 연동 방법

### 1. Unity 프로젝트에 클라이언트 스크립트 추가

제공된 `UnityTTSSTTClient.cs` 파일을 Unity 프로젝트에 추가하세요.

### 2. 컴포넌트 설정

1. 빈 GameObject를 생성하고 `UnityTTSSTTClient` 스크립트를 추가
2. AudioSource 컴포넌트를 추가하거나 기존 AudioSource를 연결
3. Server URL을 `http://localhost:8000`으로 설정

### 3. 사용 예제

```csharp
public class GameManager : MonoBehaviour
{
    public UnityTTSSTTClient ttsClient;

    void Start()
    {
        // TTS 테스트
        ttsClient.ConvertTextToSpeech("안녕하세요!", 0.7f, 0.5f);

        // 채팅 테스트
        ttsClient.SendChatMessage("안녕하세요!");

        // 음성 채팅 테스트 (마이크 녹음)
        ttsClient.TestVoiceChat();
    }

    // 음성 녹음 후 STT 변환
    public void ProcessRecordedAudio(AudioClip recordedClip)
    {
        ttsClient.ConvertRecordedAudioToText(recordedClip);
    }

    // 음성으로 AI와 대화
    public void TalkToAI()
    {
        ttsClient.StartVoiceRecording(0.8f, 0.3f); // 감정 강도 높게, 품질 조정
    }
}
```

## 프로젝트 구조

```
ICCAS/
├── back.py                    # 백엔드 서버 메인 파일
├── start_server.py           # 서버 실행 스크립트
├── requirements.txt          # Python 패키지 의존성
├── README.md                # 프로젝트 문서
└── temp_files/              # 임시 파일 저장 디렉토리 (자동 생성)
```

## 주요 기능 상세

### 음성 채팅 (핵심 기능)

- **완전한 음성 대화**: 음성 입력 → STT → AI 응답 → TTS → 음성 출력
- **실시간 대화**: 마이크 녹음부터 AI 응답 음성까지 자동 처리
- **감정 조절**: AI 응답 음성의 감정 강도 조절 가능
- **대화 히스토리**: 연속적인 대화 맥락 유지

### TTS (Text-to-Speech)

- **Chatterbox TTS** 사용
- **감정 조절**: exaggeration 파라미터로 감정 강도 조절
- **품질 조절**: cfg_weight 파라미터로 생성 품질 조절
- **음성 복제**: 참조 음성으로 특정 목소리 모방 가능
- WAV 형식으로 음성 파일 생성

### STT (Speech-to-Text)

- **OpenAI Whisper API** 사용
- 다양한 오디오 형식 지원 (WAV, MP3, M4A, OGG, FLAC)
- Unity에서 녹음된 음성 직접 전송 가능

### AI 채팅

- **OpenAI GPT-3.5-turbo** 사용
- 대화 히스토리 관리
- 한국어 대화 최적화
- 게임 맥락에 맞는 응답 생성

### 서버 시작 실패

1. Python 패키지 설치 확인: `pip install -r requirements.txt`
2. OpenAI API 키 설정 확인
3. 포트 8000이 사용 중인지 확인
