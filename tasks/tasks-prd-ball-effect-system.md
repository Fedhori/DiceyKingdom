## Relevant Files

- `Assets/StreamingAssets/Data/Balls.json` - Ball 데이터/효과 스키마 추가 및 기본 효과 정의.
- `Assets/Scripts/Ball/BallDto.cs` - Ball 효과 필드 직렬화/역직렬화 확장.
- `Assets/Scripts/Data/StaticDataManager.cs` - Balls.json 로드 경로 및 리포지토리 초기화 연동.
- `Assets/Scripts/Ball/BallInstance.cs` - 효과 평가/발동 파이프라인과 충돌 시점 훅 추가.
- `Assets/Scripts/Ball/BallFactory.cs`/`BallController.cs` - 충돌 이벤트 연동 시 참고.
- `Assets/Scripts/Pin/PinInstance.cs`/`PinEffectManager.cs`/`PinDto.cs` - 동일 구조 참조용.
- `Assets/Scripts/Tooltip/BallTooltipUtil.cs`/`BallTooltipTarget.cs`/`BallUITooltipTarget.cs` - 효과 설명/로컬라이징/툴팁 업데이트.

### Notes

- Pin 효과 구조와 동일한 필드/흐름을 Ball에도 적용.
- 신규 enum/DTO 추가 시 기본값 검증/로그를 Pin 수준으로 맞출 것.

## Tasks

- [ ] 1.0 데이터 스키마 확장 및 로드
  - [x] 1.1 Balls.json에 Pin과 동일한 효과 필드(트리거/컨디션/이펙트 리스트) 스키마 추가
  - [x] 1.2 BallDto에 효과 필드/타입 추가 및 역직렬화 기본값 검증(PinDto 수준의 로그/유효성 체크)
  - [x] 1.3 BallRepository/StaticDataManager에서 신규 필드 로드 및 기존 필드 호환성 확인
- [ ] 2.0 Ball 효과 런타임 파이프라인 구현
  - [x] 2.1 Ball 트리거/컨디션/효과 enum/DTO 정의(Pin과 동일 네이밍/값으로 정렬)
  - [x] 2.2 BallInstance에 효과 평가 흐름 추가(트리거별 조건 검사 → 다중 효과 실행), 충돌 이벤트에 훅 연결(OnHitBall/OnHitPin 등)
  - [x] 2.3 PinEffectManager와 유사한 BallEffectManager(또는 유틸)로 효과 실행 분리 및 재사용 구조 설계
- [ ] 3.0 요구 효과(점수 배율 +1) 정의 및 적용
  - [x] 3.1 Balls.json에 “Ball-Ball 충돌 시 상대 Ball 점수 배율 +1” 룰 추가
  - [x] 3.2 Balls.json에 “Ball-Pin 충돌 시 자기 Ball 점수 배율 +1” 룰 추가
  - [ ] 3.3 런타임에서 두 효과가 누적 적용되는지 검증(배율 증가 반영)
- [ ] 4.0 로컬라이징/툴팁 반영
  - [ ] 4.1 효과 설명 문자열 키를 Ball용으로 추가(핀 포맷과 동일 변수명 사용)
  - [ ] 4.2 BallTooltipUtil에 효과 설명/값 노출 로직 추가(Pin과 동일 포맷)
  - [ ] 4.3 Tooltip 타겟(BallTooltipTarget/BallUITooltipTarget)에서 신규 설명이 표시되는지 확인
- [ ] 5.0 수동 검증 가이드 마련
  - [ ] 5.1 에디터/플레이 모드에서 Ball-Pin, Ball-Ball 충돌로 효과 발동/누적 여부 확인 절차 정리
  - [ ] 5.2 로컬라이징 문자열/툴팁 표시 확인 절차 정리
