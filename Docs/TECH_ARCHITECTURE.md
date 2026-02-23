# TECH_ARCHITECTURE
> 역할: 현재 구현 기준 아키텍처 요약.

- 마지막 갱신: `2026-02-23`

---

## 1) 계층

- Domain
  - `DuelState`, `ClashState`, `AbilityInstance`
  - 순수 상태/계산(`DuelSimulator`)
- Application
  - `DuelPhaseRunner`
  - `DuelSessionBuilder`
  - `DuelTurnProcessor`
  - `AbilityTimedEffectRunner`, `DuelEffectClashResolver`
- Infrastructure
  - `GameDatabaseLoader`, `GameDataValidator`
  - StreamingAssets JSON 로딩(Newtonsoft.Json)
- Presentation
  - `DuelDebugPanel`, `DuelAbilityBlockView`

---

## 2) 데이터 파이프라인

1. `Data/DataIndex.json` 로드
2. `configs`, `clashes`, `abilities`, `encounters` 순서 파싱
3. `GameDataValidator` 검증
4. 성공 시 `GameDataRuntime.CurrentDatabase`에 반영

---

## 3) 전투 실행 파이프라인

1. `DuelSessionBuilder.TryCreateInitialState`
2. `DuelPhaseRunner.StartDuel`
3. `AutoDeployOpponentIntent`
4. 플레이어 배치
5. `DuelTurnProcessor.TryRollAllDeployedAbilities`
6. `DuelTurnProcessor.TryClashResolveAllClashes`

---

## 4) 설계 원칙

- 신규 전투 데이터 단위는 Ability로 통일
- 구용어는 신규 코드에서 사용 금지
- 조용한 자동 보정은 피하고, 필요한 경우 최소 Warning 로그를 남긴다
- UI 배치는 가능한 한 에디터에서 처리하고 코드 배치는 불가피한 경우만 사용한다
