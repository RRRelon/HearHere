#!/usr/bin/env python3
"""
음성 채팅 백엔드 테스트 스크립트
음성 파일을 서버에 전송하여 STT → OpenAI API → TTS 플로우를 테스트.
"""

import requests
import json
import os
import time
from pathlib import Path

# 서버 설정
SERVER_URL = "http://localhost:8000"


def test_server_health():
    """서버 상태 확인"""
    try:
        response = requests.get(f"{SERVER_URL}/health")
        if response.status_code == 200:
            health_data = response.json()
            print("서버 상태:")
            print(f"   - 상태: {health_data.get('status')}")
            print(f"   - OpenAI 설정: {health_data.get('openai_configured')}")
            print(
                f"   - Chatterbox 로드: {health_data.get('chatterbox_loaded')}")
            print(f"   - 디바이스: {health_data.get('device')}")
            return True
        else:
            print(f"서버 상태 확인 실패: {response.status_code}")
            return False
    except Exception as e:
        print(f"서버 연결 실패: {e}")
        return False


def test_voice_chat(audio_file_path):
    """음성 채팅 테스트"""
    if not os.path.exists(audio_file_path):
        print(f"음성 파일을 찾을 수 없습니다: {audio_file_path}")
        return False

    print(f"음성 채팅 테스트 시작: {audio_file_path}")

    try:
        # 음성 파일을 서버로 전송
        with open(audio_file_path, 'rb') as f:
            files = {'file': f}
            data = {
                'exaggeration': 0.7,  # 감정 강도
                'cfg_weight': 0.5     # 품질 조절
            }

            print("서버로 음성 파일 전송 중...")
            response = requests.post(
                f"{SERVER_URL}/voice-chat", files=files, data=data)

        if response.status_code == 200:
            result = response.json()
            if result.get('success'):
                print("음성 채팅 성공!")
                print(f"   - 사용자 음성 텍스트: {result.get('user_text')}")
                print(f"   - AI 응답: {result.get('ai_response')}")
                print(f"   - 대화 ID: {result.get('conversation_id')}")
                print(f"   - 음성 파일 ID: {result.get('audio_file_id')}")

                # AI 응답 음성 다운로드
                download_url = result.get('download_url')
                if download_url:
                    download_ai_response(
                        download_url, result.get('audio_file_id'))

                return True
            else:
                print(f"음성 채팅 실패: {result}")
                return False
        else:
            print(f"서버 요청 실패: {response.status_code}")
            print(f"   응답: {response.text}")
            return False

    except Exception as e:
        print(f"음성 채팅 테스트 실패: {e}")
        return False


def download_ai_response(download_url, file_id):
    """AI 응답 음성 다운로드"""
    try:
        print("AI 응답 음성 다운로드 중...")
        response = requests.get(f"{SERVER_URL}{download_url}")

        if response.status_code == 200:
            output_path = f"ai_response_{file_id}.wav"
            with open(output_path, 'wb') as f:
                f.write(response.content)
            print(f"AI 응답 음성 저장됨: {output_path}")
            return True
        else:
            print(f"음성 다운로드 실패: {response.status_code}")
            return False

    except Exception as e:
        print(f"음성 다운로드 실패: {e}")
        return False


def test_individual_endpoints():
    """개별 엔드포인트 테스트"""
    print("\n개별 엔드포인트 테스트")

    # TTS 테스트
    print("\n1. TTS 테스트")
    try:
        tts_data = {
            "text": "안녕하세요! 백엔드 TTS 테스트입니다.",
            "exaggeration": 0.6,
            "cfg_weight": 0.5
        }
        response = requests.post(f"{SERVER_URL}/tts", json=tts_data)

        if response.status_code == 200:
            result = response.json()
            print(f"TTS 성공: {result.get('download_url')}")
        else:
            print(f"TTS 실패: {response.status_code}")

    except Exception as e:
        print(f"TTS 테스트 실패: {e}")

    # 채팅 테스트
    print("\n2. 채팅 테스트")
    try:
        chat_data = {
            "message": "안녕하세요! 백엔드 테스트 중입니다."
        }
        response = requests.post(f"{SERVER_URL}/chat", json=chat_data)

        if response.status_code == 200:
            result = response.json()
            print(f"채팅 성공: {result.get('response')}")
        else:
            print(f"채팅 실패: {response.status_code}")

    except Exception as e:
        print(f"채팅 테스트 실패: {e}")


def create_sample_audio():
    """테스트용 샘플 음성 파일 안내"""
    print("\n🎵 테스트용 음성 파일 준비:")
    print("1. 마이크로 간단한 음성 메시지 녹음 (예: '안녕하세요')")
    print("2. 파일을 test_audio.wav로 저장")
    print("3. 이 스크립트와 같은 폴더에 배치")
    print("\n또는 기존 음성 파일 경로를 직접 입력하세요.")


def main():
    print("Unity TTS/STT 백엔드 테스트")
    print("=" * 50)

    # 1. 서버 상태 확인
    if not test_server_health():
        print("\n서버가 실행되지 않았거나 문제가 있습니다.")
        print("다음 명령어로 서버를 먼저 실행하세요:")
        print("python start_server.py")
        return

    # 2. 음성 파일 확인
    test_audio_files = [
        "test_audio.wav",
        "sample.wav",
        "voice.wav",
        "audio.wav"
    ]

    audio_file = None
    for file_path in test_audio_files:
        if os.path.exists(file_path):
            audio_file = file_path
            break

    if audio_file:
        print(f"\n테스트 음성 파일 발견: {audio_file}")
        # 3. 음성 채팅 테스트
        test_voice_chat(audio_file)
    else:
        print(f"\n테스트용 음성 파일을 찾을 수 없습니다.")
        create_sample_audio()

        # 사용자 입력 받기
        custom_path = input("\n음성 파일 경로를 입력하세요 (엔터로 건너뛰기): ").strip()
        if custom_path and os.path.exists(custom_path):
            test_voice_chat(custom_path)
        else:
            print("음성 채팅 테스트를 건너뜁니다.")

    # 4. 개별 엔드포인트 테스트
    test_individual_endpoints()

    print(f"\n테스트 완료!")
    print(f"서버 API 문서: {SERVER_URL}/docs")


if __name__ == "__main__":
    main()
