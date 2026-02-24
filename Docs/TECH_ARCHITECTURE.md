# TECH_ARCHITECTURE
> ??븷: ?꾩옱 援ы쁽 湲곗? ?꾪궎?띿쿂 ?붿빟.

- 留덉?留?媛깆떊: `2026-02-24`

---

## 1) 怨꾩링

- Domain
  - `DuelState`, `ClashState`, `AbilityInstance`
  - ?쒖닔 ?곹깭/怨꾩궛(`DuelSimulator`)
- Application
  - `DuelPhaseRunner`
  - `DuelSessionBuilder`
  - `DuelTurnProcessor`
  - `AbilityTimedEffectRunner`, `DuelEffectClashResolver`
- Infrastructure
  - `GameDatabaseLoader`, `GameDataValidator`
  - StreamingAssets JSON 濡쒕뵫(Newtonsoft.Json)
- Presentation
  - `DuelDebugPanel`, `DuelAbilityBlockView`

---

## 2) ?곗씠???뚯씠?꾨씪??
1. `Data/DataIndex.json` 濡쒕뱶
2. `configs`, `abilities`, `encounters` ?쒖꽌 ?뚯떛
3. `GameDataValidator` 寃利?4. ?깃났 ??`GameDataRuntime.CurrentDatabase`??諛섏쁺

---

## 3) ?꾪닾 ?ㅽ뻾 ?뚯씠?꾨씪??
1. `DuelSessionBuilder.TryCreateInitialState`
2. `DuelPhaseRunner.StartDuel`
3. `DuelSessionBuilder.AutoDeployOpponentClash`
4. ?뚮젅?댁뼱 諛곗튂(`PlayerSetup`)
5. `DuelTurnProcessor.TryRollAllDeployedAbilities`
6. `DuelTurnProcessor.TryResolveAllClashes`

---

## 4) ?ㅺ퀎 ?먯튃

- ?좉퇋 ?꾪닾 ?곗씠???⑥쐞??Ability濡??듭씪
- 援ъ슜?대뒗 ?좉퇋 肄붾뱶?먯꽌 ?ъ슜?섏? ?딆쓬
- 議곗슜???먮룞 蹂댁젙? ?쇳븯怨? ?꾩슂??寃쎌슦 理쒖냼 Warning 濡쒓렇瑜??④?
- UI 諛곗튂??媛?ν븳 ???먮뵒?곗뿉??泥섎━?섍퀬 肄붾뱶 諛곗튂??遺덇??쇳븳 寃쎌슦留??ъ슜

