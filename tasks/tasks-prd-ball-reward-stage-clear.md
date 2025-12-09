## Relevant Files

- `Assets/Scripts/RewardManager.cs` - 보상 오버레이 열기/닫기, 보상 생성·리롤 흐름 구현.
- `Assets/Scripts/Reward/BallRewardController.cs` - 보상 카드 UI 데이터 바인딩 및 클릭 시 지급/닫기 처리.
- `Assets/Scripts/Ball/BallDto.cs` - `isNotReward` 필터 확인.
- `Assets/Scripts/Ball/BallRepository.cs` - 보상 후보 볼 목록 조회.
- `Assets/Scripts/Player/PlayerManager.cs` - 현재 플레이어 참조.
- `Assets/Scripts/Player/PlayerInstance.cs` - `BallDeck`, `BallCost` 사용.
- `Assets/Scripts/Currency/CurrencyManager.cs` - 리롤 비용 지불(`CurrentCurrency`, `TrySpend`).
- `Assets/Scripts/SpriteCache.cs` - `GetBallSprite`로 아이콘 로드(경로 추정).
- `Assets/Scripts/Colors.cs` - 리롤 버튼 텍스트 색상(`Colors.Red`).
- `Assets/StreamingAssets/Data/Balls.json` - `isNotReward` 데이터 확인(필요 시).
- `Assets/Prefabs/.../ballRewardPrefab` - 에디터에서 `BallRewardController` 부착, RewardManager 인스펙터 연결 확인.

### Notes

- 유닛 테스트 인프라가 없다면 수동/인게임 검증 위주로 진행.
- 리롤 버튼/텍스트 오브젝트는 Reward UI 프리팹/씬에 이미 배치되어 있다고 가정하고 스크립트 필드로 연결.

## Tasks

- [ ] 1.0 RewardManager 보상 흐름 확장
  - [x] 1.1 Reward 단계 진입 시 `currentBallRerollCost = baseBallRerollCost`, `ballRewardOverlay` 활성화, 이전 보상 인스턴스 정리.
  - [x] 1.2 `BallRepository`에서 `isNotReward == false`인 볼 목록을 가져와 `ballRewardCount`개 보상 생성(고유 우선, 부족 시 중복 허용).
  - [x] 1.3 `BallCost`에 1.0~1.5 난수 계수 곱해 `adjustedCost` 산출, `ballCount = ceil(adjustedCost / ballDto.cost)` 계산.
  - [ ] 1.4 생성된 보상마다 `ballRewardPrefab`을 `ballRewardParent` 아래 instantiate하고 데이터 바인딩 호출.
  - [ ] 1.5 리롤 시 비용 차감 성공 시 기존 보상 파괴 후 1.2~1.4 재실행, 실패 시 리롤 버튼 텍스트를 `Colors.Red`로 세팅하고 인터랙션 차단.
  - [ ] 1.6 보상 선택 완료 시 `RewardManager.Close()`에서 FlowManager 후속 호출 유지, 오버레이 비활성/정리.

- [ ] 2.0 보상 데이터 바인딩/선택 처리
  - [ ] 2.1 `BallRewardController`에서 `Initialize(ballId, count, onSelected)` 형태의 초기화 메서드로 아이콘(`SpriteCache.GetBallSprite`)과 수량 UI 표시.
  - [ ] 2.2 클릭 시 전달받은 콜백으로 `PlayerManager.Instance.Current.BallDeck.Add(ballId, count)` 실행하고 중복 클릭 방지.
  - [ ] 2.3 보상 선택 시 `RewardManager.Close()` 트리거하여 다음 단계로 진행.

- [ ] 3.0 리롤 UI/자원 처리
  - [ ] 3.1 리롤 버튼 클릭 핸들러에서 `CurrencyManager.TrySpend(currentBallRerollCost)`로 비용 지불, 성공 시 `currentBallRerollCost += ballRerollCostIncrement`.
  - [ ] 3.2 `RewardManager.HandleCurrencyChanged`로 `CurrencyManager.CurrentCurrency` 변동을 구독/반영해 리롤 가능 여부와 버튼 상태를 갱신.
  - [ ] 3.3 비용 부족 상태에서 리롤 버튼 텍스트 색을 `Colors.Red`로 표시하고 버튼을 비활성화.
