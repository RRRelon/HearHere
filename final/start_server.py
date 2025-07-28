#!/usr/bin/env python3
"""
스마트 음성 채팅 서버 시작 스크립트
VectorDB 기반 자동 학습 시스템과 함께 실행됩니다.
"""

import os
import sys
import subprocess
import platform
from pathlib import Path
from dotenv import load_dotenv

# .env 파일 로드
load_dotenv()


def check_python_version():
    """Python 버전 확인"""
    if sys.version_info < (3, 8):
        print("Python 3.8 이상이 필요합니다.")
        print(f"현재 버전: {sys.version}")
        return False

    print(f"Python 버전 확인: {sys.version.split()[0]}")
    return True


def check_dependencies():
    """필요한 패키지들이 설치되어 있는지 확인"""
    # 패키지명과 실제 import명 매핑
    package_imports = {
        "fastapi": "fastapi",
        "uvicorn": "uvicorn",
        "openai": "openai",
        "python-multipart": "multipart",
        "python-dotenv": "dotenv",
        "chatterbox-tts": "chatterbox",
        "chromadb": "chromadb",
        "sentence-transformers": "sentence_transformers",
        "soundfile": "soundfile"
    }

    missing_packages = []

    for package_name, import_name in package_imports.items():
        try:
            __import__(import_name)
            print(f"{package_name}")
        except ImportError:
            missing_packages.append(package_name)
            print(f"{package_name} (누락)")

    if missing_packages:
        print(f"\n누락된 패키지들: {', '.join(missing_packages)}")
        print("다음 명령어로 설치하세요:")
        print(f"pip install {' '.join(missing_packages)}")
        return False

    print("모든 필수 패키지가 설치되어 있습니다!")
    return True


def check_openai_api_key():
    """OpenAI API 키 확인"""
    api_key = os.getenv("OPENAI_API_KEY")

    if not api_key:
        print("OPENAI_API_KEY 환경 변수가 설정되지 않았습니다!")
        print("\n설정 방법:")
        print("1. .env 파일 생성:")
        print("   echo 'OPENAI_API_KEY=your-api-key-here' > .env")
        print("2. 또는 환경 변수 설정:")

        if platform.system() == "Windows":
            print("   set OPENAI_API_KEY=your-api-key-here")
        else:
            print("   export OPENAI_API_KEY=your-api-key-here")

        return False

    print(f"OpenAI API 키 확인: {api_key[:8]}...")
    return True


def setup_directories():
    """필요한 디렉토리 생성"""
    directories = [
        "chroma_db",
        "temp_files",
        "__pycache__"
    ]

    for dir_name in directories:
        dir_path = Path(dir_name)
        if not dir_path.exists():
            dir_path.mkdir(exist_ok=True)
            print(f"디렉토리 생성: {dir_name}")
        else:
            print(f"디렉토리 확인: {dir_name}")


def show_startup_info():
    """시작 정보 표시"""
    print("\n" + "="*60)
    print("스마트 음성 채팅 서버 v2.0")
    print("="*60)
    print("VectorDB 기반 자동 학습 시스템")
    print("GPT-3.5-turbo + Chatterbox TTS")
    print("실시간 응답 캐싱 및 학습")
    print("Unity 게임 연동 지원")
    print("="*60)


def show_server_urls():
    """서버 URL 정보 표시"""
    print("\n서버 정보:")
    print("- 메인 서버: http://localhost:8000")
    print("- API 문서: http://localhost:8000/docs")
    print("- 서버 상태: http://localhost:8000/health")
    print("- 시스템 통계: http://localhost:8000/stats")


def show_endpoints():
    """주요 엔드포인트 정보 표시"""
    print("\n주요 엔드포인트:")
    print("- POST /voice-chat    : 음성 채팅 (STT→AI→TTS)")
    print("- POST /chat         : 텍스트 채팅만")
    print("- POST /tts          : TTS만")
    print("- POST /stt          : STT만")
    print("- GET  /stats        : 시스템 통계")
    print("- POST /settings     : 설정 변경")


def show_learning_info():
    """학습 시스템 정보 표시"""
    print("\n자동 학습 시스템:")
    print("- 모든 GPT 응답이 자동으로 VectorDB에 저장됩니다")
    print("- 유사한 질문이 들어오면 즉시 캐시된 응답을 제공합니다")
    print("- 시간이 지날수록 응답 속도가 빨라집니다")
    print("- 캐시 적중률을 /stats에서 확인할 수 있습니다")


def run_server():
    """서버 실행"""
    try:
        print("\n서버 시작 중...")
        print("Ctrl+C로 서버를 중지할 수 있습니다")
        print("\n" + "-"*60)

        # uvicorn으로 서버 실행
        subprocess.run([
            sys.executable, "-m", "uvicorn",
            "back:app",
            "--host", "0.0.0.0",
            "--port", "8000",
            "--reload"
        ], check=True)

    except KeyboardInterrupt:
        print("\n\n서버가 중지되었습니다.")
    except subprocess.CalledProcessError as e:
        print(f"\n서버 실행 실패: {e}")
        print("back.py 파일이 현재 디렉토리에 있는지 확인해주세요.")
    except Exception as e:
        print(f"\n예상치 못한 오류: {e}")


def main():
    """메인 함수"""
    show_startup_info()

    print("\n시스템 검사 중...")

    # 시스템 검사
    if not check_python_version():
        return

    if not check_dependencies():
        print("\n패키지 설치 후 다시 실행해주세요:")
        print("pip install -r requirements.txt")
        return

    if not check_openai_api_key():
        return

    # 디렉토리 설정
    setup_directories()

    # 정보 표시
    show_server_urls()
    show_endpoints()
    show_learning_info()

    print("\n모든 검사 완료! 서버를 시작합니다...")

    # 서버 실행
    run_server()


if __name__ == "__main__":
    main()
