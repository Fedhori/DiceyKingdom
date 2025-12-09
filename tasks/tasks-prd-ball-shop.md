## Relevant Files

- `Assets/Scripts/Shop/ShopManager.cs` - 상점 열기/재롤/상품 목록 관리; Ball 슬롯 추가/구매 흐름 확장 대상.
- `Assets/Scripts/Shop/ShopView.cs` - 상점 UI 루트; Ball 전용 컨테이너/프리팹 연결 및 렌더링 확장 지점.
- `Assets/Scripts/Shop/PinItemData.cs` - 상점 아이템 데이터 구조; Ball도 유사 구조 필요 시 재사용/확장 검토.
- `Assets/Scripts/Shop/BallItemData.cs` - Ball 상품 슬롯 데이터 구조.
- `Assets/Scripts/Shop/PinItemView.cs` - Pin용 뷰; BallItemView 구조 참고용.
- `Assets/Scripts/Ball/BallRepository.cs` (또는 Ball DTO/데이터 소스) - Ball 목록/price/isNotSell 필터 출처.
- `Assets/Scripts/Ball/BallUiTooltipTarget.cs` - Ball 툴팁 타겟 연결.
- `Assets/Scripts/Ball/BallDeck.cs` - `TryReplace`로 덱 교체 수행.
- `Assets/Scripts/Ball/BallFactory.cs`/`BallInstance.cs` - 필요 시 스프라이트/프리뷰 생성에 참고.
- `Assets/Scripts/Shop/BallItemView.cs` - Ball 상품 카드 UI.

### Notes

- 인스펙터에서 Ball 컨테이너/프리팹 연결은 사용자가 수행.
- 기존 테스트 부재, 수동 검증 위주.

## Tasks

- [ ] 1.0 Ball 상점 슬롯 구성/데이터 로딩 추가
  - [x] 1.1 `isNotSell == false` Ball 풀 구성 및 슬롯 수(기본 2, 변수화) 랜덤 노출 로직 추가
  - [x] 1.2 재롤/회차마다 Ball 상품 재생성 및 중복 방지 처리
- [ ] 2.0 Ball 전용 뷰/툴팁 연결
  - [x] 2.1 `BallItemView` 신설(아이콘/가격/구매 가능 표시; 배경/하이라이트 없음)
  - [ ] 2.2 `BallUiTooltipTarget` 바인딩 및 ShopView에서 Ball 컨테이너/프리팹 직렬화 필드 처리
- [ ] 3.0 Ball 구매 흐름 연동
  - [ ] 3.1 Ball 카드 클릭 시 즉시 구매 시도, 가격 검증 및 `BallDeck.TryReplace(ballId)` 호출
  - [ ] 3.2 구매 성공 시 통화 차감/판매 처리, 실패(`TryReplace == false`) 시 무동작(리펀드/메시지 없음)
- [ ] 4.0 UI 통합 및 수동 검증
  - [ ] 4.1 Pin/Ball 혼합 레이아웃에서 구매 가능/불가 색상/비활성화가 Pin 규칙과 일관되는지 확인
  - [ ] 4.2 재롤/회차 전환 시 Ball 슬롯이 2종 중복 없이 갱신되는지 확인
  - [ ] 4.3 TryReplace 실패 시 조용히 종료되고 UI 상태가 유지되는지 확인
