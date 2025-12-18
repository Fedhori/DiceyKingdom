## Relevant Files

- `Assets/Scripts/Data/StaticDataManager.cs` - Balls.json 로딩 제거 및 Players.json 스키마 확장 반영 필요.
- `Assets/Scripts/Data/PlayerRepository.cs` - 플레이어 데이터에 희귀도 확률/볼 개수/성장율/초기 배율 필드 추가 및 검증.
- `StreamingAssets/Data/Players.json` - 희귀도 확률, 라운드당 볼 개수, 성장율/배율 기본값을 포함하도록 스키마/샘플 데이터 수정.
- `Assets/Scripts/Ball/BallInstance.cs` - id/Dto 의존성을 제거하고 희귀도/배율/색상 중심으로 재구성, 성장율 적용 경로 추가.
- `Assets/Scripts/Ball/BallManager.cs` - 라운드 시작 시 희귀도 확률 기반 볼 생성 시퀀스 구성으로 변경.
- `Assets/Scripts/Ball/BallFactory.cs` - id 기반 스폰 로직 제거 또는 단일 프리팹/희귀도 파라미터 기반 스폰으로 단순화.
- `Assets/Scripts/Score/ScoreManager.cs` - 희귀도 배율을 점수 계산에 반영.
- `Assets/Scripts/Shop/ShopManager.cs` - 볼 판매/리롤/드래그 UI 및 로직 제거, 핀 관련 UI만 유지.
- `Assets/Scripts/Colors.cs` - 희귀도별 색상 참조 확인(필요 시 확장).
- `Assets/Scripts/Ball/BallController.cs` - 초기화 경로를 희귀도 기반으로 단순화.
- `Assets/Scripts/Ball/BallEffectManager.cs` `Assets/Scripts/Ball/BallDto.cs` 등 id/효과 기반 레거시 - 제거/정리(삭제됨).
- `Assets/Scripts/Stage/StageManager.cs` - 라운드 시작 시 새 스폰 시퀀스 연동 확인.
- `Assets/Scripts/Shop/ShopView.cs` - 볼 상점 UI 제거 후 핀 UI만 유지.

### Notes

- 확률 합이 100%가 아닐 때는 에러 로그를 남기고 정규화해서 사용해야 함.
- 성장율(기본 2) 변경 시 희귀도별 배율을 재계산하도록 런타임 경로 필요.

## Tasks

- [x] 1.0 데이터 스키마/로딩 정리
  - [x] 1.1 StaticDataManager에서 Balls.json 로딩 경로 제거 및 Players.json 확장 반영
  - [x] 1.2 Players.json 스키마/샘플에 희귀도 확률, 라운드당 볼 개수, 성장율(기본 2), 희귀도별 초기 배율 필드 추가
  - [x] 1.3 PlayerRepository(및 DTO)에서 새 필드 파싱/보정 로직 추가
- [x] 2.0 플레이어 희귀도 파라미터 적용
  - [x] 2.1 PlayerInstance에 희귀도 확률/볼 개수/성장율/초기 배율을 보관하는 구조 추가
  - [x] 2.2 확률 합 검증: 100% 미만/초과 시 에러 로그 후 정규화 처리
  - [x] 2.3 성장율 변경 시 희귀도별 배율을 재계산하는 메서드 제공
- [x] 3.0 볼 엔티티/스폰 파이프라인 리팩터링
  - [x] 3.1 BallDto/BallRepository/Balls.json 의존 제거 및 관련 클래스 정리(필요 시 삭제/더미화)
  - [x] 3.2 BallInstance를 희귀도·배율·색상 중심으로 단순화하고 초기화 시 희귀도 설정/색상 적용
  - [x] 3.3 BallManager: 플레이어 설정(볼 개수, 희귀도 확률) 기반으로 라운드 스폰 시퀀스 생성
  - [x] 3.4 BallFactory/BallController 초기화 경로를 희귀도 파라미터 기반으로 조정하고 id 의존 제거
  - [x] 3.5 BasicBallId 의존 제거 및 관련 로직 비활성화(상점/통계/메인메뉴)
- [x] 4.0 점수 계산 희귀도 배율 반영
  - [x] 4.1 ScoreManager.CalculateScore에서 볼 희귀도 배율을 곱하도록 변경
  - [x] 4.2 배율/성장율 변경 시 ScoreManager 경로가 최신 값을 참조하도록 확인
- [x] 5.0 상점 볼 관련 기능 제거
  - [x] 5.1 ShopManager에서 볼 판매/리롤/드래그/볼 아이템 관련 데이터 구조 및 UI 바인딩 제거
  - [x] 5.2 볼 관련 Prefab/뷰 스크립트 참조 정리, 핀 관련 기능만 유지
- [ ] 6.0 검증/에러 로깅 및 기본값 처리
  - [ ] 6.1 확률 합 에러 로깅 + 정규화 처리 구현
  - [ ] 6.2 성장율/배율 기본값이 누락되거나 0/음수일 때 기본값(성장율 2, 배율 1/2/4/8/16)으로 보정
  - [ ] 6.3 런타임 예외/로그 확인을 위한 간단 샘플(디버그 출력) 경로 마련(필요 시)
