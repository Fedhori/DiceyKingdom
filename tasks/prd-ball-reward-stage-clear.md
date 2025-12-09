# 스테이지 클리어 볼 보상 PRD

## Introduction/Overview
- 스테이지 클리어 시 기존에 고정된 볼 지급(예: `ball.basic` 5개)을 개선하여, 여러 후보 보상 중 하나를 플레이어가 선택하도록 하는 보상 단계 추가.
- 보상 화면에서 지정된 개수만큼 무작위 볼 보상을 제시하고, 선택 시 해당 볼이 지급되며 보상 단계가 종료된다. 리롤을 통해 보상을 다시 뽑을 수 있다.

## Goals
- 스테이지 클리어 후 보상 화면(`ballRewardOverlay`)이 열리고, 설정된 개수(`ballRewardCount`)의 볼 보상을 노출한다.
- 보상 선택 시 해당 볼이 즉시 지급되고 보상 화면이 닫히며 다음 단계로 진행된다.
- 리롤 버튼을 통해 비용 지불 후 보상 후보를 새로 뽑고, 리롤 비용이 증가하는 흐름을 제공한다.

## User Stories
- 플레이어로서 스테이지를 클리어하면 여러 볼 보상 중 하나를 골라 얻고 싶다. 그래야 원하는 빌드에 맞춰 볼을 선택할 수 있다.
- 플레이어로서 현재 보상이 마음에 들지 않으면 리롤 비용을 내고 새로운 보상 후보를 보고 싶다. 그래야 더 나은 선택지를 노려볼 수 있다.

## Functional Requirements
1. 보상 진입/초기화
   - 스테이지 클리어 후 Reward 단계에서 `RewardManager.ballRewardOverlay`가 열린다.
   - Reward 단계 진입 시 `currentBallRerollCost`를 `baseBallRerollCost`로 초기화하고, 기존 보상 프리팹 인스턴스를 정리한다.
2. 보상 생성
   - `ballRewardCount`만큼 보상을 생성한다(Inspector 설정값 사용).
   - 보상은 `{ballId, ballCount}` 구조다. `ballId`는 기존 보상에 중복되지 않도록 무작위로 선택한다(사용 가능한 고유 `ballId`보다 `ballRewardCount`가 큰 경우 고유 목록을 소진한 뒤 중복 허용).
   - `ballDto.isNotReward`가 `true`인 볼은 보상 후보에서 제외한다.
   - `PlayerManager.Instance.Current.BallCost`를 `BallCost`라 하고, 1.0~1.5 무작위 계수를 곱해 `adjustedCost`를 만든다.
   - `ballCount = ceil(adjustedCost / ballDto.cost)` 공식을 사용한다(`ballDto`는 선택된 `ballId`에 해당하는 데이터).
   - `ballRewardPrefab`을 `ballRewardParent` 아래에 `ballRewardCount`개 instantiate한다.
3. 보상 표시 및 인터랙션
   - 보상 프리팹(에디터에서 세팅됨)에 `BallRewardController`가 포함되어 있으며, 런타임에 선택된 `ballId`에 맞는 아이콘을 `SpriteCache.GetBallSprite(ballId)`로 설정하고 `ballCount`를 표시한다.
   - 플레이어가 보상 항목을 클릭하면 `PlayerManager.Instance.Current.BallDeck.Add(ballId, ballCount)`로 지급하고, 자동으로 `RewardManager.Close()`를 호출하여 보상 화면을 닫는다. 선택 후 추가 입력은 모두 비활성화한다.
4. 리롤
   - 리롤 버튼 클릭 시 `CurrencyManager.TrySpend(currentBallRerollCost)`로 `CurrentCurrency`를 소모해야 한다. 비용이 부족하면 버튼 텍스트를 `Colors.Red`로 바꾸고 클릭 불가 상태로 유지한다.
   - 리롤에 성공하면 기존 보상 프리팹을 모두 파괴하고, Functional Requirement 2의 규칙을 따라 새 보상 세트를 생성한다.
   - 리롤이 발생할 때마다 `currentBallRerollCost += ballRerollCostIncrement`.
5. 상태/호환
   - 기존 Reward 플로우와 호환되도록 `RewardManager.Close()` 이후 후속 로직은 기존 흐름을 그대로 따른다(추가 처리 없음).
   - `.meta` 파일은 Unity가 관리하므로 수동으로 수정하거나 생성하지 않는다.

## Non-Goals (Out of Scope)
- 스테이지 데이터 구조나 클리어 조건 변경.
- 볼 데이터(BallDto) 정의, 밸런스 수치(개별 `cost` 값) 변경.
- 다른 유형 보상(골드, 핀 등) 추가/조정.
- 새로운 통화나 경제 시스템 설계.

## Design Considerations
- 보상 카드: 아이콘 + 수량 텍스트 명확히 노출, 클릭 영역은 카드 전체. 리롤 불가 상태일 때 버튼 텍스트 컬러를 붉게 표시하고 인터랙션 차단.
- 기존 UI 톤을 유지하며, 오버레이 활성/비활성 시 애니메이션이 필요하면 현행 패턴을 따른다(새 모션 추가는 범위 밖).

## Technical Considerations
- 데이터 소스: `StaticDataManager`/`BallRepository`에 있는 볼 목록과 `ballDto.cost` 사용. `ballDto.isNotReward`가 `true`인 항목은 필터링한다.
- 무작위: Unity `Random` 계열 사용(프로젝트 관례 준수).
- 비용 처리: `CurrencyManager.CurrentCurrency`를 대상으로 `CurrencyManager.TrySpend(cost)` 사용. 실패 시 UI 비활성/적색 처리.
- 코드 스타일: Unity C# 컨벤션(4-스페이스, Allman braces). 컴포넌트 참조는 프리팹/인스펙터에서 설정.

## Success Metrics
- 스테이지 클리어 시 보상 오버레이가 열리고 `ballRewardCount`개 항목이 생성된다.
- 각 항목 클릭 시 지정된 `ballId`/`ballCount`가 지급되고 보상 화면이 닫힌다.
- 리롤 시 자원이 충분하면 비용이 차감되고 새 보상으로 교체되며, 리롤 비용이 누적 증가한다. 자원이 부족하면 버튼이 비활성화/적색 표시된다.

## Open Questions
- 없음.
