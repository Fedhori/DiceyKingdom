## Relevant Files

- `Assets/Scripts/Block/BlockManager.cs` - 블럭 스폰 예산/패턴 선택 로직을 대체할 핵심 파일.
- `Assets/Scripts/Block/BlockFactory.cs` - 패턴 수치(size/speed/health)에 따른 블럭 생성 적용 지점.
- `Assets/Scripts/Block/BlockInstance.cs` - 블럭 체력/상태 값 구조 확인 및 health 적용.
- `Assets/Scripts/Block/BlockController.cs` - 블럭 이동/속도 등 runtime 수치 적용 가능 지점.
- `Assets/Scripts/Data/StaticDataManager.cs` - 패턴 JSON 로딩 등록이 필요한 경우.
- `Assets/StreamingAssets/Data/Blocks.json` - (신규/확장) 패턴 풀 데이터 정의 파일.

### Notes

- 테스트 파일은 현재 구조상 별도 규칙이 없으므로 필요 시 추가 논의.

## Tasks

- [ ] 1.0 패턴 데이터 구조 및 로딩 체계 설계/구현
  - [ ] 1.1 패턴 JSON 스키마 확정 및 `Blocks.json`에 기본 패턴(normal/big/fast) 정의
  - [ ] 1.2 `BlockPattern` DTO/리포지토리/로더 구조 추가(StreamingAssets 로드)
  - [ ] 1.3 런 전체 공유 패턴 풀 접근 API 제공(조회/갱신용 인터페이스 포함)
- [ ] 2.0 스폰 예산 기반 패턴 선택/대기 로직으로 스폰 흐름 대체
  - [ ] 2.1 기존 accumulatedDifficulty 기반 스폰 루프 제거 및 예산 누적 로직 연결
  - [ ] 2.2 패턴을 먼저 선택하고 예산 충족 시 스폰하는 “대기형” 흐름 구현
  - [ ] 2.3 가중치 랜덤 선택 로직 추가(패턴 weight 기준)
- [ ] 3.0 패턴 수치(size/speed/health) 적용 및 블럭 생성 파이프라인 정리
  - [ ] 3.1 블럭 생성 시 health/size/speed 수치 적용 경로 정리
  - [ ] 3.2 BlockFactory/BlockController에 패턴 수치 전달 및 적용
  - [ ] 3.3 기존 스폰 관련 변수/메서드 정리 및 불필요 코드 제거
