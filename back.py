import os
import uuid
import shutil
from pathlib import Path

# --- 라이브러리 임포트 ---
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.responses import FileResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from openai import OpenAI
from dotenv import load_dotenv

# --- [수정] PyTorch 라이브러리 추가 (device 확인용) ---
try:
    import torch
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False


# --- 초기 설정 ---
load_dotenv()
app = FastAPI()

# OpenAI 클라이언트 초기화
try:
    client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))
    OPENAI_CONFIGURED = True
except Exception:
    client = None
    OPENAI_CONFIGURED = False

# CORS 미들웨어 설정
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

TEMP_DIR = Path("temp_files")
TEMP_DIR.mkdir(exist_ok=True)

conversations = {}

# --- Pydantic 모델 정의 ---
class TTSRequest(BaseModel):
    text: str
    exaggeration: float = 0.5
    cfg_weight: float = 0.5
    audio_prompt_path: str | None = None

class ChatRequest(BaseModel):
    message: str
    conversation_id: str | None = None

# --- 모의 TTS 함수 (실제 Chatterbox 코드로 대체 필요) ---
def generate_tts_with_chatterbox(text: str, exaggeration: float, cfg_weight: float) -> bytes:
    print(f"--- [모의 TTS 호출] 텍스트: {text} ---")
    # 실제 Chatterbox 라이브러리가 로드되지 않았으므로 이 부분은 항상 모의로 동작합니다.
    dummy_wav_data = (
        b'RIFF\x24\x00\x00\x00WAVEfmt \x10\x00\x00\x00\x01\x00\x01\x00'
        b'\x44\xac\x00\x00\x88\x58\x01\x00\x02\x00\x10\x00data\x00\x00\x00\x00'
    )
    return dummy_wav_data

# --- API 엔드포인트 구현 ---

# --- [수정] 테스트 스크립트와 호환되도록 상세 정보 추가 ---
@app.get("/health", summary="서버 상태 확인")
def health_check():
    """서버의 상세 상태 정보를 반환합니다."""
    device = "N/A"
    if TORCH_AVAILABLE:
        device = "cuda" if torch.cuda.is_available() else "cpu"
        
    return {
        "status": "ok",
        "message": "서버가 정상적으로 동작하고 있습니다.",
        "openai_configured": OPENAI_CONFIGURED,
        "chatterbox_loaded": False,  # 실제 라이브러리가 없으므로 False로 고정
        "device": device
    }

@app.post("/stt", summary="STT (Speech-to-Text)")
async def speech_to_text(file: UploadFile = File(...)):
    if not OPENAI_CONFIGURED:
        raise HTTPException(status_code=500, detail="OpenAI 클라이언트가 초기화되지 않았습니다.")
    try:
        transcription = client.audio.transcriptions.create(
            model="whisper-1", file=file.file, response_format="json"
        )
        return {"success": True, "text": transcription.text}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"STT 처리 중 오류 발생: {e}")

@app.post("/chat", summary="AI 채팅")
async def chat_with_ai(request: ChatRequest):
    if not OPENAI_CONFIGURED:
        raise HTTPException(status_code=500, detail="OpenAI 클라이언트가 초기화되지 않았습니다.")
    conv_id = request.conversation_id or str(uuid.uuid4())
    if conv_id not in conversations:
        conversations[conv_id] = [{"role": "system", "content": "당신은 사용자와 대화하는 친절한 AI 어시스턴트입니다."}]
    conversations[conv_id].append({"role": "user", "content": request.message})
    try:
        response = client.chat.completions.create(
            model="gpt-3.5-turbo", messages=conversations[conv_id]
        )
        ai_message = response.choices[0].message.content
        conversations[conv_id].append({"role": "assistant", "content": ai_message})
        return {
            "success": True, 
            "ai_response": ai_message, # --- [수정] 키 이름 변경: response -> ai_response ---
            "conversation_id": conv_id
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"AI 채팅 처리 중 오류 발생: {e}")

@app.post("/tts", summary="TTS (Text-to-Speech)")
async def text_to_speech(request: TTSRequest):
    try:
        audio_bytes = generate_tts_with_chatterbox(
            text=request.text, exaggeration=request.exaggeration, cfg_weight=request.cfg_weight
        )
        file_id = str(uuid.uuid4())
        file_path = TEMP_DIR / f"{file_id}.wav"
        with open(file_path, "wb") as f:
            f.write(audio_bytes)
        return {
            "success": True, "audio_file_id": file_id, "download_url": f"/download/{file_id}.wav"
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"TTS 처리 중 오류 발생: {e}")

@app.get("/download/{file_id}.wav", summary="음성 파일 다운로드") # --- [수정] .wav 확장자 추가 ---
async def download_audio(file_id: str):
    file_path = TEMP_DIR / f"{file_id}.wav"
    if not file_path.exists():
        raise HTTPException(status_code=404, detail="파일을 찾을 수 없습니다.")
    return FileResponse(path=file_path, media_type="audio/wav", filename=f"{file_id}.wav")

@app.post("/voice-chat", summary="음성 채팅 (핵심 기능)")
async def voice_chat(
    file: UploadFile = File(...),
    conversation_id: str | None = Form(None),
    exaggeration: float = Form(0.5),
    cfg_weight: float = Form(0.5)
):
    # 1. STT
    stt_result = await speech_to_text(file)
    user_text = stt_result.get("text")

    # 2. Chat
    chat_request = ChatRequest(message=user_text, conversation_id=conversation_id)
    chat_result = await chat_with_ai(chat_request)
    ai_response = chat_result.get("ai_response")
    conv_id = chat_result.get("conversation_id")

    # 3. TTS
    tts_request = TTSRequest(text=ai_response, exaggeration=exaggeration, cfg_weight=cfg_weight)
    tts_result = await text_to_speech(tts_request)
    
    return {
        "success": True,
        "user_text": user_text,
        "ai_response": ai_response,
        "conversation_id": conv_id,
        "audio_file_id": tts_result.get("audio_file_id"),
        "download_url": tts_result.get("download_url")
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)

