## Relevant Files

- `Assets/Scripts/Flow/FlowManager.cs` - 단계 전환 순서와 페이즈 관리 로직 수정 지점
- `Assets/Scripts/Stage/StageManager.cs` - 스테이지 시작/종료 호출부 확인 (순서 변경 영향 검토)
- `Assets/Scenes/GameScene.unity` - FlowManager/StageManager 등 씬 내 컴포넌트 연결 상태 확인
- `tasks/prd-reorder-stage-flow.md` - 요구사항 참고용

### Notes

- 트리거/점수 판정 로직은 변경 없이 순서만 조정한다.
- 최초 스테이지(인덱스 0)는 Shop을 건너뛰고 Play→Reward로 진행한다.

## Tasks

- [x] 1.0 흐름 정의 업데이트: 스테이지 인덱스 0 스킵, 1+는 Shop→Play→Reward 순서로 재배치
  - [x] 1.1 FlowManager에서 StartStage 진입 시 스테이지 인덱스 0이면 Shop 없이 Play로 바로 진입하도록 분기 추가
  - [x] 1.2 스테이지 인덱스 1 이상일 때 StartStage → Shop → Play → Reward 순서가 되도록 초기 진입 흐름 정리
- [ ] 2.0 페이즈 전환/호출부 정리: 기존 트리거·점수 판정 로직 유지한 채 순서 변경 반영
  - [ ] 2.1 OnPlayFinished에서 성공 시 Reward로 바로 진입하도록 유지 확인, 실패 시 GameOver 동작 검증
  - [ ] 2.2 OnRewardClosed가 Shop이 아닌 다음 단계(Shop→Play→Reward 순서)에 맞춰 호출되도록 조정
  - [ ] 2.3 OnShopClosed 후 다음 스테이지로 진행 시 새 순서 반복 적용 확인
- [ ] 3.0 씬/레퍼런스 확인: FlowManager/StageManager 등 컴포넌트 연결 및 초기 상태 검증
  - [ ] 3.1 GameScene 등에서 FlowManager/StageManager가 정상 배치·참조되어 있는지 확인
  - [ ] 3.2 필요 시 시리얼라이즈드 필드(ShopView 등) 누락 여부 점검
