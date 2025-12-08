## Relevant Files

- `Assets/Scripts/Shop/ShopManager.cs` - 상점 열기/아이템 구매 흐름 관리; 현재 기본핀 빈 슬롯을 찾아 자동 배치하는 로직이 있음.
- `Assets/Scripts/Shop/ShopView.cs` - 상점 UI 루트; ShopItemView 인스턴스 생성 및 클릭 콜백 연결.
- `Assets/Scripts/Shop/ShopItemView.cs` - 단일 상품 카드 UI; 배경 이미지/버튼 클릭 처리 지점.
- `Assets/Scripts/Shop/ShopItemData.cs` - 상점 아이템 슬롯 데이터 구조; 가격/판매 상태 포함.
- `Assets/Scripts/Pin/PinManager.cs` - 핀 그리드 관리, 기본(pin.basic) 슬롯 검색, `TryReplace`로 교체 수행.
- `Assets/Scripts/Pin/PinController.cs` - 개별 핀 제어 및 이벤트 구독 후보; 클릭/하이라이트 연동 추가 예정.
- `Assets/Scripts/Pin/SellClickTarget.cs` - 핀을 클릭했을 때 처리하는 IPointerClickHandler; 구매용 클릭 확장 시 참고.
- `Assets/Scripts/WorldHighlight.cs` - 하이라이트 색/표시 토글 처리; 기본 핀 하이라이트에 사용.
- `Assets/Scripts/Data/Colors.cs` - `HighlightColor` 정의; ShopItemView 배경 색 설정에 사용.

### Notes

- 현재 테스트 파일 없음. 수동 검증 위주로 계획.

## Tasks

- [x] 1.0 상점 핀 상품 선택 상태 관리 및 이벤트 발행 구조 추가
  - [x] 1.1 선택 상태를 보관할 필드/프로퍼티 정의(선택 PinInstance 또는 pinId, null 시 해제) 및 이벤트(Action 등) 발행 메커니즘 추가
  - [x] 1.2 ShopItemView 클릭 시 구매 직행 대신 “선택 Setter → 이벤트 Invoke”로 흐름 전환, 다른 아이템 선택 시 이전 선택 해제 처리
  - [x] 1.3 상점 열림/닫힘 시 선택 상태 초기화(이벤트로 null 전달)하여 배경/하이라이트가 남지 않도록 정리
  - [x] 1.4 메인 스토어 한정 동작 유지(다른 상점 컨텍스트와 혼동되지 않도록 가드)
- [ ] 2.0 선택된 핀 상품 UI 배경/하이라이트 표시 연동 (`Colors.HighlightColor`)
  - [x] 2.1 ShopItemView가 선택 이벤트를 구독하여 선택된 카드만 배경을 `Colors.HighlightColor`, 나머지는 `FFFFFF`로 유지
  - [ ] 2.2 선택 해제(null 이벤트) 시 모든 ShopItemView 배경을 기본색으로 복원
- [ ] 3.0 기본(pin.basic) 핀 하이라이트 토글 및 클릭 시 구매/배치 처리 (`PinManager.TryReplace`)
  - [ ] 3.1 PinController(또는 별도 핀 하이라이트 핸들러)가 선택 이벤트를 구독해 기본 핀만 `WorldHighlight.SetHighlight(true/false)` 토글
  - [ ] 3.2 핀 클릭 입력 처리 추가: 하이라이트된 기본 핀 클릭 시 현재 선택된 pinId를 가져와 `CurrencyManager.TrySpend` 후 `PinManager.TryReplace(pinId, row, col)`로 교체
  - [ ] 3.3 구매 성공 시 선택 상태 해제 및 하이라이트/배경 초기화, 실패 시 통화/Replace 실패 대응(환불/로그) 후 상태 일관성 유지
- [ ] 4.0 통합 동작 및 회귀 수동 점검 (메인 스토어 한정)
  - [ ] 4.1 시나리오: 상점 열림 → 상품 선택 시 기본 핀 하이라이트 ON/배경 하이라이트 ON → 선택 해제/다른 상품 선택 시 즉시 OFF 전환 확인
  - [ ] 4.2 시나리오: 하이라이트된 기본 핀 클릭 시 구매/배치가 클릭 위치에 즉시 적용되고 선택/하이라이트가 초기화되는지 확인
  - [ ] 4.3 시나리오: 통화 부족 또는 TryReplace 실패 시 구매 불가 처리 후 선택 상태/하이라이트 일관성 확인
  - [ ] 4.4 시나리오: 상점 닫기/씬 전환 시 선택 상태와 모든 하이라이트/배경이 초기화되는지 확인
