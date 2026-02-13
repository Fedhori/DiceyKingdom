## Relevant Files

- `Assets/Scripts/Equipment/EquipmentManager.cs` - 장비 인스턴스/컨트롤러 목록을 관리하며 Register/Unregister 기능을 제공.
- `Assets/Scripts/Equipment/EquipmentFactory.cs` - 장비 UI를 생성하고 파괴하는 전담 Factory.
- `Assets/Scripts/Equipment/EquipmentController.cs` - EquipmentInstance와 EquipmentView를 연결하고 수명주기를 담당.
- `Assets/Scripts/Equipment/EquipmentView.cs` - 장비 UI 본체; 슬롯/쿨다운/텍스트를 표시할 예정.
- `Assets/Scripts/Equipment/DiceSlotView.cs` - 슬롯 조건/값 텍스트를 표시하며 주사위 상태 표현을 담당.
- `Assets/Scripts/Equipment/EquipmentInstance.cs` - 장비 데이터 및 주사위 슬롯/쿨다운 로직.
- `Assets/StreamingAssets/Data/Equipment.json` - 장비/슬롯/조건 설정 데이터.

### Notes

- Prefab 필드는 Unity Inspector에서 설정해야 하므로 코드 수정 후 참조 연결이 필요하다.
- SituationManager/SituationController 구조를 참고해 일관된 수명주기와 이벤트 처리를 유지한다.

## Tasks

- [ ] 1.0 EquipmentFactory 도입 및 EquipmentManager 리팩터링  
  - [x] 1.1 `EquipmentFactory` 싱글톤을 생성하고 `equipmentPrefab`, `diceSlotPrefab`, `equipmentsContainer` SerializeField를 정의한다.  
  - [x] 1.2 Factory에 `SpawnEquipment(string equipId)` 메서드를 작성해 DTO 조회 → EquipmentInstance 생성 → 프리팹 Instantiate를 수행한다.  
  - [ ] 1.3 `EquipmentManager`에서는 Factory API만 사용하도록 조정하고, 기존 프리팹/컨테이너 의존을 제거한다.  
  - [x] 1.4 장비 제거 시 Factory 또는 Manager가 GameObject 파괴를 명확히 담당하도록 Destroy 경로를 정리한다.

- [ ] 2.0 EquipmentController 및 등록 구조 구현  
  - [x] 2.1 `EquipmentController`를 SituationController 패턴에 맞게 작성하고 `Initialize(EquipmentInstance)`로 인스턴스를 주입받는다.  
  - [x] 2.2 OnEnable/OnDisable에서 `EquipmentManager`에 Register/Unregister하도록 구현한다.  
  - [x] 2.3 Controller가 `EquipmentView`를 Bind/Unbind하고 추후 Instance 이벤트를 연결할 준비를 한다.

- [ ] 3.0 EquipmentView & DiceSlotView 확장  
  - [x] 3.1 `EquipmentView`에서 슬롯 동적 생성을 지원하고, Factory의 diceSlotPrefab을 이용해 `diceSlotsContainer`에 슬롯을 구성한다.  
  - [x] 3.2 각 슬롯에 `DiceSlotView`를 연결해 주사위 값/쿨다운/상태를 표현할 수 있도록 한다.
  - [x] 3.4 장비가 쿨다운 상태일 때 `cooldownMask`를 활성화하고, 사용 가능 상태에서는 비활성화하도록 구현한다.

- [ ] 4.0 조건 텍스트 및 이벤트 연동  
  - [x] 4.1 `DiceSlotView`에 조건 타입별 텍스트 표시 로직을 추가한다 (`More` → `N+`, `Less` → `N-`, `Same` → `N`).  
  - [x] 4.2 `EquipmentView`가 `EquipmentDto`의 조건 데이터를 슬롯에 매핑해 조건 없는 슬롯은 빈 문자열로 남도록 한다.  
  - [ ] 4.3 EquipmentInstance(또는 Controller)에서 슬롯 조건 충족 여부와 주사위 배치 상태 변화를 이벤트로 발행하고 UI가 반영되도록 한다.  
  - [ ] 4.4 예시 장비(dagger, shortsword 등)로 조건 텍스트와 쿨다운 마스크가 올바르게 표시되는지 확인한다.
