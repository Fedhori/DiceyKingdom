# 스킬 (Skill)

이 문서는 스킬 시스템의 데이터/상태/UI 규칙을 정리합니다.

## 문서 범위/분리 기준

- 전체 턴/상태머신: `Docs/GAME_STRUCTURE.md`
- 이 문서: 스킬 데이터 스키마, 현재 동작 상태, UI 표시 규칙

## 데이터 정의 (`Assets/StreamingAssets/Data/Skills.json`)

- `skillId`: 고유 ID
- `nameKey`: 로컬라이즈 키
- `cooldownTurns`: 기본 쿨다운 턴
- `maxUsesPerTurn`: 턴당 사용 횟수
- `effectBundle`: 효과 묶음 (`effects[]`)

## v0 기본 스킬 3종 (현재 데이터)

1. `skill_reroll_agent`
   - 효과: `rerollAgentDice` (`targetAgentMode=selectedAgent`, `rerollRule=all`)
2. `skill_die_face_plus_one`
   - 효과: `dieFaceDelta +1` (`targetAgentMode=selectedAgent`, `diePickRule=selected`)
3. `skill_reduce_situation_requirement_two`
   - 효과: `situationRequirementDelta -2` (`targetMode=selectedSituation`)

## 현재 동작 상태 (주사위 대결 전환 단계)

- `GameManager.TryUseSkillBySlotIndex`는 현재 `false`를 반환하며 실제 적용은 비활성 상태입니다.
- 스킬 쿨다운 상태(`SkillCooldownState`)는 런타임 상태에 유지됩니다.
- 턴 시작 시 쿨다운 감소 및 턴당 사용 플래그 초기화는 유지됩니다.
- 하단 액션 바(`BottomActionBarController`)는 슬롯 상태 UI를 표시하지만, 실제 캐스팅은 비활성입니다.

## UI/입력 규칙

- 하단 액션 바: `Assets/Scripts/Game/UI/BottomActionBarController.cs`
- 타깃 세션 관리: `Assets/Scripts/Game/UI/SkillTargetingSession.cs`
- 단축키: `Assets/Scripts/Game/UI/GameTurnHotkeyController.cs` (QWER)

## 재활성화 시 주의사항

- 현재 전투 루프가 `요구치 감소`가 아닌 `주사위 대결`로 바뀌었기 때문에, `situationRequirementDelta` 계열 스킬은 효과 재정의가 필요합니다.
- 타깃 지정형 스킬(상황/주사위 단일 선택)은 새 루프의 선택 단계와 충돌 없이 동작하도록 입력 우선순위를 다시 고정해야 합니다.

