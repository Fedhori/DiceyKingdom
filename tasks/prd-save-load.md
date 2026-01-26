# PRD: Save/Load (Run Save System)

## 1. 소개/개요
이 기능은 런 진행 상태를 저장/로드하여 "이어하기"를 가능하게 한다. 현재 프로젝트에는 세이브/로드 로직이 없으며, 런 시작 시점의 상태를 엄밀하게 재현하는 것이 목표다. 저장 실패/로드 실패 시에는 상세한 디버그 로그와 사용자용 모달 안내가 필요하다.

## 2. 목표
- 스테이지 시작 시점에 런 상태를 자동 저장한다.
- 메인 메뉴에서 이어하기 버튼을 통해 동일한 런 상태로 로드한다.
- 저장/로드 시 검증 로직과 백업 로직을 통해 데이터 무결성을 보장한다.
- 저장 실패/로드 실패 시 개발자가 쉽게 추적 가능한 상세 로그를 남기고, 사용자에게는 간단한 안내를 표시한다.

## 3. 사용자 스토리
- 플레이어로서, 게임을 종료해도 이어하기로 같은 런을 계속할 수 있다.
- 플레이어로서, 저장 파일이 손상되었을 때 안내를 받고 백업으로 이어서 플레이할 수 있다.
- 개발자로서, 저장/로드 실패 원인을 빠르게 파악할 수 있도록 상세 로그를 확인할 수 있다.

## 4. 기능 요구사항
1) 저장 파일 경로와 파일명
- 저장 경로는 `Application.persistentDataPath/saves/save.json`으로 한다.
- 백업 파일은 `save_backup.json`, 손상 파일은 `save_invalid.json`을 사용한다.

2) 저장 트리거 시점
- 스테이지 시작 시점에 자동 저장한다.
- 구체적으로 `StageManager.StartStage` 내부에서 `CurrentStage`가 확정된 뒤, `OnPlayStart` 호출 직전에 저장한다.

3) 저장 데이터 범위 (필수)
- 메타: `schemaVersion`, `appVersion`, `timestampUtc`, `runSeed`, `checksum`
- 런: `stageIndex`
- 플레이어: `playerId`, `currency`, `permanentStatModifiers`
- 인벤토리: 슬롯 순서 유지, 슬롯별 `itemId`, `itemUniqueId`, `permanentItemStatModifiers`, `upgrades`
- 업그레이드: 장착 업그레이드와 미장착 업그레이드 모두 `upgradeId`, `upgradeUniqueId` 저장
- 영구 스탯 모디파이어는 `statId`, `op`, `value`, `source`, `priority`, `layer` 포함
- `triggerRepeatStats`는 저장하지 않는다. (업그레이드 재적용으로 복원)

4) StatLayer 처리 정책
- 로드 시 아이템/업그레이드로 재계산하되, `StatLayer.Permanent`는 저장값을 복원한다.
- 로드 모드에서 트리거는 실행하되, `duration=Permanent` 효과는 필터링한다.

5) 로드 직후 보정 처리
- 로드 직후 `OnCurrencyChanged`와 `OnItemChanged`를 1회 실행하여 Owned/조건부 효과를 맞춘다.

6) RNG
- `runSeed`만 저장한다.
- 로드 시 `GameManager.Rng = new System.Random(runSeed)`로 재시드한다.
- UnityEngine.Random.state는 저장하지 않는다.

7) 저장 검증
- 필수 필드 누락, 형식 오류, ID 불일치, 슬롯 범위 초과 등의 검증을 수행한다.
- `fieldPath`, `expected`, `actual`을 포함한 구조화 로그를 남긴다.
- 저장 실패 시 모달로 경고하고, 세이브는 완료되지 않은 것으로 처리한다.

8) 로드 검증
- 저장 검증과 동일한 수준의 필드 검증을 수행한다.
- 유효하지 않은 `save.json`은 `save_invalid.json`으로 이동한다.
- `save.json`이 invalid이면 `save_backup.json`을 검증하고 유효하면 백업을 사용한다.
- 백업 로드 시 안내 모달을 표시한다.

9) 백업 로직
- 저장 성공 시 기존 `save.json`을 `save_backup.json`으로 교체한다.
- 원자적 저장을 위해 `save.tmp` → 검증 → `save.json` 교체 순서를 사용한다.

10) 게임오버/클리어 처리
- 게임오버/게임클리어 시 `save.json`과 `save_backup.json`을 삭제한다.

11) 이어하기 UI 동작
- 이어하기 버튼은 유효한 `save.json` 또는 `save_backup.json`이 있을 때만 노출한다.
- 이어하기 버튼 클릭 시 로드 플로우를 시작한다.

12) 로컬라이징
- 모달 텍스트는 로컬라이징 테이블 `modal`의 키를 사용한다. 코드 하드코딩 금지.
- 키/문구(초안):
  - `modal.saveFailed.title`: "세이브 실패"
  - `modal.saveFailed.message`: "세이브 파일 저장에 실패했습니다. 게임 종료 시 진행이 저장되지 않을 수 있습니다."
  - `modal.loadFailed.title`: "불러오기 실패"
  - `modal.loadFailed.message`: "세이브 파일이 손상되어 불러올 수 없습니다."
  - `modal.loadBackup.title`: "백업으로 불러오기"
  - `modal.loadBackup.message`: "세이브 파일이 손상되어 백업 파일로 불러옵니다."

13) 로깅
- Unity 콘솔 + `Application.persistentDataPath/saves/save_log.txt`에 로그를 남긴다.

## 5. 비목표 (Out of Scope)
- 스테이지 진행 중 즉시 저장(체크포인트) 기능
- 다중 세이브 슬롯
- 클라우드 동기화
- UnityEngine.Random 결정성 보장
- WebGL 전용 저장 flush/내보내기 기능

## 6. 디자인 고려사항
- 메인 메뉴에 이어하기 버튼이 필요하며, 유효한 세이브가 있을 때만 표시한다.
- 모달은 `ModalManager`를 사용하고 로컬라이징 키로 표시한다.

## 7. 기술 고려사항
- 저장 데이터는 Json 기반으로 구성한다 (Newtonsoft.Json 사용 가능).
- `UniqueId` 복원을 위해 Item/Upgrade 인스턴스 생성 경로에 UniqueId 주입 기능이 필요하다.
- 기존 시작 플로우 (`GameManager.Start` → `ItemManager.InitializeFromPlayer` → `StageManager.StartRun`)는 로드 모드에서 우회 또는 분기 처리해야 한다.
- 저장 시점은 `StageManager.StartStage` 내부, `OnPlayStart` 호출 직전으로 고정한다.
- Permanent 필터링은 `ItemEffectManager` 또는 적용 경로에서 로드 모드 플래그로 처리한다.
- 체크섬은 파일 본문(체크섬 필드 제외) 기준으로 계산한다.

## 8. 성공 지표
- 유효한 세이브는 100% 로드 성공
- 세이브/로드 실패 시 상세 로그에 `fieldPath/expected/actual` 기록
- 세이브 파일 손상 시 백업으로 정상 이어하기 가능

## 9. 오픈 질문
- 없음
