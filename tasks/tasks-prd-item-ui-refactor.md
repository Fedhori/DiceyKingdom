## Relevant Files

- `Assets/Scripts/UI/ItemView.cs` - 아이콘/배경/하이라이트/드래그 하이라이트를 담당하는 공통 시각 컴포넌트(신규).
- `Assets/Scripts/UI/ItemTooltipTarget.cs` - ItemInstance 기반 툴팁 호버 처리 컴포넌트(신규).
- `Assets/Scripts/Token/ItemSlotController.cs` - 슬롯 상호작용 로직으로 축소 리팩터링 대상.
- `Assets/Scripts/Token/ItemSlotManager.cs` - 슬롯 바인딩/하이라이트 호출 경로 갱신 필요.
- `Assets/Scripts/Shop/ProductView.cs` - ItemView 기반으로 시각 요소 이관 후 컨트롤러로 분리 대상.
- `Assets/Scripts/Shop/ProductController.cs` - 상점 상품 상호작용 전용 컨트롤러(신규).
- `Assets/Scripts/Shop/ShopView.cs` - 상품 프리팹 생성/바인딩 로직 갱신 대상.
- `Assets/Scripts/Shop/ShopManager.cs` - 선택/드래그 흐름이 ProductController 기반으로 이동될 경우 조정 필요.
- `Assets/Scripts/UI/GhostView.cs` - ItemView 포함 구조로 리팩터링 대상.
- `Assets/Scripts/UI/GhostManager.cs` - GhostView의 데이터 세팅 API 변화에 따른 수정 대상.
- `Assets/Scripts/Tooltip/ItemTooltipUtil.cs` - ItemTooltipTarget이 참조하는 툴팁 모델 빌드 유틸.
- `Assets/Prefabs/.../BaseItemView.prefab` - 공통 ItemView 프리팹(에디터 생성).
- `Assets/Prefabs/.../ItemSlot.prefab` - 슬롯용 ItemView Variant(에디터 생성/교체).
- `Assets/Prefabs/.../ItemProduct.prefab` - 상품용 ItemView Variant(에디터 생성/교체).
- `Assets/Prefabs/.../ItemGhost.prefab` - 고스트용 ItemView Variant(에디터 생성/교체).

### Notes

- Unity 프로젝트로 자동화 테스트 경로는 현재 정의되어 있지 않음.

## Tasks

- [ ] 1.0 ItemView/ItemTooltipTarget 공통 컴포넌트 설계 및 추가
  - [x] 1.1 ItemView 스크립트 생성(아이콘/배경/하이라이트/드래그 하이라이트 표시 API 정의)
  - [x] 1.2 ItemView에 Sprite/rarity/선택 상태 입력 시각 적용 로직 구현
  - [ ] 1.3 ItemTooltipTarget 스크립트 생성(ItemInstance 입력, 호버 시 TooltipManager 호출)
  - [ ] 1.4 ItemTooltipTarget이 ItemView와 분리되어 독립적으로 동작하도록 연결 포인트 정리

- [ ] 2.0 ItemSlotController/ProductController/GhostView 로직 분리 리팩터링
  - [ ] 2.1 ItemSlotController의 시각 처리 제거, ItemView/ItemTooltipTarget 사용으로 교체
  - [ ] 2.2 ProductView를 ProductController로 분리하고, ItemView를 통한 시각 처리로 교체
  - [ ] 2.3 GhostView를 ItemView 기반으로 변경(아이콘/배경/하이라이트 사용)
  - [ ] 2.4 기존 하이라이트/선택 로직을 컨트롤러에서 ItemView로 전달하도록 변경

- [ ] 3.0 Shop/Slot/Ghost 연결 코드 갱신 및 데이터 바인딩 정리
  - [ ] 3.1 ShopView가 ProductController 프리팹을 생성하도록 변경
  - [ ] 3.2 ShopManager의 선택/드래그 이벤트가 ProductController와 일관되게 연결되도록 갱신
  - [ ] 3.3 ItemSlotManager가 ItemView 기반 바인딩/하이라이트 갱신을 사용하도록 수정
  - [ ] 3.4 GhostManager의 Show/Update API가 ItemView 입력에 맞게 변경

- [ ] 4.0 프리팹 교체 가이드 정리 및 수동 검증 항목 정리
  - [ ] 4.1 BaseItemView + Variant 프리팹 교체 절차 문서화(에디터 작업 목록)
  - [ ] 4.2 기존 프리팹 참조 경로 업데이트 체크리스트 작성
  - [ ] 4.3 수동 검증 항목(상점 선택/드래그/슬롯 드래그/고스트 표시/툴팁) 정리
