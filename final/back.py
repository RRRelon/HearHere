from fastapi import FastAPI, File, UploadFile, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from pydantic import BaseModel
import openai
import os
import uuid
import asyncio
from datetime import datetime
import io
import base64
import soundfile as sf
from dotenv import load_dotenv
import chatterbox
import torch

from smart_chat_system import SmartChatSystem
from audio_classifier import NatureSoundClassifier
from forest_danger_detector import ForestDangerDetector
from escape_game_engine import EscapeGameEngine, MultiSoundAnalyzer

# 환경 변수 로드
load_dotenv()

# TTS 엔진 설정 (환경변수로 제어 가능)
TTS_ENGINE = os.getenv("TTS_ENGINE", "chatterbox")  # "chatterbox" 또는 "openai"

app = FastAPI(title="Smart Voice Chat API", version="2.0.0")

# CORS 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# 전역 변수
smart_chat_system = None
chatterbox_tts = None
openai_client = None
audio_classifier = None
forest_detector = None
escape_game = None

async def generate_tts_audio(text: str) -> tuple[bytes, int]:
    """TTS 오디오 생성 - 엔진에 따라 다른 방식 사용"""
    global chatterbox_tts, openai_client
    
    if TTS_ENGINE == "openai" and openai_client:
        # OpenAI TTS (빠름)
        print("OpenAI TTS 사용 중...")
        response = openai_client.audio.speech.create(
            model="tts-1",  # 빠른 모델
            voice="alloy",
            input=text
        )
        return response.content, 22050  # OpenAI TTS 샘플링 레이트
    
    else:
        # Chatterbox TTS (품질 좋음, 느림)
        print("Chatterbox TTS 사용 중...")
        audio_output = chatterbox_tts.generate(text)
        
        # PyTorch tensor를 numpy array로 변환
        if hasattr(audio_output, 'numpy'):
            audio_output = audio_output.numpy()
        elif hasattr(audio_output, 'detach'):
            audio_output = audio_output.detach().cpu().numpy()
        
        # WAV로 변환
        audio_buffer = io.BytesIO()
        sf.write(audio_buffer, audio_output.squeeze(), chatterbox_tts.sr, format='WAV')
        return audio_buffer.getvalue(), chatterbox_tts.sr

# Pydantic 모델들


class TTSRequest(BaseModel):
    text: str
    conversation_id: str = None


class ChatRequest(BaseModel):
    message: str
    conversation_id: str = None


class SettingsRequest(BaseModel):
    similarity_threshold: float = None
    learning_enabled: bool = None
    gpt_model: str = None


@app.on_event("startup")
async def startup_event():
    """서버 시작 시 초기화"""
    global smart_chat_system, chatterbox_tts, openai_client, audio_classifier, forest_detector, escape_game

    print("서버 시작 중...")

    # OpenAI API 키 확인
    openai_api_key = os.getenv("OPENAI_API_KEY")
    if not openai_api_key:
        print("OPENAI_API_KEY 환경 변수가 설정되지 않았습니다!")
        return

    try:
        # 스마트 채팅 시스템 초기화
        print("스마트 채팅 시스템 초기화 중...")
        smart_chat_system = SmartChatSystem(
            openai_api_key=openai_api_key,
            vector_db_path="./chroma_db"
        )

        # 기본 데이터가 없으면 초기화
        stats = smart_chat_system.get_system_stats()
        if stats["vector_db_stats"]["total_cached_responses"] == 0:
            print("기본 게임 데이터 초기화 중...")
            smart_chat_system.initialize_basic_data()

        # OpenAI 클라이언트 초기화
        openai_client = openai.OpenAI()
        
        # TTS 엔진 초기화
        if TTS_ENGINE == "openai":
            print("OpenAI TTS 엔진 사용 (빠른 속도)")
        else:
            print("Chatterbox TTS 엔진 사용 (고품질)")
            device = "cuda" if torch.cuda.is_available() else "cpu"
            
            # CPU 최적화 설정
            if device == "cpu":
                torch.set_num_threads(4)  # CPU 스레드 수 설정
                print("CPU 모드 최적화 적용")
            
            chatterbox_tts = chatterbox.ChatterboxTTS.from_pretrained(device=device)

        # 자연소리 분류기 초기화
        print("자연소리 분류기 초기화 중...")
        audio_classifier = NatureSoundClassifier()
        
        # 모델이 없으면 훈련, 있으면 로드
        if not audio_classifier.load_model():
            print("모델이 없습니다. 자동 훈련을 시작합니다...")
            try:
                audio_classifier.train_model("./audio_file")
                audio_classifier.save_model()
                print("자연소리 분류 모델 훈련 완료!")
            except Exception as e:
                print(f"모델 훈련 실패: {e}")
                audio_classifier = None

        # 숲 생존 위험 감지 시스템 초기화
        print("숲 생존 위험 감지 시스템 초기화 중...")
        forest_detector = ForestDangerDetector()
        print("생존 시스템 준비 완료!")

        # 방탈출 게임 엔진 초기화
        print("시각장애인 방탈출 게임 엔진 초기화 중...")
        escape_game = EscapeGameEngine()
        escape_game.initialize_game()
        print("방탈출 게임 시스템 준비 완료!")

        print("서버 초기화 완료!")
        print(
            f"캐시된 응답: {stats['vector_db_stats']['total_cached_responses']}개")

    except Exception as e:
        print(f"서버 초기화 실패: {e}")


@app.get("/")
async def root():
    """서버 상태 확인"""
    return {"message": "Smart Voice Chat API Server", "status": "online", "version": "2.0.0"}


