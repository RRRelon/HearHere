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
