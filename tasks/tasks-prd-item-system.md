## Relevant Files

- `Assets/StreamingAssets/Data/Items.json` - 아이템(포탑) 정의 파일.
- `Assets/StreamingAssets/Data/Players.json` - 플레이어 이동 스탯 및 시작 itemIds 정의.
- `Assets/Scripts/GameConfig.cs` - 이동/탄환 base 수치 추가.
- `Assets/Scripts/InputManager.cs` - MoveX 및 화면 좌/우 터치 입력 처리 확장.
- `Assets/Scripts/Stage/StageManager.cs` / `FlowManager.cs` - 플레이어/아이템 초기화, 상태 연계.
- `Assets/Scripts/Player/` - 기존 Player DTO/Instance/Manager 확장(이동 스탯 적용, itemIds 반영).
- `Assets/Scripts/Item/`(신규 예정) - Item DTO/Repository/Instance/Controller/Manager/Factory.
- `Assets/Scripts/Bullet/`(신규 예정) - 탄환 프리팹 제어/충돌 처리.
- `Assets/Prefabs/` - 플레이어/아이템/탄환 프리팹 SerializeField 연결.

## Tasks

- [ ] 1.0 데이터 정의/로딩 (Items.json, Players.json에 itemIds 및 이동 스탯 반영, DTO/Repository 생성)
  - [x] 1.1 Items.json 스키마 확정(id, damageMultiplier, attackSpeed, bulletSize, bulletSpeed 등) 및 기본 item.default 추가
  - [x] 1.2 Players.json에 itemIds(배열)와 이동 속도 배율 필드 추가, DTO/Instance에 반영
  - [x] 1.3 Item DTO/Repository/Instance 계층 추가
  - [x] 1.4 Player DTO/Instance 확장(이동 배율, itemIds 보유)
- [ ] 2.0 GameConfig에 플레이어 이동/탄환 크기·속도 base 수치 추가
  - [x] 2.1 PlayerBaseMoveSpeed, ItemBaseBulletSize, ItemBaseBulletSpeed 필드 추가
  - [x] 2.2 아이템/탄환 계산 시 base × 배율 적용
- [ ] 3.0 플레이어 시스템 구현 (배치, 이동 입력, 범위 클램프, 시작 위치 설정)
  - [x] 3.1 플레이어 시작 위치(x=0, y=PlayArea 하단+64) 배치
  - [x] 3.2 MoveX(A/D) 및 좌/우 반 터치/홀드 입력 처리
  - [x] 3.3 이동 속도 = GameConfig.PlayerBaseMoveSpeed × 플레이어 이동 배율, PlayArea 좌우로 클램프
- [ ] 4.0 아이템 시스템 구현 (itemIds 로드, 플레이어에 장착, 공격속도·피해·탄환 스펙 관리)
  - [x] 4.1 ItemManager가 itemIds로 아이템 생성/장착
  - [ ] 4.2 아이템 발사 타이머(attackSpeed) 및 피해 배율 관리
  - [ ] 4.3 아이템 위치를 플레이어에 종속(이번 범위는 붙어 있는 형태)
- [ ] 5.0 탄환 발사/충돌 처리 (위쪽 고정 발사, 브릭 충돌 시 피해 적용, 기타 오브젝트 무시)
  - [ ] 5.1 탄환 프리팹 SerializeField 연결, 스폰 포인트/오프셋(기본 0) 적용
  - [ ] 5.2 발사 방향 고정(위), 속도/크기 = GameConfig base × 아이템 배율
  - [ ] 5.3 브릭 충돌 시 플레이어 basePoint × 아이템 피해 배율로 피해, 브릭 외 충돌은 무시/관통
- [ ] 6.0 초기화/플로우 연계 (Stage/Flow와 시작 시 생성·장착, 종료 시 정리)
  - [ ] 6.1 Stage 시작 시 플레이어 생성/배치, 아이템 장착
  - [ ] 6.2 Stage 종료 시 탄환/아이템 정리
- [ ] 7.0 에디터 연결 지시 (플레이어/아이템/탄환 프리팹 SerializeField 안내)
  - [ ] 7.1 플레이어 본체, 아이템(포탑), 탄환 프리팹을 각 Manager/Controller에 연결하도록 에디터 작업 가이드 추가