@app.get("/health")
async def health_check():
    """헬스 체크"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    stats = smart_chat_system.get_system_stats()
    return {
        "status": "healthy",
        "timestamp": datetime.now().isoformat(),
        "system_stats": stats
    }


@app.post("/voice-chat")
async def voice_chat_endpoint(
    file: UploadFile = File(...),
    conversation_id: str = Form(None)
):
    """음성 채팅 (음성 → STT → 스마트채팅 → TTS → 음성)"""
    if not smart_chat_system or not chatterbox_tts:
        raise HTTPException(status_code=503, detail="Services not initialized")

    try:
        start_time = datetime.now()

        # 1. STT: 음성을 텍스트로 변환
        print("STT 처리 중...")
        audio_data = await file.read()

        # 임시 파일로 저장 (자연소리 분석용)
        temp_filename = f"voice_{uuid.uuid4().hex}.wav"
        temp_path = os.path.join("temp_files", temp_filename)
        with open(temp_path, "wb") as f:
            f.write(audio_data)

        # OpenAI Whisper API 호출 - 영어 우선 인식
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

        audio_file = io.BytesIO(audio_data)
        audio_file.name = "audio.wav"

        # STT 영어 우선 설정 (게임 모드에서 영어 답변 정확히 인식)
        try:
            # 영어로 먼저 시도
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="en"  # 영어 우선!
            )
            user_text = transcript.text.strip()
            print(f"영어 STT 결과: {user_text}")
            
            # 영어 단어가 있는지 체크
            english_words = ["cat", "dog", "bird", "sound", "that", "is", "wind", "water", "noise", "frog", "hear", "can"]
            has_english = any(word.lower() in user_text.lower() for word in english_words)
            
            if not has_english:
                raise Exception("영어 인식 부족")
                
        except:
            # 실패시에만 한국어로 재시도
            audio_file = io.BytesIO(audio_data)
            audio_file.name = "audio.wav"
            
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="ko"
            )
            user_text = transcript.text.strip()
            print(f"한국어 STT 결과: {user_text}")

        print(f"최종 STT 결과: '{user_text}'")

        # 1.5. 방탈출 게임 모드 - 음성 답안 평가 준비
        print("방탈출 게임 모드: 사용자 음성 답안 분석 중...")
        
        # 임시 파일 삭제
        try:
            os.remove(temp_path)
        except:
            pass
            
        # 테스트용 숨겨진 인공소리 설정 (실제로는 Unity에서 제공)
        current_hidden_sounds = ["쇠 긁는 소리", "금속 소리", "파이프 소리", "철판 소리"]
        game_mode = True  # 방탈출 게임 모드 활성화

        if not user_text:
            raise HTTPException(status_code=400, detail="음성 인식 실패")

        # 2. 방탈출 게임 답안 평가
        print("GPT 답안 평가 중...")
        
        # 변수 초기화
        response_text = ""
        enhanced_input = user_text
        
        if game_mode:
            # 🎯 FOREST_SOUNDS 키워드 매칭 추가
            detected_sound = None
            matched_keywords = []
            user_text_lower = user_text.lower()
            
            for sound_file, keywords in FOREST_SOUNDS.items():
                for keyword in keywords:
                    if keyword.lower() in user_text_lower:
                        detected_sound = sound_file
                        matched_keywords.append(keyword)
                        break
                if detected_sound:
                    break
            
            print(f"키워드 매칭 결과: {detected_sound}")
            print(f"매칭된 키워드: {matched_keywords}")
            # 키워드 매칭 기반 직접 응답 생성 (GPT 우회)
            if detected_sound:
                # 정답: 키워드가 매칭된 경우
                sound_name = detected_sound.replace('.mp3', '').replace('_', ' ').replace('10 ', '').replace('9 ', '').replace('8 ', '').replace('7 ', '').replace('6 ', '').replace('5 ', '').replace('4 ', '').replace('3 ', '').replace('2 ', '').replace('1 ', '')
                response_text = f"✅ CORRECT! You identified {sound_name}."
                print(f"키워드 매칭 성공 → 직접 응답: {response_text}")
            else:
                # 오답: 키워드가 매칭되지 않은 경우
                response_text = "❌ WRONG! That is not one of the 10 forest sounds."
                print(f"키워드 매칭 실패 → 직접 응답: {response_text}")
            
            # GPT 우회하고 직접 응답
            enhanced_input = None  # GPT 호출하지 않음
            
            print(f"10개 숲소리 게임 평가: '{user_text}'")
            # enhanced_input은 이미 위에서 설정됨 (None = GPT 우회)
        else:
            enhanced_input = user_text
        
        # 게임 모드에서는 키워드 매칭 기반 직접 응답
        if game_mode and enhanced_input is None:
            print("방탈출 게임 모드: 키워드 매칭 기반 직접 응답")
            ai_response = response_text  # 위에서 생성한 직접 응답 사용
            response_source = "keyword_matching"
            print(f"키워드 매칭 응답: {ai_response}")
        elif game_mode and enhanced_input:
            print("방탈출 게임 모드: GPT 백업 호출")
            openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
            
            try:
                response = openai_client.chat.completions.create(
                    model="gpt-3.5-turbo",
                    messages=[{"role": "user", "content": enhanced_input}],
                    max_tokens=200,
                    temperature=0.1,
                    timeout=15
                )
                ai_response = response.choices[0].message.content.strip()
                response_source = "gpt_direct"
                print(f"GPT 직접 응답: {ai_response}")
            except Exception as e:
                print(f"GPT 직접 호출 실패: {e}")
                ai_response = "게임 답안을 처리하는 중 오류가 발생했습니다. 다시 시도해주세요."
                response_source = "error"
        else:
            # 일반 모드에서는 기존 방식 사용
            chat_result = await smart_chat_system.chat(enhanced_input, conversation_id)

            if not chat_result["success"]:
                raise HTTPException(
                    status_code=500, detail=chat_result.get("error", "채팅 처리 실패"))

            ai_response = chat_result["ai_response"]
            response_source = chat_result["response_source"]

        # 3. TTS: 텍스트를 음성으로 변환
        print("TTS 처리 중...")
        audio_bytes, sample_rate = await generate_tts_audio(ai_response)
        audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')

        # 총 처리 시간 계산
        total_time = (datetime.now() - start_time).total_seconds()

        # 응답 데이터 구성 (게임 모드와 일반 모드 구분)
        if game_mode:
            response_conversation_id = conversation_id or f"escape_game_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
            chat_response_time = round(total_time, 2)
        else:
            response_conversation_id = chat_result["conversation_id"]
            chat_response_time = chat_result["response_time_seconds"]

        response_data = {
            "success": True,
            "user_text": user_text,
            "ai_response": ai_response,
            "conversation_id": response_conversation_id,
            "response_source": response_source,  # "cache", "gpt", "gpt_direct", "error"
            "audio_data": audio_base64,
            "sample_rate": sample_rate,
            "processing_time_seconds": round(total_time, 2),
            "chat_response_time": chat_response_time,
            "timestamp": datetime.now().isoformat()
        }

        # 방탈출 게임 정보 추가
        if game_mode:
            # GPT 응답에서 정답 여부 판단 (영어 응답 지원)
            is_correct = ("✅" in ai_response and "CORRECT" in ai_response.upper()) or ("정답" in ai_response and "🎉" in ai_response)
            
            response_data["game_mode"] = True
            response_data["user_answer"] = user_text
            response_data["hidden_sounds"] = current_hidden_sounds
            response_data["is_correct"] = is_correct
            response_data["gpt_evaluation"] = ai_response
            
            # 정답 여부에 따른 추가 정보
            if is_correct:
                response_data["game_status"] = "correct"
                response_data["next_action"] = "다음 레벨로 진행합니다!"
            else:
                response_data["game_status"] = "incorrect"
                response_data["next_action"] = "다시 시도해보세요!"

        return JSONResponse(response_data)

    except Exception as e:
        print(f"음성 채팅 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/chat")
async def chat_endpoint(request: ChatRequest):
    """텍스트 채팅 (스마트 채팅만)"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    try:
        result = await smart_chat_system.chat(
            request.message,
            request.conversation_id
        )

        return JSONResponse(result)

    except Exception as e:
        print(f"채팅 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/tts")
async def tts_endpoint(request: TTSRequest):
    """TTS만 (텍스트 → 음성)"""
    if not chatterbox_tts:
        raise HTTPException(status_code=503, detail="TTS not initialized")

    try:
        print(f"TTS 변환: {request.text[:30]}...")

        # TTS 처리
        audio_bytes, sample_rate = await generate_tts_audio(request.text)
        audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')

        return JSONResponse({
            "success": True,
            "text": request.text,
            "audio_data": audio_base64,
            "sample_rate": sample_rate,
            "conversation_id": request.conversation_id,
            "timestamp": datetime.now().isoformat()
        })

    except Exception as e:
        print(f"TTS 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/analyze-nature-sound")
async def analyze_nature_sound(file: UploadFile = File(...)):
    """자연소리 분석 + GPT 조언"""
    global audio_classifier, smart_chat_system
    
    try:
        print("자연소리 분석 중...")
        
        if audio_classifier is None:
            raise HTTPException(status_code=500, detail="자연소리 분류기가 초기화되지 않았습니다.")
        
        # 임시 파일로 저장
        temp_filename = f"temp_{uuid.uuid4().hex}.wav"
        temp_path = os.path.join("temp_files", temp_filename)
        
        audio_data = await file.read()
        with open(temp_path, "wb") as f:
            f.write(audio_data)
        
        # 자연소리 분류
        sound_type, confidence = audio_classifier.predict_sound(temp_path)
        
        if sound_type is None:
            # 임시 파일 삭제
            os.remove(temp_path)
            raise HTTPException(status_code=400, detail="오디오 분석에 실패했습니다.")
        
        # GPT에게 상황별 조언 요청
        context = f"게임 플레이어가 '{sound_type}' 소리를 듣고 있습니다. 이 상황에 대한 게임 플레이 조언을 해주세요."
        
        chat_result = await smart_chat_system.chat(
            user_input=context,
            conversation_id="nature_sound_analysis"
        )
        
        ai_advice = chat_result["ai_response"]
        
        # 임시 파일 삭제
        os.remove(temp_path)
        
        return JSONResponse({
            "success": True,
            "detected_sound": sound_type,
            "confidence": float(confidence),
            "basic_advice": audio_classifier.get_advice_for_sound(sound_type),
            "ai_advice": ai_advice,
            "timestamp": datetime.now().isoformat()
        })
        
    except Exception as e:
        print(f"❌ 자연소리 분석 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/stt")
