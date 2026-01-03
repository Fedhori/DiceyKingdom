# PRD: 아이템 UI 컴포넌트 분리/통합 리팩터링

## 1. 소개 / 개요
현재 아이템 슬롯(ItemSlotController), 상점 상품(ProductView), 고스트(GhostView)가 각자 UI 로직을 중복으로 갖고 있어 기능 추가 시 3곳을 모두 수정해야 한다. 이를 공통 시각 요소를 담당하는 ItemView 중심으로 분리하고, 슬롯/상품/고스트 전용 로직은 각각의 컨트롤러로 분리해 유지보수성을 높인다.

## 2. 목표
- 아이템 UI의 공통 시각 요소를 ItemView로 단일화한다.
- 슬롯/상품/고스트의 상호작용 로직을 각각의 컨트롤러로 분리한다.
- 아이템 툴팁 처리 로직을 ItemTooltipTarget으로 통일한다.
- 프리팹 구조를 BaseItemView + Variant(ItemSlot/ItemProduct/ItemGhost)로 재정립한다.

## 3. 사용자 스토리
- 디자이너로서 아이템 UI의 배경/아이콘/하이라이트를 한 컴포넌트에서 수정하고 싶다. 그래야 변경이 한 번에 반영된다.
- 개발자로서 슬롯/상점/고스트의 상호작용 코드를 분리해 유지보수 비용을 낮추고 싶다.

## 4. 기능 요구사항
1) ItemView는 아이콘/배경/하이라이트/드래그 하이라이트 등 공통 시각 요소만 담당해야 한다.
2) ItemSlotController는 슬롯 상호작용(클릭/드래그/교환/판매 등) 로직만 담당해야 한다.
3) ProductController는 상점 상품 상호작용(클릭/드래그/선택/판매 상태 등) 로직만 담당해야 한다.
4) GhostView는 드래그 중 표현 로직만 담당하며, 내부적으로 ItemView를 사용한다.
5) ItemTooltipTarget은 ItemInstance 기반으로 툴팁을 띄운다.
6) ProductController는 선택 상태를 ItemView에 전달하여 하이라이트를 제어한다.
7) ItemSlotController는 슬롯 상태를 ItemView에 전달하여 하이라이트를 제어한다.
8) 각 UI는 동일한 ItemView 기반으로 시각 요소를 공유해야 한다.

## 5. 비목표(범위 제외)
- 아이템 데이터 구조/룰 변경
- 상점 구매/판매 로직 변경
- 툴팁 내용/포맷 변경

## 6. 디자인 고려사항
- BaseItemView 프리팹을 생성하고, 이를 Prefab Variant로 확장한 ItemSlot, ItemProduct, ItemGhost 프리팹 3종을 사용한다.
- BaseItemView는 공통 UI 요소(아이콘, 배경, 하이라이트 마스크, 드래그 하이라이트 등)를 포함한다.
- 기존 UI 레이아웃/스타일은 유지한다(기존 시스템과 호환).

## 7. 기술적 고려사항
- ItemTooltipTarget은 ItemInstance를 입력으로 받는 구조를 유지한다.
- ProductController는 상점에서 선택/드래그/구매 관련 이벤트를 처리하며, 선택 상태는 ItemView로 전달한다.
- ItemSlotController는 슬롯 인벤토리 연동 및 드래그/판매 UI 로직을 담당한다.
- GhostView는 ItemView를 포함하되, 상호작용은 하지 않는다.
- 스크립트/프리팹 위치는 기존 위치를 유지한다.

## 8. 성공 지표
- 아이템 UI 관련 변경이 ItemView와 해당 컨트롤러에서만 수정되며, 중복 변경이 사라진다.
- 슬롯/상품/고스트의 시각 요소가 동일하게 동기화된다.
- 기존 상점/슬롯/고스트 기능이 리그레션 없이 동작한다.

## 9. 오픈 질문
- 없음
