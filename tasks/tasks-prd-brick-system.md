## Relevant Files

- `Assets/Scripts/Brick/BrickInstance.cs` - 벽돌 데이터(HP 등)
- `Assets/Scripts/Brick/BrickController.cs` - 벽돌 개별 오브젝트(HP 텍스트, 충돌 처리)
- `Assets/Scripts/Brick/BrickManager.cs` - 8×16 그리드 관리, 스폰/이동, 게임오버 판정
- `Assets/Scripts/Brick/BrickFactory.cs` - 벽돌 생성/풀링 담당
- `Assets/Scripts/Flow/FlowManager.cs` - 스테이지 시작 시점 훅(벽돌 이동/스폰 트리거)
- `Assets/Scripts/Stage/StageManager.cs` - Stage 시작/전환 시 BrickManager 호출 지점
- `Assets/Scripts/Ball/BallController.cs` - 벽돌 충돌 시 데미지 적용 경로
- `Assets/Scenes/GameScene.unity` - PlayArea 배치, Brick 프리팹 레퍼런스 연결
- `tasks/prd-brick-system.md` - 요구사항 참고용

### Notes

- PlayArea 좌측 상단 기준 8×16 그리드(벽돌 크기 128×64, PlayArea 1024×1024)로 배치.
- 스테이지 시작 시 기존 벽돌 2칸 하강 + 새 2줄 스폰, 하단 초과 시 GameOver.
- GameOver는 벽돌 하단 도달만으로 판정(점수 미달 GameOver 제거).
- 데미지 = BallInstance.ScoreMultiplier × PlayerManager.Current.BaseScore.
- 벽돌 HP 텍스트는 TMP_Text로 체력 변경 시 갱신.

## Tasks

- [x] 1.0 벽돌 기본 구조 추가 (Instance/Controller/Factory)
  - [x] 1.1 BrickInstance 정의: HP(기본 100) 및 상태 관리
  - [x] 1.2 BrickController: HP 텍스트(TMP), 스프라이트 설정, HP 갱신/파괴 처리
  - [x] 1.3 BrickFactory: 프리팹 생성/풀링, 초기 HP 세팅
- [ ] 2.0 그리드/매니저 로직
  - [x] 2.1 BrickManager에서 8×16 그리드 관리(PlayArea 좌측 상단 기준 좌표계, 월드 좌표 계산)
  - [x] 2.2 스테이지 시작 시 기존 벽돌을 2칸 아래로 이동, 상단에 새 2줄 스폰
  - [x] 2.3 하단 초과 시 GameOver 처리(기존 점수 미달 GameOver 비활성화/제거)
  - [ ] 2.4 벽돌 없어진 칸 정리 및 배열/리스트 동기화
- [ ] 3.0 충돌/데미지 처리
  - [ ] 3.1 BallController 충돌 시 데미지 = ScoreMultiplier × PlayerManager.Current.BaseScore 적용
  - [ ] 3.2 체력 0 이하 시 파괴 및 HP 텍스트 갱신
  - [x] 3.3 크리티컬 로직 적용 및 FloatingTextManager를 통한 플로팅 텍스트 표시 (기존 로직 재사용)
- [ ] 4.0 스테이지 흐름 연동
  - [ ] 4.1 FlowManager/StageManager에서 스테이지 시작 훅에 BrickManager 이동/스폰 호출 연결
  - [ ] 4.2 기존 점수 미달 GameOver 호출부 제거/비활성화 확인
- [ ] 5.0 씬/리소스 연결
  - [ ] 5.1 Brick 프리팹에 스프라이트, TMP_Text 연결 및 Factory 참조 세팅
  - [ ] 5.2 GameScene에서 PlayArea 좌표에 맞춰 BrickManager/Factory 레퍼런스 연결
- [ ] 6.0 검증
  - [ ] 6.1 스테이지 시작마다 2줄 하강+스폰, 16줄 초과 시 GameOver 확인
  - [ ] 6.2 볼 충돌 시 데미지/HP 텍스트 갱신, 파괴 동작 확인
  - [ ] 6.3 기존 점수 GameOver가 발생하지 않는지 확인