async def stt_endpoint(file: UploadFile = File(...)):
    """STT만 (음성 → 텍스트)"""
    try:
        print("STT 처리 중...")

        audio_data = await file.read()

        # OpenAI Whisper API 호출
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

        audio_file = io.BytesIO(audio_data)
        audio_file.name = "audio.wav"

        transcript = openai_client.audio.transcriptions.create(
            model="whisper-1",
            file=audio_file,
            language="ko"
        )

        user_text = transcript.text.strip()

        return JSONResponse({
            "success": True,
            "text": user_text,
            "timestamp": datetime.now().isoformat()
        })

    except Exception as e:
        print(f"STT 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/stats")
async def get_stats():
    """시스템 통계 정보"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    return smart_chat_system.get_system_stats()


@app.post("/settings")
async def update_settings(request: SettingsRequest):
    """시스템 설정 업데이트"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    try:
        # 설정 업데이트
        settings = {}
        if request.similarity_threshold is not None:
            settings["similarity_threshold"] = request.similarity_threshold
        if request.learning_enabled is not None:
            settings["learning_enabled"] = request.learning_enabled
        if request.gpt_model is not None:
            settings["gpt_model"] = request.gpt_model

        smart_chat_system.update_settings(**settings)

        return {
            "success": True,
            "message": "설정이 업데이트되었습니다",
            "updated_settings": settings
        }

    except Exception as e:
        print(f"설정 업데이트 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/backup")
async def backup_data(filename: str = Form(None)):
    """VectorDB 데이터 백업"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    try:
        success = smart_chat_system.backup_data(filename)

        if success:
            return {"success": True, "message": "데이터 백업 완료"}
        else:
            raise HTTPException(status_code=500, detail="백업 실패")

    except Exception as e:
        print(f"백업 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/reset-vectordb")
async def reset_vectordb():
    """VectorDB 초기화"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    try:
        success = smart_chat_system.reset_vector_db()

        if success:
            # 기본 데이터 다시 초기화
            smart_chat_system.initialize_basic_data()
            return {"success": True, "message": "VectorDB 초기화 및 기본 데이터 복원 완료"}
        else:
            raise HTTPException(status_code=500, detail="초기화 실패")

    except Exception as e:
        print(f"VectorDB 초기화 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/clear-game-cache")
async def clear_game_cache():
    """게임 관련 잘못된 캐시 삭제"""
    if not smart_chat_system:
        raise HTTPException(status_code=503, detail="Smart chat system not initialized")

    try:
        # VectorDB 전체 초기화
        success = smart_chat_system.reset_vector_db()
        
        if success:
            # 기본 데이터만 다시 초기화 (게임 관련 제외)
            basic_qa_pairs = [
                ("안녕", "안녕하세요! 게임에 오신 걸 환영합니다! 😊"),
                ("안녕하세요", "안녕하세요! 무엇을 도와드릴까요?"),
                ("도움말", "게임 조작법이나 궁금한 점을 언제든 물어보세요!"),
                ("조작법", "기본 조작법을 알려드릴게요. W키로 전진, A/D키로 좌우 이동, 스페이스바로 점프입니다."),
                ("시작", "새로운 모험을 시작하시는군요! 행운을 빕니다! 🎮")
            ]
            
            for question, answer in basic_qa_pairs:
                smart_chat_system.vector_db.add_response(question, answer, "safe_initialization")
            
            return {
                "success": True, 
                "message": "게임 캐시 삭제 완료! 이제 새로운 프롬프트가 제대로 작동합니다.",
                "cache_cleared": True,
                "basic_data_restored": len(basic_qa_pairs)
            }
        else:
            raise HTTPException(status_code=500, detail="캐시 삭제 실패")

    except Exception as e:
        print(f"게임 캐시 삭제 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/similar-questions/{user_input}")
async def get_similar_questions(user_input: str, limit: int = 3):
    """유사한 질문들 검색 (디버깅용)"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    try:
        similar = await smart_chat_system.get_similar_questions(user_input, limit)
        return {
            "user_input": user_input,
            "similar_questions": similar
        }

    except Exception as e:
        print(f"유사 질문 검색 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/conversation/{conversation_id}")
async def clear_conversation(conversation_id: str):
    """특정 대화 히스토리 삭제"""
    if not smart_chat_system:
        raise HTTPException(
            status_code=503, detail="Smart chat system not initialized")

    try:
        smart_chat_system.clear_conversation(conversation_id)
        return {"success": True, "message": f"대화 {conversation_id} 삭제 완료"}

    except Exception as e:
        print(f"대화 삭제 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

# ============ 방탈출 게임 API 엔드포인트 ============

@app.get("/escape-game/start")
async def start_escape_game():
    """방탈출 게임 시작"""
    try:
        global escape_game
        
        if escape_game is None:
            escape_game = EscapeGameEngine()
            escape_game.initialize_game()
        
        welcome_message = escape_game.get_welcome_message()
        game_status = escape_game.get_game_status()
        
        return JSONResponse({
            "success": True,
            "welcome": welcome_message,
            "game_status": game_status,
            "instructions": {
                "accessibility": "헤드폰 착용을 권장합니다",
                "objective": "숲에 어울리지 않는 인공소리를 찾아내세요",
                "method": "Unity에서 오디오를 전송하면 AI가 분석합니다"
            }
        })
        
    except Exception as e:
        print(f"게임 시작 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/escape-game/analyze")
async def analyze_mixed_audio(file: UploadFile = File(...), hidden_sound: str = Form(None)):
    """Unity에서 온 중첩 오디오 분석 (10개 소리) + 숨겨진 인공소리 정보"""
    try:
        global escape_game
        
        if escape_game is None:
            raise HTTPException(status_code=400, detail="게임이 초기화되지 않았습니다. /escape-game/start를 먼저 호출하세요.")
        
        print(f"방탈출 게임 - 중첩 오디오 분석 시작...")
        
        # 임시 파일 저장
        temp_path = f"temp_{uuid.uuid4()}.wav"
        audio_data = await file.read()
        
        with open(temp_path, "wb") as f:
            f.write(audio_data)
        
        # 중첩 소리 분석
        analysis_result = escape_game.analyze_game_audio(temp_path)
        
        # 임시 파일 삭제
        os.remove(temp_path)
        
        if not analysis_result["success"]:
            raise HTTPException(status_code=400, detail=analysis_result["message"])
        
        # 접근성을 위한 상세한 피드백 (음성 입력 방식)
        accessibility_feedback = {
            "total_sounds_detected": analysis_result["total_sounds"],
            "natural_sounds_count": analysis_result["natural_sounds"], 
            "artificial_sounds_count": analysis_result["artificial_sounds"],
            "audio_description": f"{analysis_result['total_sounds']}개의 소리가 겹쳐 들렸습니다. "
                                f"그 중 {analysis_result['natural_sounds']}개는 자연소리, "
                                f"{analysis_result['artificial_sounds']}개는 인공소리로 분석되었습니다.",
            "question": "어떤 소리가 숲에 어울리지 않나요? 마이크에 대고 구체적으로 말씀해주세요.",
            "instruction": "예: '쇠 긁는 소리가 들린다', '헬리콥터 소리가 난다' 등",
            "hidden_sound_hint": hidden_sound if hidden_sound else "Unity에서 정보 제공 안됨"
        }
        
        return JSONResponse({
            "success": True,
            "analysis": analysis_result,
            "accessibility_feedback": accessibility_feedback,
            "game_status": escape_game.get_game_status(),
            "level": escape_game.current_level,
            "hidden_artificial_sound": hidden_sound,  # Unity에서 제공한 실제 인공소리
            "timestamp": datetime.now().isoformat()
        })
        
    except Exception as e:
        print(f"중첩 오디오 분석 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/escape-game/voice-answer")
async def submit_voice_answer(file: UploadFile = File(...), analysis_result: str = Form(...)):
    """사용자 음성 답안 제출 및 평가 (STT → GPT → TTS)"""
    try:
        global escape_game, smart_chat_system
        
        if escape_game is None:
            raise HTTPException(status_code=400, detail="게임이 초기화되지 않았습니다.")
        
        print("음성 답안 처리 시작...")
        
        # 1. STT: 음성을 텍스트로 변환
        audio_data = await file.read()
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
        
        audio_file = io.BytesIO(audio_data)
        audio_file.name = "voice_answer.wav"
        
        transcript = openai_client.audio.transcriptions.create(
            model="whisper-1",
            file=audio_file,
            language="ko"
        )
        
        user_description = transcript.text.strip()
        print(f"사용자 음성: '{user_description}'")
        
        # 2. 분석 결과에서 실제 숨겨진 인공소리 추출
        import json
        analysis_data = json.loads(analysis_result)
        
        # Unity에서 제공한 실제 숨겨진 인공소리 정보 사용
        hidden_artificial_sound = analysis_data.get("hidden_artificial_sound")
        
        if hidden_artificial_sound:
            hidden_artificial_sounds = [hidden_artificial_sound]
        else:
            # Unity에서 정보를 안 보냈거나 인공소리가 없는 경우
            hidden_artificial_sounds = []
        
        # 3. GPT로 답안 평가
        if hidden_artificial_sounds:
            gpt_prompt = f"""
