## Relevant Files

- `Assets/Scripts/Item/ItemInstance.cs` - 아이템에 다중 강화 리스트를 보관하고 변경 이벤트를 제공해야 함.
- `Assets/Scripts/Upgrade/UpgradeManager.cs` - 다중 강화 적용/제거 로직 및 스탯 누적 처리가 필요.
- `Assets/Scripts/Upgrade/UpgradeReplaceRequest.cs` - 강화 교체 요청 정보를 보관하고 확인/취소 콜백을 전달.
- `Assets/Scripts/Upgrade/UpgradeInventoryManager.cs` - 강화 적용/교체 흐름과 모달 트리거를 다중 강화 기준으로 수정.
- `Assets/Scripts/Shop/ShopManager.cs` - 상점 강화 적용/교체 흐름을 다중 강화 기준으로 수정.
- `Assets/Scripts/Token/ItemSlotManager.cs` - 아이템 판매 시 모든 강화를 강화 가방으로 이동 처리.
- `Assets/Scripts/Token/ItemSlotController.cs` - 아이템 슬롯에 강화 아이콘 리스트 표시 및 패널 열기 트리거 처리.
- `Assets/Scripts/UI/ItemTooltipTarget.cs` - 강화 보기 버튼이 패널 오픈으로 전환되며 토글 로직 조정 필요.
- `Assets/Scripts/Tooltip/TooltipManager.cs` - 기존 강화 토글 동작 제거/대체 및 패널 연동.
- `Assets/Scripts/Tooltip/TooltipView.cs` - 다중 툴팁 프리팹 생성용 재사용 포맷 점검.
- `Assets/Scripts/Tooltip/UpgradeTooltipUtil.cs` - 강화 툴팁 모델 생성 로직 재사용.
- `Assets/Scripts/Data/LocalizationUtil.cs` - 강화 이름 로컬라이즈 사용 지점.
- `Assets/Scripts/UI/UpgradePanelView.cs` - 강화 전용 오버레이 패널과 다중 툴팁 렌더링 관리.
- `Assets/Scripts/UI/UpgradePanelSlot.cs` - 강화 패널 내 툴팁 인스턴스와 버튼 이벤트 연결.
- `Assets/Localization/modal Shared Data.asset` - 교체 모달 신규 키 추가.
- `Assets/Localization/modal_ko-KR.asset` - 교체 모달 문구 추가.
- `Assets/Localization/tooltip Shared Data.asset` - “교체하기” 버튼 키 추가.
- `Assets/Localization/tooltip_ko-KR.asset` - “교체하기” 문구 추가.
- `Assets/Scripts/UI` (신규) - 강화 전용 패널/툴팁 컨테이너 UI 스크립트 추가.
- `Assets/Scripts/Data/GameConfig.cs` - 최대 강화 슬롯 수 상수 추가.

### Notes

- 테스트 코드는 현재 구조상 별도 없음. 필요 시 유닛 테스트 파일을 동일 폴더에 추가.

## Tasks

- [x] 1.0 다중 강화 데이터 모델 도입 및 최대 슬롯 상수 추가
- [x] 1.1 `GameConfig`에 최대 강화 슬롯 수 상수 추가 및 접근자 정리
- [x] 1.2 `ItemInstance`에 강화 리스트 보관 및 변경 알림(이벤트/콜백) 추가
- [x] 1.3 강화 리스트를 기준으로 스탯 합산/복제 로직이 정상 작동하도록 업데이트
- [x] 2.0 강화 적용/제거/교체 로직을 다중 강화 기준으로 리팩터링
  - [x] 2.1 `UpgradeManager`의 적용/해제 API를 리스트 기반으로 변경하고 중복 허용 처리
  - [x] 2.2 슬롯이 가득 찼을 때 교체 모드 플래그/대기 강화 정보를 전달할 수 있도록 흐름 정리
  - [x] 2.3 상점/강화 가방에서 강화 적용 시 다중 슬롯 로직을 공통 경로로 합치기
- [x] 3.0 아이템 슬롯에 강화 아이콘 리스트 표시 UI 추가
  - [x] 3.1 `ItemSlotController`에 강화 아이콘 컨테이너/프리팹 필드 추가
  - [x] 3.2 아이템 변경/강화 변경 시 아이콘 리스트를 갱신하는 메서드 추가
  - [x] 3.3 희귀도 배경색/아이콘 스프라이트 매핑 적용
- [ ] 4.0 강화 전용 오버레이 패널 및 다중 툴팁 렌더링 구조 추가
  - [ ] 4.1 강화 패널 오픈/클로즈 및 툴팁 인스턴스 생성을 담당하는 뷰 스크립트 추가
  - [ ] 4.2 강화 보기 버튼이 패널을 열도록 `ItemTooltipTarget`/`TooltipManager` 흐름 수정
  - [ ] 4.3 교체 모드일 때 기존 강화 툴팁 버튼을 “교체하기”로 변경하고 교체 대상 선택 처리
  - [ ] 4.4 신규 강화 툴팁을 패널에 함께 노출하는 비교 표시 로직 추가
- [ ] 5.0 교체 모달/버튼 로컬라이즈 키 추가 및 문구 반영
  - [ ] 5.1 `tooltip.upgrade.replace.label` 키 추가 및 버튼 라벨 연결
  - [ ] 5.2 `modal.upgradeReplace.title/body` 키 추가 및 확인 모달 호출 연결
- [ ] 6.0 아이템 판매 시 모든 강화가 강화 가방으로 이동되도록 수정
  - [ ] 6.1 아이템 판매 시 강화 리스트를 회수해 강화 가방에 추가하는 로직 수정
  - [ ] 6.2 판매 후 아이템의 강화 리스트를 비우고 UI 아이콘을 갱신
