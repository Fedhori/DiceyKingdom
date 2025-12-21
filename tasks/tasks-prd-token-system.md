## Relevant Files

- `Assets/Scripts/Data/StaticDataManager.cs` - 토큰 JSON 로드 초기화 지점 확장.
- `Assets/StreamingAssets/Data/Tokens.json` - 새 토큰 정의 JSON 추가 위치.
- `Assets/Scripts/Token/TokenDto.cs` - 토큰 DTO/리포지토리 정의(신규).
- `Assets/Scripts/Token/TokenInstance.cs` - 토큰 인스턴스/트리거 처리(신규).
- `Assets/Scripts/Token/TokenController.cs` - 토큰 UI 슬롯 제어(신규).
- `Assets/Scripts/Token/TokenManager.cs` - 7슬롯 덱 관리, 드래그/스왑, 트리거 실행(신규).
- `Assets/Scripts/Token/TokenEffectManager.cs` - 토큰 이펙트 실행 파이프라인(신규, 핀과 분리).
- `Assets/Scripts/Flow/FlowManager.cs` - 스테이지 종료 시 토큰 트리거 호출 지점 연동.
- `Assets/Scripts/Dev/DevCommandManager.cs` - `addToken` 명령어 추가.
- `Assets/Scripts/Player/PlayerManager.cs` - 토큰 효과로 변경될 스탯 적용 확인 지점.
- `Assets/Scripts/Stat/GameStat.cs` - 스탯 수정 시 사용되는 공통 구조 재사용.
- `Assets/Scripts/UI` 하위(토큰 덱 UI 프리팹/슬롯 뷰 파일들) - HorizontalLayoutGroup 기반 덱 UI 구성.

### Notes

- 단위 테스트가 있다면 스크립트 인접 경로에 배치한다.
- Unity Input System 기반 드래그 처리를 고려해 PinDragManager와 충돌하지 않도록 레이어/오브젝트 분리.

## Tasks

- [ ] 1.0 토큰 데이터 파이프라인 구축(Tokens.json, DTO, Repository, StaticDataManager 연동)
  - [x] 1.1 `Assets/StreamingAssets/Data/Tokens.json` 생성 및 기본 스키마 확정(id, price/rarity 등 메타 포함, rules 배열).
  - [x] 1.2 `TokenDto`, `TokenRule/Condition/Effect` DTO와 `TokenRepository` 구현(초기화, 검증, Get/TryGet).
  - [x] 1.3 `StaticDataManager`에서 Tokens.json을 로드/역직렬화해 리포지토리 초기화.
- [ ] 2.0 토큰 런타임 객체/이펙트 파이프라인 분리 구현(TokenInstance, TokenEffectManager, TokenTriggerType/Condition/Effect)
  - [x] 2.1 `TokenTriggerType` 정의(핀과 분리) 및 조건/이펙트 파서/DTO 분리본 구성.
  - [x] 2.2 `TokenInstance`에서 트리거 평가·조건 체크·이펙트 실행 요청 처리(핵심 상태 포함).
  - [x] 2.3 `TokenEffectManager`에서 토큰 전용 이펙트 실행 로직 구현(스탯 수정 등 공통 이펙트 지원, 핀과 분리).
- [ ] 3.0 토큰 덱 관리/드래그 UI 구현(TokenManager, 7슬롯, HorizontalLayoutGroup 스왑)
  - [x] 3.1 `TokenManager`에서 7슬롯 리스트 관리(빈 슬롯 추가, 가득 찼을 때 거부/로그, 순서 보존).
  - [x] 3.2 HorizontalLayoutGroup 기반 덱 UI와 `TokenController` 슬롯 컴포넌트 구현(표시/하이라이트).
  - [x] 3.3 덱 내 드래그 스왑/재정렬 처리(슬롯 인덱스 변환, PinDragManager와 충돌 방지).
- [ ] 4.0 `token.teacher` 정의/적용(OnStageFinished 시 플레이어 기본 점수 영구 +10)
  - [x] 4.1 Tokens.json에 `token.teacher` 항목 추가(Trigger: OnStageStarted, Condition: Always, Effect: score +10, Permanent).
  - [x] 4.2 Token 이펙트 실행 시 플레이어 기본 점수에 영구 버프 적용되는지 확인(StatLayer.Permanent).
- [ ] 5.0 FlowManager에 토큰 트리거 실행 연동(Play→Reward 전환 시점)
  - [x] 5.1 `FlowManager.OnStagePlayFinished` 흐름 내 토큰 트리거 호출 지점 삽입(스테이지 클리어 경로에서 보상/상점 전에 실행).
  - [x] 5.2 실패/게임오버 경로에서 불필요한 토큰 발동이 없도록 가드 처리.
- [ ] 6.0 DevCommand `addToken <tokenId> <slotIndex>` 추가 및 유효성/로그 처리
  - [x] 6.1 DevCommandManager에 `addToken` 명령 추가(파라미터 파싱, 범위/빈 슬롯/존재 여부 검증).
  - [x] 6.2 TokenManager에 슬롯 삽입 API 추가/호출, 실패 사유 로깅.
