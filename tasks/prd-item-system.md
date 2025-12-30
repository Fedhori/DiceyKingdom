# PRD: 아이템(탄환 발사 포탑) 시스템 추가

## 소개/개요
- 보드 하단에 플레이어가 조작할 수 있는 아이템(포탑)을 추가하고, 공격속도에 따라 탄환을 발사해 브릭을 파괴한다.
- 데이터는 Items.json → DTO → Instance → Controller/Manager/Factory 계층으로 관리한다.
- Players.json에 시작 아이템 ID(itemIds, 복수)를 지정해 게임 시작 시 아이템을 보유한다.

## 목표
1. 아이템 데이터를 로드/인스턴스화하고 스탯 배율을 적용한다.
2. 플레이어를 보드 하단에 배치하고 좌우 이동(A/D 또는 좌/우 화면 터치) 가능하게 한다.
3. 공격속도에 따라 탄환을 자동 발사(위쪽 고정 방향)하고, 브릭 충돌 시 플레이어 basePoint × 아이템 피해 배율로 피해를 준다.
4. 탄환은 새 프리팹을 사용(SerializeField 연결). Rigidbody2D 충돌 기반.
5. GameConfig에 아이템 이동/탄환 크기/탄환 속도 base값을 추가해 배율 계산에 사용한다.

## 사용자 스토리
- 플레이어로서 A/D나 화면 좌/우 터치로 아이템(포탑)을 이동해 브릭을 맞추고 싶다.
- 플레이어로서 아이템이 자동으로 탄환을 발사해 브릭을 파괴하며 점수를 얻고 싶다.
- 플레이어로서 게임 시작 시 지정된 아이템을 자동으로 보유하고 싶다.

## 기능 요구사항
1. 데이터/구조  
   - `Assets/StreamingAssets/Data/Items.json`에 아이템 정의: id(기본 item.default), damageMultiplier(기본 1), attackSpeed(기본 1).  
   - Players.json에 시작 아이템 id들을 나타내는 `itemIds` 필드 추가(복수 아이템 지원).  
   - Item DTO/Repository/Instance/Controller/Manager/Factory 계층 구현.  
   - ItemInstance 스탯(최소값 모두 0.1): 피해 배율, 공격속도, 이동 속도, 탄환 크기, 탄환 속도.
2. 배치/이동  
   - 시작 위치: x=0, y = PlayArea 하단 + offset 64.  
   - 이동 범위: PlayArea 좌우 끝으로 클램프.  
   - 이동 입력: InputManager MoveX(A/D) 사용, PlayArea 좌/우 반 화면 터치/홀드 시 좌우 이동(홀드 중 지속 이동).  
   - 이동 속도 = GameConfig baseMoveSpeed × instance 이동 속도 배율.
3. 발사/탄환  
   - 발사 방향: 위쪽(90도) 고정.  
   - 공격속도: 초당 N발(attackSpeed), 타이머 기반 자동 발사.  
   - 탄환: 새 프리팹 SerializeField 연결(코드에서 참조), Rigidbody2D 충돌 사용.  
   - 탄환 스폰 위치: 아이템 상단 기준(필요 시 offset SerializeField).  
   - 탄환 크기/속도 = GameConfig baseBulletSize/baseBulletSpeed × instance 배율.  
   - 브릭 충돌 시 피해 = 플레이어 basePoint × 아이템 피해 배율. 점수/트리거는 기존 브릭 파괴 흐름과 동일 처리.  
   - 브릭 외 오브젝트 충돌: 기본은 무시/관통 가정(필요 시 후속 조정).
4. GameConfig  
   - Base 값 추가: ItemBaseMoveSpeed, ItemBaseBulletSize, ItemBaseBulletSpeed(기본값은 임의로 설정 후 인스펙터 조정).
5. 매니저 흐름  
   - ItemManager가 Players.json의 itemIds로 시작 아이템들을 로드/생성.  
   - ItemFactory로 아이템/탄환 생성 담당.  
   - ItemController가 입력, 발사 타이머, 위치 클램프, 탄환 스폰 포인트 관리.

## 비범위
- 업그레이드/변경 UI, 다중 아이템 지원, 상점/획득 로직.
- 아이템 회전 에임(방향은 위쪽 고정).
- 탄환 특수 효과/관통/폭발 등 추가 효과.

## 디자인/리소스
- 아이템/탄환 프리팹은 새로 생성해 SerializeField로 연결(에디터 작업 지시 필요).
- PlayArea 기준 좌우/하단 위치 계산.

## 기술 고려사항
- InputManager MoveX 재사용, 화면 좌/우 반 클릭/터치 입력 추가.  
- JSON 로드/DTO/Repository 패턴 기존 스타일 준수.  
- Rigidbody2D로 탄환 충돌 처리, 브릭 충돌 시 기존 점수/트리거와 동일 흐름.

## 성공 지표
- 게임 시작 시 지정된 아이템이 나타나고 이동/발사 가능.  
- 공격속도에 맞춰 탄환이 자동 발사되고 브릭에 피해를 준다.  
- 이동 범위가 PlayArea를 넘지 않고 입력(키/터치)에 따라 즉시 반응.  
- GameConfig/Base/Instance 배율 적용이 인스펙터 조정으로 튜닝 가능.

## 오픈 질문
- (확정) 탄환이 브릭 외 오브젝트와 충돌 시 무시/관통.  
- (확정) 탄환 스폰 오프셋 기본 0(아이템/플레이어 중심 기준).
