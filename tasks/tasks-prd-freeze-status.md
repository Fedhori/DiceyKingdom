## Relevant Files

- `Assets/Scripts/Block/BlockController.cs` - 블럭 상태이상 틱/낙하 이동 및 피격 처리.
- `Assets/Scripts/Block/BlockManager.cs` - 블럭 스폰/정리 및 플레이 영역 관리.
- `Assets/Scripts/Block/BlockInstance.cs` - 블럭 상태이상 데이터 보관(남은 시간 등).
- `Assets/Scripts/Block/BlockStatus.cs` - 상태이상 enum/상태 컨테이너 정의.
- `Assets/Scripts/Data/GameConfig.cs` - 블럭 낙하 기본 속도 설정.
- `Assets/Scripts/Bullet/ProjectileController.cs` - 투사체 피격 시 상태이상 적용 트리거.
- `Assets/Scripts/Item/ItemDto.cs` - 투사체 상태이상 데이터 정의/검증.
- `Assets/Scripts/Item/ItemInstance.cs` - 투사체 상태이상 데이터 런타임 반영.
- `Assets/StreamingAssets/Data/Items.json` - item.snowballer 데이터 추가.
- `Assets/Localization/item Shared Data.asset` - 아이템 키 추가.
- `Assets/Localization/item_ko-KR.asset` - 한글 로컬라이징 추가.
- `Assets/Localization/item_en.asset` - 영문 로컬라이징 추가.

### Notes

- Unit tests는 현재 구조상 생략할 수 있으며, 필요 시 기존 패턴에 맞춰 추가합니다.

## Tasks

- [ ] 1.0 블럭 상태이상 시스템 뼈대 추가
  - [x] 1.1 상태이상 enum/컨테이너 정의 파일 추가 및 BlockInstance에 상태 저장 구조 연결
  - [x] 1.2 BlockInstance에 상태이상 갱신/조회 API 추가(지속시간 비교 포함)
  - [x] 1.3 BlockController에서 상태이상/낙하 업데이트 처리(Play 단계에서만)
- [ ] 2.0 빙결 상태이상(지속시간/감속/갱신) 및 마스크 표시 구현
  - [x] 2.1 빙결 적용 시 지속시간 갱신 규칙(더 긴 쪽 유지) 구현
  - [x] 2.2 빙결 상태일 때 이동 속도 배율(0.7) 적용
  - [x] 2.3 블럭 프리팹 마스크 토글 연동(빙결 상태에 따라 활성/비활성)
- [ ] 3.0 item.snowballer + projectile.snowball 연동 추가
  - [x] 3.1 item.snowballer 데이터 추가(가격/희귀도/스펙/투사체 키)
  - [x] 3.2 projectile.snowball 피격 시 빙결 적용 로직 연결
  - [ ] 3.3 아이템 로컬라이징 키 추가(ko/en)
- [ ] 4.0 동작 검증 및 플레이 흐름 QA 체크
  - [ ] 4.1 눈덩이 피격 시 빙결 적용 및 지속시간 갱신 확인
  - [ ] 4.2 빙결 중 속도 감속/해제 복귀 확인
  - [ ] 4.3 스테이지/플레이 단계 전환 시 상태이상 감소 타이밍 확인