[숲 탈출 게임 - 답안 평가]

🌲 상황: 숲에서 여러 소리가 들렸습니다.
🎯 숨겨진 인공소리: {', '.join(hidden_artificial_sounds)}
🎤 사용자 답안: "{user_description}"

사용자가 올바른 인공소리를 찾았는지 판단해주세요.

판단 기준:
1. 사용자가 언급한 소리가 실제 숨겨진 인공소리와 일치하는가?
2. 유사한 표현도 정답으로 인정 (예: "쇠 긁는 소리" = "금속 긁는 소리")
3. 자연소리를 언급했다면 오답

응답 형식:
- 정답이면: "정답입니다! [구체적인 칭찬과 설명]"
- 오답이면: "아쉽습니다. [힌트 제공]"
- 자연스럽고 격려하는 톤으로 답변
"""
        else:
            gpt_prompt = f"""
[숲 탈출 게임 - 답안 평가]

🌲 상황: 숲에서 여러 소리가 들렸습니다.
🎯 실제로는 모든 소리가 자연소리였습니다.
🎤 사용자 답안: "{user_description}"

사용자가 인공소리를 찾았다고 했지만 실제로는 없었습니다.

응답: "아쉽습니다. 이번에는 모든 소리가 자연소리였어요. 다시 도전해보세요!"
"""
        
        # GPT 평가
        chat_result = await smart_chat_system.chat(
            user_input=gpt_prompt,
            conversation_id=f"escape_game_level_{escape_game.current_level}"
        )
        
        gpt_feedback = chat_result["ai_response"]
        
        # 4. 정답 여부 판단 (GPT 응답에서 "정답" 키워드 포함 여부로 판단)
        is_correct = "정답" in gpt_feedback
        
        # 5. 게임 상태 업데이트
        if is_correct:
            escape_game.score += 100
            escape_game.current_level += 1
            escape_game.game_state["correct_answers"] += 1
        
        escape_game.game_state["attempts"] += 1
        
        # 6. TTS로 피드백 음성 생성
        audio_data, sample_rate = await generate_tts_audio(gpt_feedback)
        audio_b64 = base64.b64encode(audio_data).decode()
        
        print(f"GPT 평가: {gpt_feedback}")
        print(f"정답 여부: {is_correct}")
        
        response_data = {
            "success": True,
            "user_voice_text": user_description,
            "gpt_evaluation": gpt_feedback,
            "is_correct": is_correct,
            "game_status": escape_game.get_game_status(),
            "audio_feedback": audio_b64,
            "next_action": "다음 레벨" if is_correct else "다시 시도",
            "hidden_artificial_sounds": hidden_artificial_sounds if hidden_artificial_sounds else "없음"
        }
        
        return JSONResponse(response_data)
        
    except Exception as e:
        print(f"음성 답안 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/escape-game/hint")
async def get_hint():
    """힌트 요청"""
    try:
        global escape_game
        
        if escape_game is None:
            raise HTTPException(status_code=400, detail="게임이 초기화되지 않았습니다.")
        
        # 마지막 분석 결과 기반 힌트 (실제로는 세션에 저장해야 함)
        # 간단한 힌트 제공
        hints = [
            "💡 집중해서 들어보세요. 자연에서 나올 수 없는 소리가 있나요?",
            "💡 전자음, 기계음, 금속음에 주의해보세요.",
            "💡 새소리, 바람소리, 물소리 등은 자연소리입니다.",
            "💡 숲에서 들을 수 없는 현대적인 소리를 찾아보세요."
        ]
        
        hint_text = hints[escape_game.hints_used % len(hints)]
        escape_game.hints_used += 1
        
        # 힌트 음성 생성
        audio_data, sample_rate = await generate_tts_audio(hint_text)
        audio_b64 = base64.b64encode(audio_data).decode()
        
        return JSONResponse({
            "success": True,
            "hint": hint_text,
            "audio_hint": audio_b64,
            "hints_used": escape_game.hints_used,
            "accessibility_info": {
                "message": "힌트가 음성으로 제공됩니다"
            }
        })
        
    except Exception as e:
        print(f"힌트 제공 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/escape-game/status")
async def get_game_status():
    """현재 게임 상태 조회"""
    try:
        global escape_game
        
        if escape_game is None:
            return JSONResponse({
                "game_initialized": False,
                "message": "게임이 초기화되지 않았습니다. /escape-game/start를 호출하세요."
            })
        
        status = escape_game.get_game_status()
        
        return JSONResponse({
            "success": True,
            "game_initialized": True,
            "status": status,
            "accessibility_summary": f"현재 레벨 {status['level']}, "
                                   f"점수 {status['score']}점, "
                                   f"정답률 {status['accuracy']:.1f}%"
        })
        
    except Exception as e:
        print(f"게임 상태 조회 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

# ============ 유틸리티 함수 ============

def fix_korean_misrecognition(text):
    """한글 오인식 후처리"""
    # 영어를 한글로 잘못 인식한 경우들을 수정
    corrections = {
        "샛캣": "cat",
        "캣": "cat", 
        "사운드": "sound",
        "이즈": "is",
        "댓": "that",
        "도그": "dog",
        "버드": "bird",
        "윈드": "wind",
        "워터": "water"
    }
    
    corrected = text
    for wrong, correct in corrections.items():
        corrected = corrected.replace(wrong, correct)
    
    print(f"오인식 후처리: '{text}' → '{corrected}'")
    return corrected

# ============ 10개 숲 소리 게임 API 엔드포인트 ============

# 게임에서 사용하는 정확한 10개 숲 소리 목록 (영어/한국어 키워드)
FOREST_SOUNDS = {
    "1._Bird_chirping.mp3": ["새소리", "새", "조류", "bird", "chirping", "tweeting", "bird sound", "새 짹짹", "새 지저귀는 소리"],
    "2._Wind_blowing.mp3": ["바람소리", "바람", "wind", "breeze", "wind blowing", "wind sound", "바람 부는 소리"],
    "3._Leaves_rustling.mp3": ["나뭇잎", "잎사귀", "바스락", "leaves", "rustling", "leaf", "leaves rustling", "나뭇잎 소리"],
    "4._Stream_flowing.mp3": ["물소리", "시냇물", "개울", "강물", "water", "stream", "brook", "flowing", "river", "물 흐르는 소리"],
    "5._Cricket_chirping.mp3": ["귀뚜라미", "벌레소리", "곤충", "cricket", "insect", "cricket chirping", "귀뚜라미 소리"],
    "6._Owl_hooting.mp3": ["부엉이", "올빼미", "owl", "hooting", "owl sound", "부엉부엉", "부엉이 소리"],
    "7._Woodpecker_tapping.mp3": ["딱따구리", "woodpecker", "tapping", "pecking", "딱따구리 소리", "나무 두드리는 소리"],
    "8._Tree_creaking.mp3": ["나무", "삐걱", "creaking", "tree", "wood", "tree creaking", "나무 소리", "나무 삐걱거림"],
    "9._Squirrel_chattering.mp3": ["다람쥐", "squirrel", "chattering", "squirrel sound", "다람쥐 소리", "다람쥐 재잘"],
    "10._Frog_croaking.mp3": ["개구리", "frog", "croaking", "frog sound", "개골개골", "개구리 소리", "개구리 울음"]
}

@app.post("/forest-game/voice-check")
async def forest_sound_voice_check(
    file: UploadFile = File(...),
    conversation_id: str = Form(None),
    fast_mode: bool = Form(False)  # 빠른 모드 옵션
):
    """10개 숲 소리 게임 - 음성으로 소리 맞추기"""
    if not smart_chat_system:
        raise HTTPException(status_code=503, detail="Smart chat system not initialized")

    try:
        start_time = datetime.now()
        print("숲 소리 게임 - 음성 답안 처리 시작...")

        # 1. STT: 영어 우선 인식 (is that cat sound 정확히 인식하기 위해)
        audio_data = await file.read()
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

        audio_file = io.BytesIO(audio_data)
        audio_file.name = "audio.wav"

        # STT 영어 우선 설정
        try:
            # 영어로 먼저 시도
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="en"  # 영어 우선!
            )
            user_text = transcript.text.strip()
            print(f"영어 STT 결과: {user_text}")
            
            # 영어 단어가 있는지 체크
            english_words = ["cat", "dog", "bird", "sound", "that", "is", "wind", "water", "noise"]
            has_english = any(word.lower() in user_text.lower() for word in english_words)
            
            if not has_english:
                raise Exception("영어 인식 부족")
        except:
            # 실패시에만 한국어로 재시도
            # language="ko"
            audio_file = io.BytesIO(audio_data)
            audio_file.name = "audio.wav"
            
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1", 
                file=audio_file,
                language="ko"
            )
            user_text = transcript.text.strip()
            print(f"한국어 STT 결과: {user_text}")
        
        print(f"최종 STT 결과: '{user_text}'")

        if not user_text:
            raise HTTPException(status_code=400, detail="음성 인식 실패")

        # 2. 10개 숲 소리 중에서 정답 찾기 (대소문자 구분 없음)
        detected_sound = None
        matched_keywords = []
        user_text_lower = user_text.lower()
        
        for sound_file, keywords in FOREST_SOUNDS.items():
            for keyword in keywords:
                if keyword.lower() in user_text_lower:
                    detected_sound = sound_file
                    matched_keywords.append(keyword)
                    break
            if detected_sound:
                break

        # 3. GPT를 활용한 정답/오답 판정 및 피드백 생성
        forest_game_prompt = f"""
