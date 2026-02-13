# 장비 UI 구현 PRD

## 1. Introduction / Overview
- 전투 중 플레이어가 보유한 장비와 주사위 슬롯을 직관적으로 확인하고 상호작용할 수 있는 UI가 필요하다.  
- 기존 `EquipmentManager`가 장비 인스턴스 생성과 UI 표현을 모두 담당해 복잡도가 높아 유지보수가 어려움.  
- 목표는 Situation 시스템과 유사한 구조(Factory + Controller + View)를 장비에도 적용하여, UI 생성/소멸과 상태 갱신을 일관적으로 처리하는 것이다.

## 2. Goals
1. 장비 인스턴스 생성 로직을 `EquipmentFactory` 싱글톤으로 분리해 책임을 명확히 한다.  
2. `EquipmentController`가 장비 인스턴스를 소유/구독하며 UI 업데이트, 등록/해제를 수행한다.  
3. `EquipmentView`가 주사위 슬롯, 조건 메시지, 장비 아이콘 등을 표시하여 플레이어가 조건을 쉽게 이해할 수 있도록 한다.  
4. Less/More/Same 조건을 시각적으로 강조해 플레이어가 즉시 사용 가능 여부를 판단하게 한다.

## 3. User Stories
- **전투 UI 사용자**: “전투 중 어떤 장비에 어떤 주사위를 넣어야 하는지 바로 알고 싶다. 조건이 맞지 않으면 즉시 알 수 있어야 한다.”  
- **디자이너/밸런서**: “데이터(Equipment.json)만 수정해도 UI가 자동으로 장비 슬롯을 구성해주면 좋겠다.”  
- **개발자**: “장비 UI 생성/파괴가 Situation과 같은 패턴이라면 디버깅과 확장이 쉬워질 것이다.”  

## 4. Functional Requirements
1. **EquipmentFactory**
   - `EquipmentFactory : MonoBehaviour` 싱글톤.  
   - SerializeField: `GameObject equipmentPrefab`, `Transform equipmentsContainer`, `GameObject diceSlotPrefab`.  
   - `SpawnEquipment(string equipId)` → EquipmentRepository에서 DTO 조회, EquipmentInstance 생성, equipmentPrefab 인스턴스화, `EquipmentController` 초기화 후 반환.  
   - Factory는 Controller가 Destroy될 때까지 참조하지 않으며, Controller가 파괴 신호를 줄 경우 GameObject를 파괴한다.
2. **EquipmentManager 리팩토링**
   - 기존 `equipmentPrefab`, `equipmentsContainer` 직접 참조를 제거하고 Factory 사용.  
   - `AddEquipment`/`RemoveEquipment` 방식 대신, 필요한 곳에서 Factory 호출 및 Controller Register/Unregister 호출만 수행하도록 변경.  
   - `EquipmentManager`는 인벤토리 목록/데이터 관리에 집중한다.
3. **EquipmentController**
   - SituationController 패턴 참고: `Initialize(EquipmentInstance instance)`로 인스턴스 주입.  
   - Awake/OnEnable에서 `EquipmentManager` 또는 새 `EquipmentTracker`에 Register, OnDisable/OnDestroy에서 Unregister 수행.  
   - `EquipmentView` 참조를 보유하고 Instance 이벤트를 구독해 UI 반영 (쿨다운, 슬롯 상태 등).  
   - DiceSlotView 목록을 관리하며, `EquipmentInstance`가 가진 슬롯 수만큼 슬롯을 생성/삭제한다.
4. **EquipmentView & DiceSlotView 확장**
   - `EquipmentView`는 `diceSlotsContainer` 아래에 Factory가 넘긴 diceSlotPrefab을 이용해 슬롯을 생성한다.  
   - 각 슬롯에는 `DiceSlotView`가 있으며, `conditionText` 필드에 조건을 표시.  
   - 조건 타입이 `More`, `Less`, `Same`인 경우 각각 `N+`, `N-`, `N` 형식으로 표기 (N은 DTO에서 지정된 value).  
   - 다른 조건 타입은 기존 표시 방식 유지(또는 빈 문자열).  
   - 슬롯 생성 시 `EquipmentInstance`에서 초기 주사위 입력 상태를 반영하고, 주사위가 배치/해제될 때 UI를 업데이트한다.
   - 장비가 쿨다운 상태라면 `cooldownMask`를 활성화하고, 사용 가능 상태에서는 비활성화하여 플레이어에게 명확히 보여준다.
5. **Event Flow**
   - `EquipmentInstance`는 장비 사용, 주사위 배치, 조건 충족 여부 변화를 알리는 이벤트를 제공(기존 이벤트 활용 or 필요 시 신규 이벤트 정의).  
   - Controller는 이벤트를 구독해 View를 업데이트하고, 필요한 경우 `DiceSlotView`에 상태(할당 주사위 값, 조건 미충족 표시 등)를 전달한다.

## 5. Non-Goals
- 장비 밸런스 조정이나 새로운 장비 데이터 추가는 포함하지 않는다.  
- 주사위 굴림 로직, 턴 처리 등 전투 규칙 자체는 변경하지 않는다.  
- 플레이어 인벤토리/장비 획득 시스템의 UX는 이번 작업 범위에 포함되지 않는다.

## 6. Design Considerations
- Situation 시스템과 동일한 구조를 재사용함으로써 코드 일관성을 유지한다.  
- DiceSlotView는 프리팹으로 제공되며, `EquipmentView`는 해당 프리팹을 동적 생성해 Grid/Horizontal Layout에서 배치한다.  
- 조건 텍스트는 짧은 형식을 사용(N+, N-, N)하여 HUD 공간을 최소화한다.

## 7. Technical Considerations
- `EquipmentFactory`는 `EquipmentRepository`와 `EquipmentInstance`를 통해 데이터를 가져온다.  
- `EquipmentController`는 `EquipmentInstance` 이벤트(`OnDiceAssigned`, `OnCooldownChanged` 등 필요 시 구현)를 통해 UI를 동기화한다.  
- Dice 조건 타입: 최신 enum (`More`, `Less`, `Same` 등)을 기반으로 표기 문자열을 결정해야 한다.  
- 등록/해제 로직은 `SituationManager`와 동일하게 `EquipmentManager` 혹은 별도 `EquipmentTracker`에서 리스트를 유지한다.  
- Prefab 참조는 Inspector에서 관리하며, `.meta` 파일은 건드리지 않는다.

## 8. Success Metrics
- 전투 중 장비 UI가 Factory/Controller 구조로 안정적으로 생성·해제된다.  
- DTO 조건이 변경되거나 슬롯 수가 달라도 View가 자동으로 올바른 개수의 DiceSlot을 표시한다.  
- Less/More/Same 조건을 가진 장비에서 조건 텍스트가 정확히 N-/N+/N 형식으로 표기되어 QA 테스트를 통과한다.

## 9. Open Questions
- EquipmentInstance가 주사위 슬롯 변화 이벤트를 충분히 제공하는가? 필요 시 어떤 이벤트를 추가해야 하는가?  
- 장비가 삭제되거나 교체될 때 Controller 파괴를 누가 트리거하는지(Factory 또는 Manager)는 명확히 정의되어야 한다.  
- Dice 조건이 복수 개일 때의 표시 규칙(예: Same + More 동시 존재)은 어떻게 처리할지? 추가 지침이 필요하다.
