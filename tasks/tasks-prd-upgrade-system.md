## Relevant Files

- `Assets/StreamingAssets/Data/Upgrades.json` - 강화 데이터 정의 파일(신규).
- `Assets/Scripts/Upgrade/UpgradeDto.cs` - 강화 DTO/조건/효과 정의(신규).
- `Assets/Scripts/Upgrade/UpgradeInstance.cs` - 강화 인스턴스 및 룰 처리(신규).
- `Assets/Scripts/Upgrade/UpgradeRepository.cs` - 강화 데이터 로드/조회(신규).
- `Assets/Scripts/Upgrade/UpgradeConditionEvaluator.cs` - 강화 조건 판정 유틸(신규).
- `Assets/Scripts/Upgrade/UpgradeManager.cs` - 강화 적용/교체 처리(신규).
- `Assets/Scripts/Shop/ProductType.cs` - 상점 상품 타입에 Upgrade 추가 필요.
- `Assets/Scripts/Shop/ShopItemFactory.cs` - Upgrade 상품 생성 로직 추가.
- `Assets/Scripts/Shop/ShopManager.cs` - 상점 슬롯 80/20 롤링 및 강화 구매/적용 흐름.
- `Assets/Scripts/Shop/ShopView.cs` - 강화 상품 전용 UI 프리팹 생성/바인딩.
- `Assets/Scripts/Shop/ProductViewBase.cs` - 상점 상품 공통 뷰 베이스(신규).
- `Assets/Scripts/Shop/UpgradeProductController.cs` - 강화 상품 UI 컨트롤러(신규).
- `Assets/Scripts/Shop/UpgradeProduct.cs` - 강화 상품 모델(신규).
- `Assets/Scripts/Data/SpriteCache.cs` - 강화 아이콘 스프라이트 로딩 경로 추가.
- `Assets/Scripts/UI/GhostKind.cs` - 강화 전용 고스트 타입 추가.
- `Assets/Scripts/Token/ItemSlotManager.cs` - 강화 유효 슬롯 하이라이트/드래그 적용 처리.
- `Assets/Scripts/Token/ItemSlotController.cs` - 강화 선택 시 슬롯 클릭 적용 처리.
- `Assets/Scripts/Item/ItemInstance.cs` - 적용된 강화 보관 및 아이템 스탯 반영 지점.
- `Assets/Scripts/Item/ItemEffectManager.cs` - 강화 효과를 ItemEffect로 적용.
- `Assets/Scripts/Stat/GameStat.cs` - `StatLayer.Upgrade` 추가.
- `Assets/Scripts/Tooltip/ItemTooltipUtil.cs` - 아이템 툴팁에 강화명 표시.
- `Assets/Scripts/Tooltip/UpgradeTooltipUtil.cs` - 강화 툴팁 생성(신규).
- `Assets/Scripts/Tooltip/TooltipManager.cs` - 강화 툴팁 노출 흐름 연동.
- `Assets/Scripts/Data/LocalizationUtil.cs` - 강화 이름/효과 로컬라이징 접근.
- `Assets/Scripts/Tooltip/TooltipModel.cs` - 강화 라벨 오버라이드 지원.
- `Assets/Scripts/Tooltip/TooltipView.cs` - 강화 라벨 표시 처리.
- `Assets/Scripts/UI/ItemTooltipTarget.cs` - 강화 툴팁 바인딩 지원.
- `Assets/Localization/upgrade Shared Data.asset` - 강화 로컬라이징 키 테이블.
- `Assets/Localization/upgrade_ko-KR.asset` - 강화 한글 로컬라이징 데이터.

### Notes

- 현재 테스트 프레임워크가 정착되어 있지 않으므로 수동 QA 중심으로 계획.

## Tasks

- [x] 1.0 강화 데이터 파이프라인 구축
  - [x] 1.1 `Upgrades.json` 스키마 정의 및 샘플 강화(힘의 돌/바람의 돌) 데이터 추가
  - [x] 1.2 `UpgradeDto`/조건/효과 구조 설계 및 JSON 파싱 로직 구현
  - [x] 1.3 `UpgradeRepository` 초기화/조회 API 추가 및 로더 연동
  - [x] 1.4 `UpgradeInstance` 생성 및 조건 체크/효과 데이터 보관 구조 구현
- [x] 2.0 상점에 강화 상품 혼합 등장 및 전용 UI 연결
  - [x] 2.1 `ProductType`에 `Upgrade` 추가 및 `UpgradeProduct` 모델 도입
  - [x] 2.2 `ShopItemFactory`에 강화 상품 생성 로직 추가
  - [x] 2.3 `ShopManager`에서 슬롯마다 아이템 80%/강화 20% 롤링 적용
  - [x] 2.4 강화 전용 UI 프리팹을 사용하도록 `ShopView`/`UpgradeProductController` 연결
  - [x] 2.5 강화 상품의 아이콘 표시 및 “강화” 라벨 표시(희귀도 자리)
- [x] 3.0 강화 적용/교체 및 아이템 스탯 반영 처리
  - [x] 3.1 `StatLayer.Upgrade` 추가 및 아이템 스탯 적용 경로 정리
  - [x] 3.2 아이템에 강화 1개 보관 필드 추가(교체 시 기존 제거)
  - [x] 3.3 강화 적용 조건(HasDamageMultiplier/HasAttackSpeed/HasProjectile) 판정 로직 구현
  - [x] 3.4 강화 선택 시 유효 슬롯 하이라이트 및 드래그/클릭 적용 흐름 추가
  - [x] 3.5 강화 적용 시 `ItemEffect` 기반으로 아이템 스탯 변경 적용/해제
- [x] 4.0 강화/아이템 툴팁 표시 로직 추가
  - [x] 4.1 강화 툴팁 모델/유틸 생성(이름/효과/“강화” 라벨)
  - [x] 4.2 강화 상품 hover 시 툴팁 노출 연동
  - [x] 4.3 아이템 툴팁 제목에 강화명 표시 로직 추가
