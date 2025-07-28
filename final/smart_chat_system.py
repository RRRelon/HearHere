import openai
from typing import Dict, List, Optional
import uuid
from datetime import datetime
import asyncio
from vector_db_manager import VectorDBManager


class SmartChatSystem:
    def __init__(self, openai_api_key: str, vector_db_path: str = "./chroma_db"):
        """스마트 채팅 시스템 초기화"""
        # OpenAI 클라이언트 설정
        openai.api_key = openai_api_key
        self.openai_client = openai.OpenAI(api_key=openai_api_key)

        # VectorDB 매니저 초기화
        self.vector_db = VectorDBManager(vector_db_path)

        # 대화 히스토리 관리
        self.conversations: Dict[str, List[Dict]] = {}

        # 설정
        self.similarity_threshold = 0.8  # 캐시 매치 임계값
        self.learning_enabled = True     # 자동 학습 활성화
        self.gpt_model = "gpt-3.5-turbo"

        # 시스템 프롬프트
        self.system_prompt = """You are a helpful AI assistant for a Unity game. 
        Respond in English and be friendly and encouraging. 
        Help users with game-related questions, controls, and general gameplay.
        Keep responses concise but informative."""

        print("스마트 채팅 시스템 초기화 완료")

    async def chat(self, user_input: str, conversation_id: str = None) -> Dict:
        """스마트 채팅 처리 (VectorDB → GPT 순서)"""
        if not conversation_id:
            conversation_id = str(uuid.uuid4())

        start_time = datetime.now()
        response_source = "unknown"

        try:
            # 1단계: VectorDB에서 유사한 응답 검색
            print(f"VectorDB 검색 중: {user_input[:30]}...")
            cached_response = self.vector_db.search_similar_response(
                user_input,
                threshold=self.similarity_threshold
            )

            if cached_response:
                # 캐시된 응답 사용
                ai_response = cached_response
                response_source = "cache"
                print(f"캐시 응답 사용 (빠름!)")

            else:
                # 2단계: GPT API 호출
                print(f"GPT에게 질문 중...")
                ai_response = await self._call_gpt_api(user_input, conversation_id)
                response_source = "gpt"

                # 3단계: 새로운 응답 자동 학습
                if self.learning_enabled and ai_response:
                    await self._learn_new_response(user_input, ai_response, conversation_id)

            # 대화 히스토리 업데이트
            self._update_conversation_history(
                conversation_id, user_input, ai_response)

            # 응답 시간 계산
            response_time = (datetime.now() - start_time).total_seconds()

            return {
                "success": True,
                "user_input": user_input,
                "ai_response": ai_response,
                "conversation_id": conversation_id,
                "response_source": response_source,
                "response_time_seconds": round(response_time, 2),
                "timestamp": datetime.now().isoformat()
            }

        except Exception as e:
            print(f"채팅 처리 실패: {e}")
            response_time = (datetime.now() - start_time).total_seconds()

            return {
                "success": False,
                "error": str(e),
                "user_input": user_input,
                "response_time_seconds": round(response_time, 2),
                "timestamp": datetime.now().isoformat()
            }

    async def _call_gpt_api(self, user_input: str, conversation_id: str) -> str:
        """OpenAI GPT API 호출"""
        try:
            # 대화 히스토리 가져오기
            messages = self._build_message_history(conversation_id, user_input)

            # GPT API 호출
            response = self.openai_client.chat.completions.create(
                model=self.gpt_model,
                messages=messages,
                max_tokens=500,
                temperature=0.7,
                timeout=30
            )

            ai_response = response.choices[0].message.content.strip()
            print(f"GPT 응답 생성 완료")
            return ai_response

        except Exception as e:
            print(f"GPT API 호출 실패: {e}")
            return "죄송합니다. 일시적인 오류가 발생했습니다. 다시 시도해주세요."

    async def _learn_new_response(self, user_input: str, ai_response: str, conversation_id: str):
        """새로운 응답 자동 학습"""
        try:
            success = self.vector_db.add_response(
                user_input=user_input,
                ai_response=ai_response,
                conversation_id=conversation_id
            )

            if success:
                print(f"자동 학습 완료: {user_input[:20]}...")
            else:
                print(f"자동 학습 실패")

        except Exception as e:
            print(f"자동 학습 중 오류: {e}")

    def _build_message_history(self, conversation_id: str, current_input: str) -> List[Dict]:
        """GPT API용 메시지 히스토리 구성"""
        messages = [{"role": "system", "content": self.system_prompt}]

        # 기존 대화 히스토리 추가 (최근 10개)
        if conversation_id in self.conversations:
            recent_history = self.conversations[conversation_id][-10:]
            for exchange in recent_history:
                messages.append({"role": "user", "content": exchange["user"]})
                messages.append(
                    {"role": "assistant", "content": exchange["assistant"]})

        # 현재 사용자 입력 추가
        messages.append({"role": "user", "content": current_input})

        return messages

    def _update_conversation_history(self, conversation_id: str, user_input: str, ai_response: str):
        """대화 히스토리 업데이트"""
        if conversation_id not in self.conversations:
            self.conversations[conversation_id] = []

        self.conversations[conversation_id].append({
            "user": user_input,
            "assistant": ai_response,
            "timestamp": datetime.now().isoformat()
        })

        # 히스토리 크기 제한 (메모리 관리)
        if len(self.conversations[conversation_id]) > 50:
            self.conversations[conversation_id] = self.conversations[conversation_id][-30:]

    def get_system_stats(self) -> Dict:
        """시스템 통계 정보"""
        vector_stats = self.vector_db.get_stats()

        return {
            "vector_db_stats": vector_stats,
            "active_conversations": len(self.conversations),
            "learning_enabled": self.learning_enabled,
            "similarity_threshold": self.similarity_threshold,
            "gpt_model": self.gpt_model,
            "system_status": "online"
        }

    def update_settings(self, **kwargs):
        """시스템 설정 업데이트"""
        if "similarity_threshold" in kwargs:
            self.similarity_threshold = float(kwargs["similarity_threshold"])
            print(f"유사도 임계값 변경: {self.similarity_threshold}")

        if "learning_enabled" in kwargs:
            self.learning_enabled = bool(kwargs["learning_enabled"])
            print(f"자동 학습 {'활성화' if self.learning_enabled else '비활성화'}")

        if "gpt_model" in kwargs:
            self.gpt_model = kwargs["gpt_model"]
            print(f"GPT 모델 변경: {self.gpt_model}")

    def initialize_basic_data(self):
        """기본 데이터 초기화"""
        print("기본 게임 데이터 초기화...")
        self.vector_db.initialize_basic_data()
        print("기본 데이터 초기화 완료!")

    def backup_data(self, filename: str = None):
        """데이터 백업"""
        if not filename:
            filename = f"chat_backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"

        return self.vector_db.export_data(filename)

    def reset_vector_db(self):
        """VectorDB 초기화"""
        return self.vector_db.reset_database()

    def clear_conversation(self, conversation_id: str):
        """특정 대화 히스토리 삭제"""
        if conversation_id in self.conversations:
            del self.conversations[conversation_id]
            print(f"대화 히스토리 삭제: {conversation_id[:8]}...")

    def clear_all_conversations(self):
        """모든 대화 히스토리 삭제"""
        self.conversations.clear()
        print("모든 대화 히스토리 삭제 완료")

    async def get_similar_questions(self, user_input: str, limit: int = 3) -> List[Dict]:
        """유사한 질문들 반환 (디버깅용)"""
        try:
            results = self.vector_db.collection.query(
                query_texts=[user_input],
                n_results=limit,
                include=["metadatas", "distances", "documents"]
            )

            similar_questions = []
            for i, doc in enumerate(results["documents"][0]):
                distance = results["distances"][0][i]
                similarity = 1 - distance

                similar_questions.append({
                    "question": doc,
                    "response": results["metadatas"][0][i].get("ai_response", ""),
                    "similarity": round(similarity, 3),
                    "timestamp": results["metadatas"][0][i].get("timestamp", "")
                })

            return similar_questions

        except Exception as e:
            print(f"유사 질문 검색 실패: {e}")
            return []
