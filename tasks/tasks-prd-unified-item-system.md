## Relevant Files

- `Assets/StreamingAssets/Data/Items.json` - 토큰 정의를 흡수하고 `isObject`, 규칙(트리거/조건/효과), 프리팹 키 등을 추가하는 데이터 소스.
- `Assets/StreamingAssets/Data/Tokens.json` - 완전 제거 대상 데이터.
- `Assets/Scripts/Item/ItemDto.cs` - `isObject` 및 규칙 필드, 프리팹 키 필드 추가.
- `Assets/Scripts/Item/ItemInstance.cs` - 규칙 실행 및 런타임 상태 보관 로직 확장.
- `Assets/Scripts/Item/ItemRepository.cs` - Items.json 파싱/검증 확장.
- `Assets/Scripts/Item/ItemManager.cs` - ItemInventory 소유 및 단일 소스 오브 트루스 전환.
- `Assets/Scripts/Item/ItemController.cs` - `isObject == true` 아이템 Attach 동작 유지/확장.
- `Assets/Scripts/Item/ItemInventory.cs` - (신규) 슬롯/이동/스왑/추가/삭제 커맨드 API 및 이벤트.
- `Assets/Scripts/Item/ItemEffectManager.cs` - (신규 또는 기존 이동) 아이템 효과 적용 파이프라인.
- `Assets/Scripts/Item/ItemPrefabRegistry.cs` - (신규) item key -> prefab 매핑 테이블(Inspector 연결).
- `Assets/Scripts/Shop/ShopManager.cs` - itemId 기반 상품/구매/판매/드래그 처리로 통합.
- `Assets/Scripts/Shop/ShopItemFactory.cs` - 아이템 상품 생성 로직으로 통합.
- `Assets/Scripts/Shop/ShopItemView.cs` - 아이템 툴팁/아이콘/가격 렌더링으로 전환.
- `Assets/Scripts/Shop/ShopView.cs` - 토큰 분기 제거 및 단일 아이템 UI 흐름 반영.
- `Assets/Scripts/Shop/ShopItemType.cs` - 토큰 타입 제거 또는 단일 타입 정리.
- `Assets/Scripts/Shop/TokenShopItem.cs` - 제거 후 ItemShopItem 등으로 대체.
- `Assets/Scripts/Tooltip/TokenTooltipUtil.cs` - 아이템 기반 툴팁 유틸로 대체/변경.
- `Assets/Scripts/Tooltip/TooltipModel.cs` - TooltipKind Token 제거/Item 추가.
- `Assets/Scripts/Stage/StageManager.cs` - 스테이지 시작 트리거를 아이템 규칙 실행으로 교체.
- `Assets/Scripts/Data/StaticDataManager.cs` - Tokens.json 로딩 제거 및 Items.json 단일화.
- `Assets/Scripts/Dev/DevCommandManager.cs` - addtoken 등 토큰 명령 제거/아이템 명령으로 교체.
- `Assets/Scripts/Data/SpriteCache.cs` - 토큰 스프라이트 참조를 아이템 스프라이트로 변경.
- `Assets/Scripts/Data/LocalizationUtil.cs` - 토큰 이름 조회를 아이템 이름 조회로 변경.
- `Assets/Scripts/UI/GhostKind.cs` - Token 고스트를 Item 고스트로 변경.
- `Assets/Scripts/UI/SellOverlayController.cs` - 판매 오버레이 동작을 아이템 슬롯 기준으로 유지.
- `Assets/Scripts/Token/` - Token 관련 DTO/Instance/Manager/Controller 제거 대상.
- `Assets/Scripts/Bullet/` - Projectile 네이밍/행동 옵션으로 리팩터링될 가능성이 있는 기존 총알 코드.
- `Assets/Prefabs/` - 미니건/프로젝타일(볼/총알) 프리팹 추가 및 매핑 대상.

### Notes

- 자동화 테스트 인프라가 확인되지 않아, 변경 후 수동 검증 플로우를 문서화한다.
- 씬/프리팹/Inspector 변경이 필요하므로 작업 시 반드시 사용자에게 공유한다.

## Tasks

- [ ] 1.0 Items 데이터 스키마 통합 및 확장
  - [x] 1.1 `Tokens.json`의 토큰 정의를 `Items.json`으로 병합하고 ID 중복/가격 필드를 정리한다.
  - [x] 1.2 `ItemDto`에 `isObject` 및 규칙(트리거/조건/효과) 필드를 추가한다.
  - [ ] 1.3 Object 아이템용 프리팹 키 필드(예: `prefabKey`)를 `ItemDto`에 추가한다.
  - [ ] 1.4 `ItemRepository` 파싱/검증 로직을 확장해 규칙 구조를 검증한다.

