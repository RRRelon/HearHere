#!/usr/bin/env python3
"""
Unity TTS/STT 백엔드 서버 실행 스크립트
"""

import os
import sys
import subprocess
from pathlib import Path


def check_requirements():
    """필요한 패키지들이 설치되어 있는지 확인"""
    try:
        import fastapi
        import uvicorn
        import openai
        print("필수 패키지들이 설치되어 있습니다.")
        return True
    except ImportError as e:
        print(f"필수 패키지가 설치되어 있지 않습니다: {e}")
        print("다음 명령어로 필요한 패키지를 설치하세요:")
        print("pip install -r requirements.txt")
        return False


def check_openai_key():
    """OpenAI API 키가 설정되어 있는지 확인"""
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        print("OPENAI_API_KEY가 설정되지 않았습니다.")
        print("환경 변수를 설정하거나 .env 파일을 생성하세요.")
        print("예: export OPENAI_API_KEY=your-api-key-here")
        return False
    print("OpenAI API 키가 설정되어 있습니다.")
    return True


def create_temp_directory():
    """임시 파일 디렉토리 생성"""
    temp_dir = Path("temp_files")
    temp_dir.mkdir(exist_ok=True)
    print(f"임시 파일 디렉토리 생성: {temp_dir}")


def start_server():
    """서버 시작"""
    print("Unity TTS/STT 백엔드 서버를 시작합니다...")
    print("서버 URL: http://localhost:8000")
    print("API 문서: http://localhost:8000/docs")
    print("서버를 중지하려면 Ctrl+C를 누르세요.")
    print("-" * 50)

    try:
        # uvicorn으로 서버 실행
        os.system("uvicorn back:app --host 0.0.0.0 --port 8000 --reload")
    except KeyboardInterrupt:
        print("\n서버가 중지되었습니다.")


def main():
    print("Unity TTS/STT 백엔드 서버")
    print("=" * 30)

    # 요구사항 확인
    if not check_requirements():
        sys.exit(1)

    # OpenAI API 키 확인
    check_openai_key()

    # 임시 디렉토리 생성
    create_temp_directory()

    # 서버 시작
    start_server()


if __name__ == "__main__":
    main()
