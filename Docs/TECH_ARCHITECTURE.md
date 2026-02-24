# TECH_ARCHITECTURE
> 역할: 최신 기획 기준 아키텍처 요약.

- 마지막 갱신: `2026-02-24`

---

## 1) 계층

- Domain
  - `DuelState`, `CombatState`, `AbilityInstance`
  - 순수 상태/계산(`DuelSimulator`)
- Application
  - `DuelPhaseRunner`
  - `DuelSessionBuilder`
  - `DuelTurnProcessor`
  - `AbilityTimedEffectRunner`, `DuelEffectCombatResolver`
- Infrastructure
  - `GameDatabaseLoader`, `GameDataValidator`
  - StreamingAssets JSON 로딩(`Newtonsoft.Json`)
- Presentation
  - `DuelDebugPanel`, `DuelAbilityBlockView`

---

## 2) 데이터 파이프라인

1. `Data/DataIndex.json` 로드
2. `configs`, `abilities`, `enemies` 순서 파싱
3. `GameDataValidator` 검증
4. 성공 시 `GameDataRuntime.CurrentDatabase`에 반영

---

## 3) 전투 실행 파이프라인

1. `DuelSessionBuilder.TryCreateInitialState(enemyId)`
2. `DuelPhaseRunner.StartDuel`
3. `OpponentSetup`에서 적 Ability 무작위 Combat 배치
4. 플레이어 배치
5. `DuelTurnProcessor.TryRollAllDeployedAbilities`
6. `DuelTurnProcessor.TryResolveAllCombats`

---

## 4) 핵심 설계 규칙

- Combat은 런타임 고정 3개로 생성한다.
- Combat은 ID가 아니라 `combatIndex(0~2)`로 참조한다.
- 적 배치는 Pattern 기반이 아니라 `Enemy abilityLoadout` 기반이다.
- 조용한 자동 보정은 피하고 필요한 경우 `Warning` 로그를 남긴다.
- UI 배치는 가능한 한 에디터에서 처리하고 코드 배치는 불가피한 경우만 사용한다.
