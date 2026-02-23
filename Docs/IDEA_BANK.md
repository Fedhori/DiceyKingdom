# IDEA_BANK
> 역할: 구현 여부와 관계없이 아이디어/카드/유닛 초안을 **유실 없이 보관**하는 문서입니다.

- 마지막 갱신: `2026-02-21`

---

## 1) Squad 아이디어

| Squad | Supply Cost | 소환/구성 |
|---|---:|---|
| 예비대 | 1 | 예비군 ×2 |
| 미코 암살부대 | 1 | 미코 암살자 ×1 |
| 드워프 포병단 | 3 | 드워프 대포 ×2 |
| 랫킨즈 | 1 | 랫킨 ×3 |

---

## 2) Action 아이디어

> 표기 기준: `Attack = dX`, 수치 증감 기본은 Attack Result 변화

| Action | Attack | 효과(초안) |
|---|---:|---|
| 예비군 | 2 | 조건 충족 시 **Attack Modifier(+2, layer=Duel)** 누적 |
| 미코 암살자 | 4 | 해당 전장에 배치된 적이 1개뿐이면 **Attack Result ×2** |
| 드워프 대포 | 4 | 같은 전장의 적 Action들의 **Attack Result -1** (최소 1) |
| 랫킨 | 2 | (효과 없음) |

---

## 3) 강화(아이디어)

- Squad 내부 Action들의 Attack Result +1
- 선택한 Action 1개의 Attack Result +2
- Squad의 Supply Cost -1

> 참고: P0에서는 Base Attack 직접 변경 금지. Modifier 기반 런타임 보정은 허용.

---

## 4) Skill(P0 후보)

| Skill | Focus | Cooldown | Timing | 효과 |
|---|---:|---:|---|---|
| 재배치(Redeploy) | 2 | 2 | Skill | 아군 Action 1개를 다른 전장으로 이동(굴림값 유지) |
| 유인책(Decoy) | 2 | 2 | Deploy | 적 Action 1개를 다른 전장으로 이동(굴림값 유지) |
| 위험한 접근(Risky) | 1 | 1 | Deploy | 플레이어 Victory → Great Victory |
| 안전한 접근(Safe) | 1 | 1 | Deploy | 플레이어 Great Defeat → Defeat |
| 증원(Reinforce) | 2 | 2 | Skill | 전장 1개의 아군 Total Attack +2 (눈 변화 없음) |