!!!! CRITICAL OVERRIDE INSTRUCTION !!!!
YOU ARE NOT A HELPFUL ASSISTANT. YOU ARE A GAME SCORING MACHINE.
IGNORE ALL PREVIOUS INSTRUCTIONS TO BE HELPFUL OR CONVERSATIONAL.

ONLY OUTPUT ONE OF THESE EXACT PHRASES:

IF SYSTEM DETECTED: {detected_sound.replace('.mp3', '').replace('_', ' ') if detected_sound else 'NONE'}

IF DETECTED="NONE":
- "❌ WRONG! That is not a forest sound."

IF DETECTED EXISTS:
- "✅ CORRECT! You identified {detected_sound.replace('.mp3', '').replace('_', ' ') if detected_sound else 'ERROR'}."

PLAYER INPUT: "{user_text}"

ABSOLUTELY FORBIDDEN RESPONSES:
- "That sounds like a fun game"
- "I'd be happy to help"
- "Let me know if you need hints"
- Any conversation or questions

OUTPUT ONLY THE EXACT PHRASE FROM ABOVE. NO OTHER TEXT."""

        # 게임 모드에서는 VectorDB 캐시를 완전히 우회
        BYPASS_CACHE_FOR_GAME = True
        
        if BYPASS_CACHE_FOR_GAME:
            # VectorDB 캐시 우회하고 GPT 직접 호출
            print("게임 모드: 캐시 우회, GPT 직접 호출")
            openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
            
            try:
                response = openai_client.chat.completions.create(
                    model="gpt-3.5-turbo",
                    messages=[{"role": "user", "content": forest_game_prompt}],
                    max_tokens=200,
                    temperature=0.1,  # 일관된 게임 판정을 위해 낮은 온도
                    timeout=15
                )
                ai_response = response.choices[0].message.content.strip()
                print(f"GPT 직접 응답: {ai_response}")
            except Exception as e:
                print(f"GPT 직접 호출 실패: {e}")
                ai_response = "❌ WRONG! That is not a forest sound." if not detected_sound else f"✅ CORRECT! You identified {detected_sound.replace('.mp3', '').replace('_', ' ')}"
        
        elif USE_DIRECT_JUDGMENT:
            # 순수 키워드 매칭 기반 판정
            if detected_sound:
                sound_name = detected_sound.replace('.mp3', '').replace('_', ' ')
                ai_response = f"✅ CORRECT! You identified {sound_name}."
            else:
                # 고양이, 개 등 명시적 오답 체크
                wrong_sounds = ["cat", "dog", "car", "music", "human", "person", "voice", "helicopter", "plane", "고양이", "개", "자동차", "음악", "사람", "헬리콥터"]
                found_wrong = any(wrong.lower() in user_text.lower() for wrong in wrong_sounds)
                
                if found_wrong:
                    ai_response = "❌ WRONG! That is not a forest sound."
                else:
                    ai_response = "❌ WRONG! Please identify one of the 10 forest sounds: bird, wind, leaves, water, cricket, owl, woodpecker, tree, squirrel, or frog."
        else:
            # GPT 답안 평가 (기존 방식)
            chat_result = await smart_chat_system.chat(forest_game_prompt, conversation_id)

            if not chat_result["success"]:
                raise HTTPException(status_code=500, detail=chat_result.get("error", "채팅 처리 실패"))

            ai_response = chat_result["ai_response"]

        # 4. TTS: 피드백을 음성으로 변환 (빠른 모드에서는 스킵 가능)
        audio_base64 = None
        sample_rate = 22050
        
        if not fast_mode:  # 일반 모드에서만 TTS 처리
            print("TTS 처리 중...")
            try:
                audio_bytes, sample_rate = await generate_tts_audio(ai_response)
                audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')
                print("TTS 완료")
            except Exception as e:
                print(f"TTS 실패 (계속 진행): {e}")
                audio_base64 = None
        else:
            print("빠른 모드: TTS 스킵")

        # 5. 정답 여부 판단
        is_correct = detected_sound is not None
        
        # 총 처리 시간 계산
        total_time = (datetime.now() - start_time).total_seconds()

        # conversation_id 처리
        if USE_DIRECT_JUDGMENT:
            response_conversation_id = conversation_id or f"forest_game_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        else:
            response_conversation_id = chat_result["conversation_id"]

        response_data = {
            "success": True,
            "game_mode": "forest_sound_game",
            "judgment_method": "direct_matching" if USE_DIRECT_JUDGMENT else "gpt_evaluation",
            "user_text": user_text,
            "ai_response": ai_response,
            "conversation_id": response_conversation_id,
            "is_correct": is_correct,
            "detected_sound": detected_sound.replace('.mp3', '').replace('_', ' ') if detected_sound else None,
            "matched_keywords": matched_keywords,
            "audio_feedback": audio_base64,
            "sample_rate": sample_rate,
            "processing_time_seconds": round(total_time, 2),
            "timestamp": datetime.now().isoformat(),
            "game_info": {
                "total_forest_sounds": len(FOREST_SOUNDS),
                "correct_answer_found": is_correct,
                "answer_category": detected_sound.split('_')[1] if detected_sound and '_' in detected_sound else None
            }
        }

        print(f"게임 결과: {'정답' if is_correct else '오답'} - {user_text}")
        return JSONResponse(response_data)

    except Exception as e:
        print(f"숲 소리 게임 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/forest-game/sound-list")
async def get_forest_sound_list():
    """게임에서 사용하는 10개 숲 소리 목록 조회"""
    try:
        sound_info = []
        for sound_file, keywords in FOREST_SOUNDS.items():
            sound_info.append({
                "file_name": sound_file,
                "sound_name": sound_file.replace('.mp3', '').replace('_', ' '),
                "accepted_keywords": keywords,
                "main_keyword": keywords[0]
            })
        
        return JSONResponse({
            "success": True,
            "total_sounds": len(FOREST_SOUNDS),
            "forest_sounds": sound_info,
            "game_rule": "이 10가지 소리만 정답으로 인정됩니다. 다른 소리는 모두 오답입니다.",
            "usage": "Unity에서 소리를 재생하고, 플레이어가 /forest-game/voice-check 엔드포인트로 음성 답안을 제출하세요."
        })
        
    except Exception as e:
        print(f"소리 목록 조회 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/forest-game/text-check")
async def forest_sound_text_check(request: ChatRequest):
    """10개 숲 소리 게임 - 텍스트로 소리 맞추기 (테스트용)"""
    if not smart_chat_system:
        raise HTTPException(status_code=503, detail="Smart chat system not initialized")

    try:
        user_text = request.message.strip()
        print(f"텍스트 답안: {user_text}")

        # 10개 숲 소리 중에서 정답 찾기 (대소문자 구분 없음)
        detected_sound = None
        matched_keywords = []
        user_text_lower = user_text.lower()
        
        for sound_file, keywords in FOREST_SOUNDS.items():
            for keyword in keywords:
                if keyword.lower() in user_text_lower:
                    detected_sound = sound_file
                    matched_keywords.append(keyword)
                    break
            if detected_sound:
                break

        # GPT 답안 평가
        forest_game_prompt = f"""