- [ ] 2.0 ItemInventory 도입 및 ItemManager 단일 소스 전환
  - [ ] 2.1 `ItemInventory` 클래스를 생성하고 슬롯 수를 `GameConfig.TokenSlotCount`로 고정한다.
  - [ ] 2.2 `Add/Remove/Move/Swap/SetSlot` 커맨드 API와 변경 이벤트를 정의한다.
  - [ ] 2.3 `ItemManager`가 `ItemInventory`를 소유하고 기존 `PlayerInstance.ItemIds` 초기화 흐름을 이관한다.
  - [ ] 2.4 `ItemPrefabRegistry`를 추가하고 item key -> prefab 매핑을 Inspector에서 관리하도록 한다.
  - [ ] 2.5 `ItemManager`가 인벤토리 변경 이벤트를 받아 Object 아이템을 Attach/Detach한다.

- [ ] 3.0 아이템 규칙 실행 파이프라인 구축
  - [ ] 3.1 기존 Token 규칙 구조(Trigger/Condition/Effect)를 Item 규칙 구조로 이관한다.
  - [ ] 3.2 `ItemInstance`에서 규칙 평가 로직을 구현하고 Object/NonObject 모두 동일 규칙을 따른다.
  - [ ] 3.3 `ItemEffectManager`를 통해 플레이어 스탯/통화 효과를 적용한다.
  - [ ] 3.4 스테이지 시작/틱 트리거를 `ItemManager`에서 브로드캐스트한다.

- [ ] 4.0 상점 로직을 itemId 기반 단일 흐름으로 통합
  - [ ] 4.1 `ShopItemType`의 Token 분기 제거 및 단일 아이템 타입으로 정리한다.
  - [ ] 4.2 `TokenShopItem`을 `ItemShopItem`으로 대체하고 아이템 프리뷰 인스턴스를 제공한다.
  - [ ] 4.3 `ShopManager`에서 Token 관련 풀/보유 체크 로직을 제거하고 Items.json 전체를 균등 풀로 사용한다.
  - [ ] 4.4 구매/판매/드래그 흐름을 `ItemInventory` 커맨드 호출로 통일한다.
  - [ ] 4.5 판매 가격은 `가격 * 0.5` 소수점 버림 규칙으로 계산한다.

- [ ] 5.0 하단 슬롯 UI/툴팁/고스트를 ItemInventory 기준으로 전환
  - [ ] 5.1 Token 슬롯 UI를 Item 슬롯 UI로 교체하고 인벤토리 데이터를 렌더링한다.
  - [ ] 5.2 슬롯 드래그 이동/스왑, 상점 드래그 구매, 드래그 판매를 공통 규칙으로 처리한다.
  - [ ] 5.3 `TokenTooltipUtil`을 아이템 기반 툴팁 유틸로 대체한다.
  - [ ] 5.4 `SpriteCache`와 `LocalizationUtil`을 아이템 기반 조회로 변경한다.
  - [ ] 5.5 `GhostKind`의 Token 고스트를 Item 고스트로 변경한다.

- [ ] 6.0 토큰 시스템 제거 및 연결 지점 정리
  - [ ] 6.1 `Assets/Scripts/Token/*` 및 Tokens.json 로딩 경로를 제거한다.
  - [ ] 6.2 `StageManager`의 토큰 트리거 호출을 아이템 트리거 호출로 변경한다.
  - [ ] 6.3 `DevCommandManager`의 addtoken 등 토큰 전용 명령을 제거/대체한다.
  - [ ] 6.4 관련 Prefab/Scene/Inspector에서 TokenManager/TokenController 참조를 Item 시스템으로 교체한다.
  - [ ] 6.5 수동 검증: 상점 구매/판매/이동, 스테이지 시작/틱 트리거, 게임 플레이 루프 정상 동작 확인.

- [ ] 7.0 샘플 아이템 3종(볼/미니건/교과서) 구현 및 검증
  - [ ] 7.1 `Items.json`에 볼/미니건/교과서 정의를 추가한다. (`isObject`, `projectileKey`, 규칙 포함)
  - [ ] 7.2 `projectile.ball` 프리팹을 추가하고 Bounce 동작(반사)을 설정한다.
  - [ ] 7.3 미니건 프리팹을 추가하고 `ItemPrefabRegistry`에 매핑한다.
  - [ ] 7.4 교과서 아이템의 OnStageStart 영구 피해 +10 규칙을 적용한다.
  - [ ] 7.5 수동 검증: 스테이지 시작 시 볼 5개 생성/반사, 미니건 초당 2발 발사, 교과서 피해 +10 적용 확인.
