## Introduction/Overview
- 상점의 핀 상품 선택 시 기본 핀들을 하이라이트하고, 플레이어가 해당 하이라이트 핀을 직접 선택해 구매/배치까지 완료하도록 흐름을 개선한다. 자동 배치 대신 사용자 선택을 반영해 구매 경험을 명확히 한다.

## Goals
- 핀 상품 선택 시 상점 아이템 배경을 `Colors.HighlightColor`로 표시하고, 선택 해제 시 기본색(`FFFFFF`)으로 복원한다.
- 선택된 핀 상품이 있을 때만 기본(pin.basic) 핀이 `WorldHighlight.SetHighlight`로 하이라이트되고, 선택 해제 시 즉시 해제된다.
- 플레이어가 하이라이트된 기본 핀 위치 중 하나를 클릭하면 해당 상품을 구매하고 그 위치에 배치한다.
- 상점 구독 지점은 `ShopItemView`, 핀 측 구독 지점은 `PinController`에서 처리한다.

## User Stories
- 플레이어는 상점에서 핀 상품을 선택하면 기본 핀들이 하이라이트되어 배치 가능 위치를 즉시 파악할 수 있다.
- 플레이어는 하이라이트된 핀을 클릭하면 별도 확인 없이 바로 구매되며, 선택한 위치에 즉시 배치된다.
- 플레이어가 상품 선택을 해제하면 하이라이트가 사라지고 상점 아이템 배경도 기본색으로 돌아가 혼동을 줄인다.

## Functional Requirements
1. `ShopItemView`에서 핀 상품 선택 상태를 관리하고, 선택 시 배경을 `Colors.HighlightColor`, 선택 해제 시 `FFFFFF`로 설정한다.
2. 선택 상태는 Setter에서 이벤트를 Invoke하여 알린다. 상점 측(`ShopItemView`)은 이 이벤트를 구독해 배경색을 변경하고, 핀 측(`PinController`)은 이를 구독해 하이라이트 On/Off를 처리한다.
3. 핀 상품이 선택된 경우에만 기본 핀(`pin.basic`)에 대해 `WorldHighlight.SetHighlight`를 호출해 하이라이트를 켠다. 선택 해제 시 즉시 하이라이트를 끈다.
4. 플레이어가 하이라이트된 기본 핀을 클릭하면 해당 상품을 구매하고 그 위치에 배치한다. 구매/배치는 기존 자동 배치 대신 `PinManager.TryReplace`를 사용하여 클릭된 위치로 수행한다.
5. 상점은 메인 스토어(현재 핀 리스트)에서만 이 동작을 제공한다. 다른 상점/상황에는 적용하지 않는다.
6. 구매/배치 흐름에 별도 확인 모달이나 추가 연출은 없다. 잔액/실패 처리 시 기존 상점 피드백 방식을 그대로 따른다.

## Non-Goals
- 핀 배치 상태를 저장하거나 재접속 시 복원하지 않는다.
- 하이라이트 외 추가 시각효과(아웃라인, 애니메이션 등)를 도입하지 않는다.
- 상점 외 다른 UI/씬에 적용하지 않는다.

## Design Considerations
- 배경색: 기본 `FFFFFF`, 선택 시 `Colors.HighlightColor`만 적용. 추가 연출 없음.
- 하이라이트: `WorldHighlight.SetHighlight`를 기본 핀에만 적용. 선택 해제 시 즉시 Off.

## Technical Considerations
- 이벤트 기반 흐름: 선택 Setter에서 이벤트 Invoke → `ShopItemView`와 `PinController`가 각각 구독.
- 배치 로직: 기존 자동 배치(GetBasicPinSlot) 대신 클릭된 핀 위치에 `PinManager.TryReplace` 호출로 배치.
- Unity Inspector: 필요한 경우 컴포넌트/참조는 코드가 아닌 인스펙터에서 연결(기본 규칙 준수).

## Success Metrics
- 핀 상품 선택 시 기본 핀 하이라이트가 즉시 표시되고, 선택 해제 시 즉시 사라진다.
- 플레이어가 하이라이트된 핀을 클릭했을 때 상품이 구매/배치까지 한 번에 완료된다.
- 기본 핀 외 위치에서는 구매/배치가 발생하지 않는다.

## Open Questions
- 하이라이트 이벤트 페이로드(예: 선택된 상품 ID, null 시 해제 여부) 구조를 명시할 필요가 있는지 여부. 필요한 경우 구체 필드를 정의해야 함.