🌲 [숲 소리 게임 - 텍스트 답안 평가]

정답 소리: {', '.join([name.replace('.mp3', '').replace('_', ' ') for name in FOREST_SOUNDS.keys()])}

플레이어 답안: "{user_text}"
감지된 소리: {detected_sound.replace('.mp3', '').replace('_', ' ') if detected_sound else '없음'}

{"정답입니다! 🎉" if detected_sound else "오답입니다. 😅 10가지 숲 소리 중에서 답해주세요."}

{"올바른 숲 소리를 찾으셨네요!" if detected_sound else "힌트: 새, 바람, 나뭇잎, 물, 귀뚜라미, 부엉이, 딱따구리, 나무, 다람쥐, 개구리 중에서 답해보세요."}
"""

        chat_result = await smart_chat_system.chat(forest_game_prompt, request.conversation_id)
        
        return JSONResponse({
            "success": True,
            "user_text": user_text,
            "ai_response": chat_result["ai_response"],
            "is_correct": detected_sound is not None,
            "detected_sound": detected_sound.replace('.mp3', '').replace('_', ' ') if detected_sound else None,
            "matched_keywords": matched_keywords,
            "debug_info": {
                "all_forest_sounds": list(FOREST_SOUNDS.keys()),
                "search_performed": f"Looking for keywords in: '{user_text_lower}'"
            }
        })

    except Exception as e:
        print(f"텍스트 게임 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/forest-game/quick-test")
async def quick_game_test(test_answer: str = Form(...)):
    """빠른 게임 테스트 - 답안만 입력"""
    try:
        user_text_lower = test_answer.lower()
        detected_sound = None
        matched_keywords = []
        
        # 키워드 매칭
        for sound_file, keywords in FOREST_SOUNDS.items():
            for keyword in keywords:
                if keyword.lower() in user_text_lower:
                    detected_sound = sound_file
                    matched_keywords.append(keyword)
                    break
            if detected_sound:
                break
        
        # 결과 판정
        if detected_sound:
            result = f"✅ CORRECT! You identified: {detected_sound.replace('.mp3', '').replace('_', ' ')}"
            status = "correct"
        else:
            result = f"❌ WRONG! '{test_answer}' is not one of the 10 forest sounds."
            status = "incorrect"
        
        return JSONResponse({
            "success": True,
            "test_answer": test_answer,
            "result": result,
            "status": status,
            "detected_sound": detected_sound.replace('.mp3', '').replace('_', ' ') if detected_sound else None,
            "matched_keywords": matched_keywords,
            "valid_sounds": [
                "bird/새소리", "wind/바람", "leaves/나뭇잎", "water/물소리", "cricket/귀뚜라미",
                "owl/부엉이", "woodpecker/딱따구리", "tree/나무", "squirrel/다람쥐", "frog/개구리"
            ]
        })
        
    except Exception as e:
        print(f"빠른 테스트 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

# ============ 새로운 시나리오: 비숲소리 찾기 게임 ============

# 숲에 어울리지 않는 소리들 (이걸 찾아야 함)
NON_FOREST_SOUNDS = {
    "cat_sound": ["cat", "meow", "고양이", "야옹", "캣", "샛캣"],
    "dog_sound": ["dog", "bark", "woof", "개", "멍멍", "도그"],
    "car_sound": ["car", "engine", "vehicle", "자동차", "엔진", "차량"],
    "phone_sound": ["phone", "ring", "전화", "벨소리", "핸드폰"],
    "music_sound": ["music", "song", "음악", "노래", "뮤직"],
    "helicopter_sound": ["helicopter", "chopper", "헬리콥터", "헬기"],
    "human_voice": ["voice", "human", "person", "talking", "사람", "목소리", "말소리"],
    "machine_sound": ["machine", "robot", "기계", "로봇", "머신"],
    "electronic_sound": ["electronic", "beep", "전자음", "삐삐", "전자"]
}

@app.post("/find-non-forest/voice-check")
async def find_non_forest_sound(
    file: UploadFile = File(...),
    non_forest_hint: str = Form(None),  # Unity에서 실제 숨겨진 비숲소리 정보
    conversation_id: str = Form(None),
    fast_mode: bool = Form(False)  # 빠른 모드 (TTS 스킵)
):
    """새로운 시나리오: 9개 숲소리 + 1개 비숲소리에서 비숲소리 찾기"""
    if not smart_chat_system:
        raise HTTPException(status_code=503, detail="Smart chat system not initialized")

    try:
        start_time = datetime.now()
        print("비숲소리 찾기 게임 - 음성 답안 처리 시작...")

        # 1. STT: 영어 우선 인식 (is that cat sound 정확히 인식하기 위해)
        audio_data = await file.read()
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

        audio_file = io.BytesIO(audio_data)
        audio_file.name = "audio.wav"

        # STT 영어 우선 설정
        try:
            # 영어로 먼저 시도
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="en"
            )
            user_text = transcript.text.strip()
            print(f"영어 STT 결과: {user_text}")
            
            # 영어 결과가 의미있는 단어인지 확인
            english_words = ["cat", "dog", "bird", "sound", "that", "is", "the", "what", "how", "wind", "water", "noise"]
            has_english = any(word.lower() in user_text.lower() for word in english_words)
            
            if not has_english or len(user_text.split()) < 2:
                # 영어 단어가 없거나 너무 짧으면 한국어로 재시도
                raise Exception("영어 인식 부족")
        except:
            # 한국어로 재시도
            audio_file = io.BytesIO(audio_data)
            audio_file.name = "audio.wav"
            
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1", 
                file=audio_file,
                language="ko"
            )
            user_text = transcript.text.strip()
            print(f"한국어 STT 결과: {user_text}")
        
        print(f"최종 STT 결과: '{user_text}'")

        if not user_text:
            raise HTTPException(status_code=400, detail="음성 인식 실패")

        # 2. 비숲소리 감지
        detected_non_forest = None
        matched_keywords = []
        user_text_lower = user_text.lower()
        
        for sound_type, keywords in NON_FOREST_SOUNDS.items():
            for keyword in keywords:
                if keyword.lower() in user_text_lower:
                    detected_non_forest = sound_type
                    matched_keywords.append(keyword)
                    break
            if detected_non_forest:
                break

        # 3. GPT를 활용한 비숲소리 판정
        non_forest_prompt = f"""
