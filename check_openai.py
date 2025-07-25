import os
from openai import OpenAI
from dotenv import load_dotenv

# .env 파일에서 환경 변수 로드
load_dotenv()

print("OpenAI API 키 직접 테스트를 시작합니다...")

# 1. API 키 로드 확인
api_key = os.getenv("OPENAI_API_KEY")
if not api_key:
    print("오류: .env 파일에서 OPENAI_API_KEY를 찾을 수 없습니다.")
    exit()

print("API 키를 성공적으로 로드했습니다.")
print("이제 이 키로 OpenAI 서버에 직접 요청을 보냅니다...")

try:
    # 2. OpenAI 클라이언트 초기화
    client = OpenAI(api_key=api_key)

    # 3. 간단한 채팅 API 호출
    response = client.chat.completions.create(
        model="gpt-3.5-turbo",
        messages=[
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": "Hello!"}
        ]
    )

    # 4. 성공적인 응답 출력
    print("\n✅ 테스트 성공! API 키가 정상적으로 작동합니다.")
    print(f"AI 응답: {response.choices[0].message.content}")

except Exception as e:
    # 5. 실패 시 에러 메시지 출력
    print("\n❌ 테스트 실패! API 키 또는 계정에 문제가 있습니다.")
    print("아래의 에러 메시지를 확인하세요:")
    print("-" * 50)
    print(e)
    print("-" * 50)

