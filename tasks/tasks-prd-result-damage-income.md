## Relevant Files

- `Assets/Scripts/ResultManager.cs` - 결과창 열기/닫기 및 수입/리스트 바인딩 로직 수정.
- `Assets/Scripts/Damage/DamageTrackingManager.cs` - 아이템 인스턴스 참조 기반 데미지 기록 조회(직접 읽기) 지원.
- `Assets/Scripts/UI/ItemView.cs` - 아이콘 표시용 기존 UI 컴포넌트(재사용 가능).
- `Assets/Scripts/UI/Result/ResultDamageRow.cs` - 리스트 항목 UI 바인딩용 신규 스크립트(아이콘/데미지/그래프).
- `Assets/Scripts/UI/Result/ResultOverlayView.cs` - 결과창 리스트/수입/버튼 연결을 위한 신규 뷰 스크립트.
- `Assets/Localization/result Shared Data.asset` - 결과창 로컬라이즈 키(신규).
- `Assets/Localization/result_ko-KR.asset` - 결과창 한국어 텍스트(신규).
- `Assets/Localization/result.asset` - 결과창 로컬라이즈 테이블 컬렉션(신규).
- `Assets/Scenes/GameScene.unity` - 결과창 UI 배치 및 ResultManager 참조 연결.

### Notes

- 테스트 파일은 현재 프로젝트 구조에 맞춰 필요 시 추가한다.

## Tasks

- [ ] 1.0 DamageTrackingManager 기록 조회 API 정비
  - [x] 1.1 결과 UI용 레코드 구조(UniqueId, ItemId, Damage) 정의
  - [x] 1.2 아이템 인스턴스별 데미지 기록을 읽어오는 공개 메서드 추가
  - [x] 1.3 ResultManager에서 사용할 수 있도록 읽기 전용 컬렉션 반환 방식 확정
- [ ] 2.0 ResultManager 결과창 표시 로직 업데이트
  - [x] 2.1 `Open()`에서 자동 `Close()` 호출 제거
  - [x] 2.2 수입 표시를 `BaseIncome + BaseIncomeBonus` 값으로 갱신
  - [x] 2.3 DamageTrackingManager 기록을 읽어 0 데미지 제외 + 내림차순 정렬
  - [x] 2.4 최대 데미지 값을 계산해 리스트 바인딩에 전달
  - [x] 2.5 확인 버튼 클릭 시 `Close()`만 실행하도록 연결
- [ ] 3.0 결과 리스트 UI 항목/뷰 스크립트 추가
  - [x] 3.1 `ResultDamageRow` 스크립트 생성(아이콘/데미지/그래프 바 참조)
  - [x] 3.2 `Bind(itemId, damage, maxDamage)` 구현(아이콘/정수 데미지/바 비율)
  - [x] 3.3 `ResultOverlayView` 스크립트 생성(콘텐츠 초기화, 행 프리팹 생성)
- [ ] 4.0 결과창 로컬라이즈(result 테이블) 추가
  - [ ] 4.1 `result` 로컬라이즈 테이블 생성(Shared/ko-KR/Collection)
  - [ ] 4.2 키/문구 추가: `result.title`, `result.damage.title`, `result.income.label`, `result.confirm.button`
  - [ ] 4.3 결과창 UI의 LocalizeStringEvent 연결
- [ ] 5.0 씬/프리팹 연결(에디터 작업)
  - [ ] 5.1 GameScene에서 ResultOverlay에 ScrollView/Content/프리팹 배치
  - [ ] 5.2 ResultManager/ResultOverlayView에 UI 레퍼런스 연결
  - [ ] 5.3 DamageTrackingManager가 managersRoot에 존재하는지 확인
