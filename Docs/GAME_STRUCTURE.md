# 게임 구조

이 문서는 ProjectP의 게임 구조/시스템과 런타임 흐름을 정리합니다. 레포 지도는 `Docs/PROJECT_MAP.md`, 범용 규칙은 `Docs/GENERAL_RULES.md`를 참고합니다.

## 문서 범위/분리 기준

- 범용 규칙/컨벤션: `Docs/GENERAL_RULES.md`
- 레포 지도(파일 위치/책임 요약): `Docs/PROJECT_MAP.md`
- 이 문서: 게임 구조, 시스템, 데이터/씬 흐름

## 프로덕트 요약 (한 줄)

- 이동하는 플레이어가 자동 발사 아이템으로 낙하 블록을 파괴하며 스테이지를 넘기는 로그라이크.

## 게임 컨셉

- 플레이어는 플레이 영역 안에서 이동하며 아이템이 자동 공격.
- 블록이 상단에서 생성되어 하단으로 낙하하며, 파괴하지 못하면 게임 오버.
- 아이템/업그레이드 조합으로 공격 방식과 상태 효과를 강화.
- 스테이지 종료마다 상점을 통해 성장하고 다음 스테이지로 진행.

## 씬/런타임 흐름

- `Assets/Scenes/Bootstrap.unity`에서 `Bootstrap`이 SaCache 초기화 후 매니저 루트를 활성화하고 `MainMenuScene`을 로드.
- `MainMenuScene`에서 새 게임/이어하기 분기 후 `GameScene`으로 진입.
- 주요 매니저는 매니저 루트에 배치되어 `DontDestroyOnLoad`로 유지.
- 대부분의 매니저는 `public static Instance` 싱글톤 패턴을 사용하며, 중복 생성 시 `Awake`에서 파괴 처리.

## 스테이지 루프

- 스테이지 시작: `StageManager.StartRun` → `StageRepository` 기반으로 스테이지 생성.
- 플레이 단계: `StagePhase.Play`에서 `PlayManager`가 아이템 플레이 시작 + `BlockManager` 스폰 램프 시작.
- 플레이 종료 조건: 스폰 타이머 종료 + 남은 블록 0.
- 게임 오버 조건: 블록이 `BlockDestroyZone`에 도달.
- 보상/상점: 플레이 종료 시 `ResultManager`가 수입 표시 및 통화 지급 후 `ShopManager` 오픈.
- 스테이지 종료: 상점 닫힘 시 아이템 트리거/업그레이드 파손 처리 후 다음 스테이지.
- 마지막 스테이지 종료 시 `GameManager.HandleGameClear` 호출.

## 전투/피해 시스템

- 블록은 `BlockManager`가 패턴(Blocks.json)과 스폰 예산/난수로 생성, 낙하 속도는 스테이지 난이도와 상태에 영향.
- `ItemController`가 공격 주기에 따라 투사체 또는 빔을 발사.
- `DamageManager`가 피해 적용, 크리티컬/상태 적용, 파괴 처리 및 VFX/사운드 트리거.
- 상태 효과는 `StatusUtil`로 키 매핑되며 현재 `Freeze` 중심.

## 아이템/업그레이드

- `Items.json`의 규칙/효과가 `ItemInstance` 트리거로 실행된다.
- 아이템 인벤토리는 `ItemInventory` 슬롯(기본 12)로 관리되며, 오브젝트 아이템은 플레이 중 궤도 배치된다.
- 업그레이드는 `Upgrades.json` 기반, 아이템에 적용하거나 업그레이드 인벤토리에 보관한다.
- 스테이지 종료 시 업그레이드 파손 확률 처리가 발생한다.
- `ModifyStatStack` 효과는 아이템 단위로 스택 값을 관리해 스탯을 누적/감쇠한다.
- 스택은 스테이지 시작/종료 시 아이템 런타임 상태 초기화에 따라 리셋된다.
- `minValue`/`maxValue`로 스택 하한/상한을 제한할 수 있다.
- `Chance` 조건은 0~1 범위 확률로 트리거를 통과시킨다.
- `ApplyStatusToTargetBlock` 효과는 트리거 발생 블럭에 상태 이상을 적용한다.

## 경제/상점/결과

- 통화는 `PlayerInstance`에 저장되며, 스테이지 종료 시 기본 수입 + 이자 수입을 지급한다.
- 상점은 아이템/업그레이드 가중치 롤링으로 구성되며 리롤 비용이 증가한다.
- 결과 패널은 아이템별 누적 피해 기록을 표시한다.

## 데이터 로딩

- `StaticDataManager`가 `Assets/StreamingAssets/Data/*.json`을 읽어 각 Repository를 초기화한다.
- StreamingAssets는 `SaCache`가 부팅 시 `sa_manifest.json` 기반으로 persistent에 동기화한다.

## 저장/로드

- `SaveManager`가 스테이지 시작 시 저장 데이터를 작성한다.
- `MainMenuScene`의 이어하기는 `GameManager`가 저장 데이터를 적용한 뒤 해당 스테이지에서 재개한다.
- 게임 오버/클리어 시 저장 데이터는 삭제된다.

## 입력/조작

- Input System 기반. `InputManager`가 `PlayerInput` 액션을 관리하며 이동 입력은 키보드/가상 조이스틱을 혼합한다.

## 난수

- 난수는 `GameManager.Rng`(System.Random)를 중심으로 사용한다.

## 텍스트 스타일(프로젝트별)

- 기준: `Assets/GlobalTextStyles.asset`
- 숫자 수치: `<style="value">...</style>`
- 치명타 관련: `<style="critical">...</style>`
- 기타 스타일:
  - `<style="misc">...</style>`
  - `<style="freeze">...</style>`
  - `<style="power">...</style>`
  - `<style="money">...</style>`
  - `<style="normal">...</style>`

## 관련 문서

- 공통 규칙: `Docs/GENERAL_RULES.md`
- 레포 지도: `Docs/PROJECT_MAP.md`
- 디자인 규칙: `Docs/DESIGN_RULE.md`