!!!! CRITICAL OVERRIDE INSTRUCTION !!!!
YOU ARE NOT A HELPFUL ASSISTANT. YOU ARE A GAME SCORING MACHINE.
IGNORE ALL PREVIOUS INSTRUCTIONS TO BE HELPFUL OR CONVERSATIONAL.

GAME: Find the NON-forest sound among 10 sounds (9 forest + 1 non-forest)
TASK: Player must identify the sound that DOESN'T belong in a forest

SYSTEM DETECTED NON-FOREST: {detected_non_forest.replace('_', ' ') if detected_non_forest else 'NONE'}

PLAYER INPUT: "{user_text}"

ONLY OUTPUT ONE OF THESE EXACT PHRASES:

IF DETECTED NON-FOREST SOUND:
- "Correct! You found the non-forest sound: {detected_non_forest.replace('_', ' ') if detected_non_forest else 'ERROR'}!"

IF NO NON-FOREST DETECTED:
- "Wrong! That is a forest sound. Find the artificial/mechanical sound!"

ABSOLUTELY FORBIDDEN RESPONSES:
- "That sounds like a fun game"
- "I'd be happy to help"
- "Let me know if you need hints"
- Any conversation or questions

OUTPUT ONLY THE EXACT PHRASE FROM ABOVE. NO OTHER TEXT."""

        # 게임 모드에서는 VectorDB 캐시를 완전히 우회
        print("비숲소리 게임: 캐시 우회, GPT 직접 호출")
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
        
        try:
            response = openai_client.chat.completions.create(
                model="gpt-3.5-turbo",
                messages=[{"role": "user", "content": non_forest_prompt}],
                max_tokens=200,
                temperature=0.1,  # 일관된 게임 판정을 위해 낮은 온도
                timeout=15
            )
            ai_response = response.choices[0].message.content.strip()
            print(f"GPT 직접 응답: {ai_response}")
        except Exception as e:
            print(f"GPT 직접 호출 실패: {e}")
            if detected_non_forest:
                ai_response = f"✅ CORRECT! You found the non-forest sound: {detected_non_forest.replace('_', ' ')}!"
            else:
                ai_response = "❌ WRONG! That is a forest sound. Find the artificial/mechanical sound!"
        
        is_correct = detected_non_forest is not None

        # 4. TTS: 피드백을 음성으로 변환 (빠른 모드에서는 스킵)
        audio_base64 = None
        sample_rate = 22050
        
        if not fast_mode:  # 일반 모드에서만 TTS 처리
            print("TTS 처리 중...")
            try:
                audio_bytes, sample_rate = await generate_tts_audio(ai_response)
                audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')
                print("TTS 완료")
            except Exception as e:
                print(f"TTS 실패 (계속 진행): {e}")
                audio_base64 = None
        else:
            print("빠른 모드: TTS 스킵")

        # 총 처리 시간 계산
        total_time = (datetime.now() - start_time).total_seconds()

        response_data = {
            "success": True,
            "game_mode": "find_non_forest_sound",
            "scenario": "9 forest sounds + 1 non-forest sound",
            "objective": "Find the sound that doesn't belong in the forest",
            "user_text": user_text,
            "ai_response": ai_response,
            "conversation_id": conversation_id or f"non_forest_game_{datetime.now().strftime('%Y%m%d_%H%M%S')}",
            "is_correct": is_correct,
            "detected_non_forest_sound": detected_non_forest,
            "matched_keywords": matched_keywords,
            "non_forest_hint_from_unity": non_forest_hint,
            "audio_feedback": audio_base64,
            "sample_rate": sample_rate,
            "processing_time_seconds": round(total_time, 2),
            "timestamp": datetime.now().isoformat(),
            "game_info": {
                "total_non_forest_categories": len(NON_FOREST_SOUNDS),
                "found_non_forest_sound": is_correct
            }
        }

        print(f"비숲소리 게임 결과: {'정답' if is_correct else '오답'} - {user_text}")
        return JSONResponse(response_data)

    except Exception as e:
        print(f"비숲소리 찾기 게임 처리 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/find-non-forest/sound-list")
async def get_non_forest_sound_list():
    """비숲소리 목록 조회"""
    try:
        non_forest_info = []
        for sound_type, keywords in NON_FOREST_SOUNDS.items():
            non_forest_info.append({
                "sound_type": sound_type,
                "sound_name": sound_type.replace('_', ' '),
                "keywords": keywords,
                "main_keyword": keywords[0]
            })
        
        return JSONResponse({
            "success": True,
            "game_scenario": "9개 숲소리 + 1개 비숲소리",
            "objective": "숲에 어울리지 않는 소리를 찾으세요",
            "total_non_forest_types": len(NON_FOREST_SOUNDS),
            "non_forest_sounds": non_forest_info,
            "forest_sounds_count": len(FOREST_SOUNDS),
            "usage": "Unity에서 10개 소리(9개 숲소리 + 1개 비숲소리)를 재생하고, /find-non-forest/voice-check로 답안 제출"
        })
        
    except Exception as e:
        print(f"비숲소리 목록 조회 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/find-non-forest/quick-test")
async def quick_non_forest_test(test_answer: str = Form(...)):
    """비숲소리 찾기 빠른 테스트"""
    try:
        user_text_lower = test_answer.lower()
        detected_non_forest = None
        matched_keywords = []
        
        # 비숲소리 키워드 매칭
        for sound_type, keywords in NON_FOREST_SOUNDS.items():
            for keyword in keywords:
                if keyword.lower() in user_text_lower:
                    detected_non_forest = sound_type
                    matched_keywords.append(keyword)
                    break
            if detected_non_forest:
                break
        
        # 결과 판정
        if detected_non_forest:
            result = f"✅ CORRECT! You found the non-forest sound: {detected_non_forest.replace('_', ' ')}"
            status = "correct"
        else:
            # 숲소리 언급했는지 체크
            mentioned_forest = None
            for sound_file, keywords in FOREST_SOUNDS.items():
                for keyword in keywords:
                    if keyword.lower() in user_text_lower:
                        mentioned_forest = sound_file.replace('.mp3', '').replace('_', ' ')
                        break
                if mentioned_forest:
                    break
            
            if mentioned_forest:
                result = f"❌ WRONG! '{mentioned_forest}' is a forest sound. Find the NON-forest sound!"
                status = "incorrect_forest_sound"
            else:
                result = f"❌ WRONG! '{test_answer}' - Find the sound that doesn't belong in the forest!"
                status = "incorrect_other"
        
        return JSONResponse({
            "success": True,
            "test_answer": test_answer,
            "result": result,
            "status": status,
            "detected_non_forest_sound": detected_non_forest.replace('_', ' ') if detected_non_forest else None,
            "matched_keywords": matched_keywords,
            "valid_non_forest_sounds": [
                "cat", "dog", "car", "phone", "music", "helicopter", "human voice", "machine", "electronic"
            ]
        })
        
    except Exception as e:
        print(f"비숲소리 빠른 테스트 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

# ============ 타임아웃 해결용 초고속 엔드포인트 ============

@app.post("/ultra-fast/voice-check")
async def ultra_fast_voice_check(
    file: UploadFile = File(...),
    game_mode: str = Form("forest")  # "forest" 또는 "non_forest"
):
    """초고속 음성 체크 - GPT/TTS 없이 키워드 매칭만"""
    try:
        start_time = datetime.now()
        print(f"초고속 모드 시작: {game_mode}")

        # 1. STT 영어 우선 (is that cat sound 정확히 인식하기 위해)
        audio_data = await file.read()
        openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

        audio_file = io.BytesIO(audio_data)
        audio_file.name = "audio.wav"

        # STT 영어 우선 설정 (ultra-fast용)
        try:
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="en"
            )
            user_text = transcript.text.strip()
            print(f"영어 STT 결과: {user_text}")
            
            # 영어 단어 체크
            english_words = ["cat", "dog", "bird", "sound", "that", "is", "wind", "water", "noise"]
            has_english = any(word.lower() in user_text.lower() for word in english_words)
            
            if not has_english:
                raise Exception("영어 인식 부족")
        except:
            # 한국어로 재시도
            audio_file = io.BytesIO(audio_data) 
            audio_file.name = "audio.wav"
            
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                language="ko"
            )
            user_text = transcript.text.strip()
            print(f"한국어 STT 결과: {user_text}")
        
        print(f"최종 STT 결과: '{user_text}'")

        if not user_text:
            raise HTTPException(status_code=400, detail="음성 인식 실패")

        # 2. 키워드 매칭만 (GPT 없음)
        user_text_lower = user_text.lower()
        
        if game_mode == "forest":
            # 숲소리 게임
            detected_sound = None
            for sound_file, keywords in FOREST_SOUNDS.items():
                for keyword in keywords:
                    if keyword.lower() in user_text_lower:
                        detected_sound = sound_file
                        break
                if detected_sound:
                    break
            
            if detected_sound:
                result = f"✅ CORRECT! You identified {detected_sound.replace('.mp3', '').replace('_', ' ')}"
                is_correct = True
            else:
                result = "❌ WRONG! That is not a forest sound."
                is_correct = False
                
        else:  # non_forest
            # 비숲소리 게임
            detected_non_forest = None
            for sound_type, keywords in NON_FOREST_SOUNDS.items():
                for keyword in keywords:
                    if keyword.lower() in user_text_lower:
                        detected_non_forest = sound_type
                        break
                if detected_non_forest:
                    break
            
            if detected_non_forest:
                result = f"✅ CORRECT! You found the non-forest sound: {detected_non_forest.replace('_', ' ')}"
                is_correct = True
            else:
                result = "❌ WRONG! Find the artificial/mechanical sound!"
                is_correct = False

        # 3. 즉시 응답 (TTS 없음)
        total_time = (datetime.now() - start_time).total_seconds()
        
        return JSONResponse({
            "success": True,
            "ultra_fast_mode": True,
            "processing_time_seconds": round(total_time, 2),
            "user_text": user_text,
            "result": result,
            "is_correct": is_correct,
            "game_mode": game_mode,
            "detected_sound": detected_sound.replace('.mp3', '').replace('_', ' ') if game_mode == "forest" and detected_sound else None,
            "detected_non_forest": detected_non_forest.replace('_', ' ') if game_mode == "non_forest" and detected_non_forest else None,
            "note": "No GPT or TTS for maximum speed",
            "timestamp": datetime.now().isoformat()
        })

    except Exception as e:
        print(f"초고속 모드 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/timeout-safe/voice-check") 
async def timeout_safe_voice_check(
    file: UploadFile = File(...),
    max_processing_time: int = Form(15)  # 최대 처리 시간 (초)
):
    """타임아웃 안전 모드 - 시간 제한 내에서 최대한 처리"""
    import asyncio
    
    try:
        start_time = datetime.now()
        
        async def quick_process():
            # STT
            audio_data = await file.read()
            openai_client = openai.OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
            
            audio_file = io.BytesIO(audio_data)
            audio_file.name = "audio.wav"
            
            transcript = openai_client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file
            )
            
            user_text = transcript.text.strip()
            
            # 키워드 매칭
            detected = None
            for sound_file, keywords in FOREST_SOUNDS.items():
                for keyword in keywords:
                    if keyword.lower() in user_text.lower():
                        detected = sound_file
                        break
                if detected:
                    break
            
            return {
                "user_text": user_text,
                "detected": detected,
                "is_correct": detected is not None
            }
        
        # 타임아웃과 함께 실행
        try:
            result = await asyncio.wait_for(quick_process(), timeout=max_processing_time)
            
            processing_time = (datetime.now() - start_time).total_seconds()
            
            return JSONResponse({
                "success": True,
                "timeout_safe": True,
                "processing_time_seconds": round(processing_time, 2),
                "max_allowed_time": max_processing_time,
                "user_text": result["user_text"],
                "is_correct": result["is_correct"],
                "detected_sound": result["detected"].replace('.mp3', '').replace('_', ' ') if result["detected"] else None,
                "result": "✅ CORRECT!" if result["is_correct"] else "❌ WRONG!",
                "timestamp": datetime.now().isoformat()
            })
            
        except asyncio.TimeoutError:
            return JSONResponse({
                "success": False,
                "timeout_occurred": True,
                "processing_time_seconds": max_processing_time,
                "error": f"Processing took longer than {max_processing_time} seconds",
                "recommendation": "Try ultra-fast mode or increase timeout"
            })
            
    except Exception as e:
        print(f"타임아웃 안전 모드 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

# ============ Unity 연동 테스트 엔드포인트 ============

@app.post("/unity/test-mixed-audio")
async def unity_test_endpoint(file: UploadFile = File(...)):
    """Unity에서 테스트용 중첩 오디오 전송"""
    try:
        print("Unity 테스트 - 중첩 오디오 수신")
        
        # 파일 저장
        temp_path = f"unity_test_{uuid.uuid4()}.wav"
        audio_data = await file.read()
        
        with open(temp_path, "wb") as f:
            f.write(audio_data)
        
        print(f"Unity 오디오 저장: {temp_path}")
        print(f"파일 크기: {len(audio_data)} bytes")
        
        # 간단한 오디오 정보 분석
        import librosa
        y, sr = librosa.load(temp_path, duration=3.0)
        duration = len(y) / sr
        
        # 임시 파일 삭제
        os.remove(temp_path)
        
        return JSONResponse({
            "success": True,
            "message": "Unity 오디오 수신 성공!",
            "audio_info": {
                "duration": f"{duration:.2f}초",
                "sample_rate": sr,
                "file_size": f"{len(audio_data)} bytes"
            },
            "next_steps": "이제 /escape-game/analyze 엔드포인트를 사용하세요"
        })
        
    except Exception as e:
        print(f"Unity 테스트 실패: {e}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    print("스마트 음성 채팅 서버 시작!")
    uvicorn.run(app, host="0.0.0.0", port=8000)
