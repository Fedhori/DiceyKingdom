## Relevant Files

- `Assets/Scripts/Stage/StageManager.cs` - 라운드 시작 흐름(스폰 대기/시작 트리거) 연동 지점.
- `Assets/Scripts/Ball/BallManager.cs` - 선택된 스폰 위치를 받아 볼 스폰 위치로 적용.
- `Assets/Scripts/Ball/BallFactory.cs` - 스폰 시 월드 포지션 지정 지원 필요.
- `Assets/Scripts/Flow/FlowManager.cs` - 라운드 시작 대기/선택 단계와 상태 전환 연동 가능성.
- `Assets/Scripts/Pin/PinManager.cs` - 핀 격자 중심/간격(pinGap, centerOffset) 기반 스폰 지점 계산.
- `Assets/Scripts/UI/BallRarityPanel.cs` (참고) - UI 초기화 패턴 참고용.
- `Assets/Scripts/UI/BallRarityEntryView.cs` (참고) - 프리팹 바인딩 패턴 참고용.
- `Assets/Scripts/Ball/BallController.cs` - 스폰 위치 지정, 초기 속도/위치 설정.
- `Assets/Scripts/UI` 내 신규 상단 안내 텍스트 UI 추가 예정(스폰 선택 단계에서 표시).
- `Assets/Scripts/Flow/FlowPhase`/`FlowManager` - 선택 대기 상태 추가 시 수정 필요.
- `Assets/StreamingAssets/Data/Players.json` - 스폰 개수/희귀도는 기존 설정 유지, 위치만 변경.
- 신규 프리팹/스크립트: 스폰 지점 표시 프리팹(아이콘+알파 페이드), 스폰 지점 매니저/컨트롤러 스크립트.

### Notes

- 스폰 지점은 월드 오브젝트(스프라이트+콜라이더)로 표시, 알파 페이드 애니메이션 필요.
- 선택 전에는 BallManager 스폰 대기, 타임아웃 없음. 선택 즉시 라운드 시작.
- 스폰 좌표 예시(pinGap=64, center=0): (-64,64),(0,64),(64,64) / (-64,0),(0,0),(64,0) / (-64,-64),(0,-64),(64,-64). pinGap=160이면 동일 패턴.
- 스폰 선택 단계에서만 상단 안내 텍스트 UI를 표시/숨김.

## Tasks

- [ ] 1.0 설계/데이터 계산 준비
  - [x] 1.1 PinManager 기준(행/열/간격/센터)으로 3x3 스폰 지점 좌표 계산 헬퍼 구현 또는 유틸 추가
  - [x] 1.2 스폰 지점 프리팹 스펙 정의(스프라이트, 알파 페이드, Collider2D)
- [ ] 2.0 스폰 지점 표시/입력 처리
  - [x] 2.1 스폰 지점 생성/표시 스크립트(지점 매니저) 추가: 라운드 대기 시 9개 지점 생성, 라운드 시작 시 제거/비활성화
  - [x] 2.2 알파 페이드 점멸 효과 구현(코루틴/트윈 등) 및 프리팹 바인딩
  - [x] 2.3 터치/클릭 입력 처리: 단일 입력으로 지점 선택 → 콜백으로 선택 좌표 전달
  - [ ] 2.4 (해당 없음)
- [ ] 3.0 라운드 흐름 연동
  - [x] 3.1 Flow/StageManager에 “스폰 지점 선택 대기” 상태 추가 또는 RoundStart 버튼 클릭 시 지점 표시 후 대기하도록 변경
  - [x] 3.2 선택 콜백에서 BallManager.StartSpawning을 호출하고 선택 좌표를 넘기도록 연계
- [ ] 4.0 스폰 위치 적용
  - [x] 4.1 BallManager/BallFactory가 선택된 시작 위치를 받아 최초 스폰 위치로 사용하도록 수정
  - [x] 4.2 첫 스폰 이후 기존 속도/랜덤 벨로시티 적용 로직이 정상 동작하는지 확인
- [ ] 5.0 상단 안내 텍스트 UI
  - [ ] 5.1 스폰 선택 단계 진입 시 텍스트 표시, 라운드 시작 시 숨김
