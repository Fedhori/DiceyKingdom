## Relevant Files

- `Assets/Scripts/Flow/FlowManager.cs` - Ready/Play 전환과 입력 허용 상태 관리
- `Assets/Scripts/Stage/StageManager.cs` - Ready 상태 준비, Play 전환, 볼 스폰 시퀀스 진입부
- `Assets/Scripts/Ball/BallManager.cs` - 볼 스폰 시퀀스/방향 설정 로직 적용 지점
- `Assets/Scripts/Ball/BallFactory.cs` - 실제 볼 생성 및 초기 속도/방향 적용
- `Assets/Scenes/GameScene.unity` - BallDragArea 오브젝트와 조준 시각화(Sprites) 배치/연결 확인
- `tasks/prd-ball-drag-launch.md` - 요구사항 참고용

### Notes

- Input System 기반 마우스/터치 드래그 지원.
- 시작 위치 y는 월드 좌표 고정값(인스펙터 조정).
- 발사 속도/볼 시퀀스 로직은 기존 유지, 방향만 새로 지정.
- 최소 드래그 거리/유효 각도(위쪽만) 임계값은 인스펙터로 조정 가능하게.

## Tasks

- [x] 1.0 Ready 단계 입력/조준 흐름 설계 및 상태 관리
  - [x] 1.1 FlowPhase Ready 상태에서만 드래그 입력을 허용하고 Play 이후 입력 차단 로직 정리
  - [x] 1.2 BallDragArea와 고정 y 기반 시작 위치 선택 로직 추가(미니 볼 스프라이트 표시)
  - [x] 1.3 드래그 벡터 계산 및 점선 시각화(기존 스프라이트 활용)
  - [x] 1.4 유효성 판정(최소 거리, 위쪽 각도) 및 취소 처리 시 Ready 상태 유지/시각 요소 숨김
- [ ] 2.0 발사 전환 및 스폰 방향 적용
  - [x] 2.1 드래그 Release 시 유효하면 Play로 전환하는 흐름 연결(FlowManager/StageManager)
  - [x] 2.2 기존 볼 스폰 시퀀스에 지정 방향을 반영하도록 BallManager/BallFactory 초기 방향 설정 수정
  - [x] 2.3 무효 드래그 시 기존 랜덤 스폰 로직이 호출되지 않도록 방지
- [ ] 3.0 시각 리소스/씬 연결
  - [ ] 3.1 기존 스프라이트로 미니 볼/화살표 표시 오브젝트 구성 및 인스펙터 연결
  - [ ] 3.2 GameScene에서 BallDragArea 배치/참조 확인 및 새 필드 연결
- [ ] 4.0 검증 및 정리
  - [ ] 4.1 Ready→Play→Reward 흐름에서 드래그 조준 후 발사 동작 확인(스테이지 0, 1+)
  - [ ] 4.2 무효 드래그(짧은 거리/아래 각도) 시 발사되지 않고 Ready 유지 확인
  - [ ] 4.3 코드/필드 누락·경고 정리 (Missing refs, 로그 확인)
