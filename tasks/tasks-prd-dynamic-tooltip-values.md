## Relevant Files

- `Assets/Scripts/Tooltip/ItemTooltipUtil.cs` - 아이템 툴팁 본문/효과 라인 생성 로직이 있음.
- `Assets/Scripts/Tooltip/UpgradeTooltipUtil.cs` - 강화 툴팁 본문/효과 라인 생성 로직이 있음.
- `Assets/Scripts/Tooltip/TooltipDynamicValueUtil.cs` - 현재값 결합을 위한 공통 유틸(라인 후처리).
- `Assets/Scripts/Tooltip/TooltipManager.cs` - 툴팁 표시/갱신 타이밍 제어 가능 지점.
- `Assets/Scripts/UI/ItemTooltipTarget.cs` - TooltipModel 빌드/핀 로직이 있어 갱신 훅 연결 지점.
- `Assets/Scripts/Item/ItemInstance.cs` - StatSet 보유 및 아이템 스탯 접근 지점.
- `Assets/Scripts/Stat/GameStat.cs` - StatSet/StatSlot 구조(변경 이벤트 추가 필요 시 수정 대상).
- `Assets/Localization/item_ko-KR.asset` - item.angel 효과 문구 수정 필요.
- `Assets/Localization/upgrade_ko-KR.asset` - upgrade.whetstone 효과 문구 수정 필요.
- `Assets/Localization/tooltip_ko-KR.asset` - “현재” 라벨 등 추가 키가 필요할 경우.

### Notes

- 테스트 코드는 현재 구조상 별도 프레임워크가 없어 추가하지 않는다.

## Tasks

- [x] 1.0 가변 효과 현재값 표시 구조 설계 및 매핑 정의
  - [x] 1.1 가변 효과 라인별 현재값 매핑 방식 확정(효과 라인 -> statId)
  - [x] 1.2 현재값 포맷 키 설계(currentValue0/currentPercentValue0) 및 결합 규칙 정의
  - [x] 1.3 실시간 갱신을 위한 이벤트 훅 후보 정리(StatSet/ItemInstance/TooltipManager)
- [x] 2.0 툴팁 현재값 결합/갱신 흐름 추가
  - [x] 2.1 TooltipModel 빌드 시 현재값 추가 결합 로직 구현
  - [x] 2.2 statId -> 현재값 읽기/포맷 공통 유틸 추가
  - [x] 2.3 툴팁 열림 상태에서 stat 변화 이벤트 반영(실시간 갱신)
- [x] 3.0 초기 적용 대상(item.angel, upgrade.whetstone) 문구/표시 적용
  - [x] 3.1 item.angel 효과 라인에 현재값 결합 규칙 적용
  - [x] 3.2 upgrade.whetstone 효과 라인에 현재값 결합 규칙 적용
  - [x] 3.3 ko-KR 로컬라이즈 키(현재값 포맷) 추가 및 검증
