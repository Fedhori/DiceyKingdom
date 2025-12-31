# PRD: Unified Item System (Token -> Item 통합)

## 1. 소개 / 개요
토큰과 아이템을 분리해 관리하던 현재 구조를 단일 Item 시스템으로 통합한다. 토큰 전용 JSON/DTO/Instance/Controller 파이프라인을 제거하고, 모든 보유/구매/판매/이동/렌더링을 ItemInventory 기반으로 처리한다. 목표는 중복 파이프라인 제거, 데이터/런타임 흐름 단순화, UI/상점/전투 로직의 일관성 확보이다.

## 2. 목표
- 토큰 데이터를 Items.json으로 완전 흡수하고 토큰 전용 파이프라인을 제거한다.
- ItemManager가 단일 인벤토리(ItemInventory)를 소유하는 구조로 전환한다.
- 상점 상품을 itemId 기반 단일 모델로 통일한다.
- 기존 토큰 UI를 유지하되 데이터 소스를 ItemInventory로 교체한다.
- isObject(boolean) 필드를 추가해 런타임 동작 경로(Attach 필요/불필요)를 결정한다.

## 3. 사용자 스토리
- 플레이어로서, 상점에서 아이템을 드래그하거나 선택 후 슬롯을 클릭해 구매할 수 있다.
- 플레이어로서, 하단 슬롯 UI에서 아이템 위치를 드래그로 교체할 수 있다.
- 플레이어로서, 상점 단계에서 아이템을 드래그하여 판매할 수 있다.
- 디자이너로서, Items.json에 아이템을 정의하면 상점/인벤토리/런타임에서 동일 itemId 흐름으로 동작한다.

## 4. 기능 요구사항
1. 시스템은 토큰 전용 JSON/DTO/Instance/Controller 파이프라인을 제거해야 한다.
2. 시스템은 Items.json만 로드해 아이템 정의를 구성해야 한다.
3. 시스템은 ItemDto에 `isObject: boolean` 필드를 추가해야 하며, `true`면 Attach가 필요함을 의미해야 한다.
4. 시스템은 ItemManager 내부에 ItemInventory를 두고, 인벤토리 데이터를 단일 소스로 사용해야 한다.
5. ItemInventory는 슬롯 수를 `GameConfig.TokenSlotCount`로 고정해야 한다.
6. ItemInventory는 Add/Remove/Move/Swap/SetSlot 등 단일 커맨드 API로 조작되어야 한다.
7. 하단 아이템 슬롯 UI는 ItemInventory 데이터만 참조해 렌더링해야 한다.
8. 상점은 itemId 기반 단일 상품 모델만 사용해야 하며, 상품 풀은 Items.json 전체를 균등 확률로 구성해야 한다.
9. 구매는 (1) 드래그 드롭, (2) 상품 선택 후 빈 슬롯 클릭 두 방식 모두 지원해야 한다.
10. 구매가 성공하면 ItemInventory의 지정 슬롯에 아이템이 추가되어야 한다.
11. 판매는 상점 단계에서만 가능해야 하며, 판매가 성공하면 해당 슬롯 아이템이 제거되고 가격의 50%를 소수점 버림으로 지급해야 한다.
12. 아이템 이동/스왑은 상점 단계에서만 가능해야 한다.
13. `isObject == true`인 아이템은 플레이어 오브젝트에 프리팹을 Attach하여 런타임 동작을 수행해야 한다.
14. `isObject == false`인 아이템은 프리팹 없이 ItemManager(또는 ItemRuntime)에서 이벤트/틱 기반으로 실행될 수 있어야 한다.
15. Object/NonObject 모두 동일한 `trigger -> condition -> effects` 실행 규칙을 따라야 하며, 스테이지 시작/틱 트리거를 지원해야 한다.
16. 기존 토큰 UI의 툴팁/하이라이트/드래그 판매 UX는 아이템 슬롯 공통 규칙으로 동작해야 한다.

## 5. 비목표 (범위 제외)
- 토큰/아이템 병행 지원 또는 마이그레이션/호환성 레이어 제공
- 신규 UI/UX 전면 개편 (기존 토큰 UI 유지)
- 기존 저장 데이터 변환 지원
- isObject 외 별도 구분 필드 추가

## 6. 디자인 고려사항 (옵션)
- 기존 토큰 슬롯 UI 레이아웃/동작을 유지한다.
- 드래그 시 하이라이트, 판매 오버레이, 툴팁 표시 등 기존 상호작용을 유지한다.

## 7. 기술 고려사항 (옵션)
- Unity 6.2, Input System 사용 규칙 준수.
- 데이터는 `Assets/StreamingAssets/Data/Items.json` 단일 경로로 관리.
- `Token*` 클래스 및 Token 관련 데이터 로더/리포지토리/매니저 제거 필요.
- `ShopManager`는 itemId 기반으로만 동작하도록 통합해야 한다.
- 씬/프리팹/Inspector 수정이 필요한 경우 별도 안내 필요.

## 8. 성공 지표
- 프로젝트가 Token 관련 코드 없이 컴파일/실행된다.
- 상점에서 아이템 구매/판매/이동이 정상 동작한다.
- 하단 슬롯 UI가 ItemInventory 기준으로 올바르게 렌더링된다.
- 스테이지 진행(Play/Reward/Shop) 흐름에 영향 없이 아이템 동작이 유지된다.

## 9. 오픈 질문
- Object 아이템의 프리팹 참조는 Items.json에는 키만 두고, Unity 인스펙터의 매핑 테이블(예: ItemPrefabRegistry)에서 key->Prefab 연결로 처리한다.
