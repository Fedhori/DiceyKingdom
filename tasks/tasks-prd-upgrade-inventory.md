## Relevant Files

- `Assets/Scripts/Upgrade/UpgradeInventoryManager.cs` - 강화 목록/선택/적용 흐름 관리(신규).
- `Assets/Scripts/UI/UpgradeInventoryView.cs` - ScrollView/빈 상태/열기-닫기 UI 제어(신규).
- `Assets/Scripts/UI/UpgradeInventorySlotController.cs` - 슬롯 표시/선택/드래그/툴팁 바인딩(신규).
- `Assets/Scripts/Token/ItemSlotManager.cs` - 아이템 판매 시 강화 회수, 적용 가능한 슬롯 하이라이트 연동.
- `Assets/Scripts/Token/ItemSlotController.cs` - 강화 인벤토리 선택 상태에서 클릭 적용 흐름.
- `Assets/Scripts/Upgrade/UpgradeManager.cs` - 강화 적용/교체 확인 로직 공통화 또는 보조 메서드 추가.
- `Assets/Localization/upgrade Shared Data.asset` - 강화 인벤토리 빈 상태 문구 키 추가.
- `Assets/Localization/upgrade_ko-KR.asset` - 빈 상태 문구 한글 추가.

### Notes

- UI 배치/프리팹 연결은 에디터에서 진행(코드에서는 열기/닫기/바인딩만 제공).
- 자동화 테스트 대신 상점 단계에서 선택/드래그 적용 흐름을 플레이 모드로 확인.

## Tasks

- [x] 1.0 강화 인벤토리 데이터/매니저 도입
  - [x] 1.1 강화 보유 목록(무제한)과 변경 이벤트를 매니저에 통합
  - [x] 1.2 강화 선택/선택해제 상태를 관리하는 `UpgradeInventoryManager` 생성
  - [x] 1.3 아이템 판매 시 강화 회수용 API 설계(아이템에서 강화 분리 → 인벤토리로 이동)
  - [x] 1.4 판매 시 강화 회수 로그 추가(검증용)
- [x] 2.0 강화 인벤토리 UI 스캐폴딩(ScrollView/슬롯/빈 상태)
  - [x] 2.1 ScrollView 기반 컨테이너와 슬롯 프리팹 바인딩용 View 작성
  - [x] 2.2 강화 슬롯 아이콘/희귀도 배경/툴팁 바인딩 로직 추가
  - [x] 2.3 빈 상태 텍스트 로컬라이즈 키 추가 및 표시 제어
- [x] 3.0 강화 선택/적용/드래그 흐름 구현
  - [x] 3.1 슬롯 선택 시 적용 가능 아이템 슬롯 하이라이트 연동
  - [x] 3.2 슬롯 클릭으로 적용 시 ConfirmModal 및 교체 로직 연계
  - [x] 3.3 강화 드래그 고스트/드롭 적용 연동(기존 고스트 재사용)
  - [x] 3.4 같은 슬롯 재클릭/빈 공간 클릭 시 선택 해제 처리
- [x] 4.0 상점 단계 연동 및 선택 해제/하이라이트 정리
  - [x] 4.1 상점 단계에서만 열기/닫기 가능한 함수 제공
  - [x] 4.2 상점 닫힘/선택 변경 시 강화 선택/하이라이트 정리
  - [x] 4.3 상점 구매 흐름은 기존 즉시 적용 유지(인벤토리 보관 없음)
