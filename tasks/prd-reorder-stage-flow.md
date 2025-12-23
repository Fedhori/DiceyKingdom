# PRD - 스테이지 단계 순서 변경 (Shop → Play → Reward)

## 1. 개요
- 목표: 스테이지 진행 순서를 기존 `Play → Reward → Shop`에서 `Shop → Play → Reward`로 변경한다.
- 예외: 최초 스테이지(인덱스 0)는 Shop 단계를 건너뛴 뒤 Play → Reward로 진행한다.
- 기존 로직(트리거, 점수 판정, 실패 처리 등)은 가능한 한 유지한다.

## 2. 목표
- 스테이지 0: Shop 없이 Play → Reward로 정상 진행.
- 스테이지 1 이상: Shop → Play → Reward 순서를 따른다.
- 실패 처리(점수 미달 등)는 기존과 동일하게 GameOver로 종료하고 이후 단계를 진행하지 않는다.
- 기존 트리거(OnStageStart 등)는 시점 변경 없이 유지된다.

## 3. 유저 스토리
1) 플레이어로서, 스테이지 시작 시 먼저 상점에서 핀/토큰을 준비한 뒤 라운드를 플레이하고, 보상을 받는 흐름을 원한다.
2) 플레이어로서, 첫 스테이지는 튜토리얼처럼 바로 플레이 후 보상을 받도록 Shop을 건너뛰길 원한다.
3) 플레이어로서, 스테이지 실패 시 기존과 동일하게 즉시 게임 오버로 처리되길 원한다.

## 4. 기능 요구사항
1) 스테이지 0은 StartStage → Play → Reward로 진행하며 Shop 단계를 건너뛴다.
2) 스테이지 1 이상은 StartStage → Shop → Play → Reward 순서로 진행한다.
3) Play 실패(점수 미달 등) 시 기존과 동일하게 GameOver로 종료하며 Shop/Reward를 진행하지 않는다.
4) 트리거/이벤트(예: OnStageStart/OnStageFinished)는 기존 시점 그대로 유지하며, 단계 순서 변경만 적용한다.
5) NextStage로 이동 시 새 순서 규칙을 반복 적용한다.

## 5. 비범위 (Out of Scope)
- 상점/보상/플레이 내부 로직 변경 없음.
- 트리거/이벤트의 시점 재정의 없음.
- UI/애니메이션 추가 변경 없음.

## 6. 설계 고려사항
- FlowPhase(Enum: None, Play, Reward, Shop) 유지. 단계 전환 순서만 변경.
- StageManager/FlowManager 내 StartStage, OnPlayFinished, OnRewardClosed, OnShopClosed 등의 호출 순서를 재구성하되, 기존 함수 재사용 권장.
- 최초 스테이지 판정은 `currentStageIndex == 0` 기반으로 처리.

## 7. 성공 지표
- 스테이지 0: 게임 시작 시 Shop 없이 Play → Reward로 정상 종료.
- 스테이지 1+: Shop → Play → Reward 순서로 정상 진행.
- 실패 시 기존 GameOver 플로우가 유지되고, Shop/Reward로 넘어가지 않음.

## 8. 남은 질문
- 없음 (요구사항 확정됨).*** End Patch ***!
