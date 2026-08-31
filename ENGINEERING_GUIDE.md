# ENGINEERING_GUIDE

이 문서는 TacticalCombatSystem 저장소의 개발·검수 계약을 관리하는 단일 source of truth다. 외부 공통 guide를 복제하거나 동기화하지 않는다. 현재 작업에 관련된 절만 읽고, 일반적인 C# 지식보다 이 저장소의 실제 코드와 계약을 우선한다.

## 1. 작업 범위와 질문 경계

- 사용자의 수정·구현 요청은 요청 범위 안의 로컬 편집과 비파괴적 build/test를 승인한 것으로 본다.
- 요구사항에 없는 refactoring, package 교체, 파일 대규모 이동, 공개 계약 변경은 하지 않는다.
- 요청에 포함되지 않은 공개 API, JSON·table field, Unity·.NET·package version, DB schema, scene·prefab 구조, 결정성·balance 변경이 필요하면 먼저 확인한다.
- 기존 worktree의 사용자 변경을 보존하고, 관련 없는 파일을 정리하거나 되돌리지 않는다.

## 2. 프로젝트 핵심 계약

TacticalCombatSystem은 Unity Client가 전투를 실행하고 Server가 같은 CommonLib 전투 로직으로 결과를 재현·검증한다.

1. CommonLib 전투 규칙은 Client와 Server에서 동일하게 동작한다.
2. 동일 입력은 platform과 실행 이력에 관계없이 동일 결과를 만든다.
3. 화면 표현과 Server 검증은 전투 판정을 중복 구현하지 않는다.

## 3. 폴더와 책임

### CommonLib

`Assets/Script/CommonLib`에는 전투 상태·규칙, Entity·Projectile·Map·Pathfinding, fixed-point 값 type, 공용 DTO·table을 둔다.

- `MonoBehaviour`, `UnityEditor`, View, HTTP, DB 구현체를 참조하지 않는다.
- Client와 Server에서 같은 source가 compile되는지 함께 확인한다.
- `BattleMapData`의 `BlockedPoints`, `Waypoints` 원본을 바꾸는 기능에는 cache invalidation을 포함한다.

### ClientLib

`Assets/Script/ClientLib`은 Unity 표현과 Server 호출을 담당한다.

- `ClientBattleMapSimulator`는 CommonLib event를 View 조작으로 변환한다.
- `EntityView`, `ProjectileView`는 model·animation·표현 상태만 갱신한다.
- 피해, 상태 전이, pathfinding 같은 전투 규칙을 자체 구현하지 않는다.

### EditorLib

`Assets/Script/EditorLib`은 map·obstacle·배치 데이터 작성과 저장을 담당한다.

- Editor 전용 코드를 runtime assembly에 섞지 않는다.
- JSON 형식 변경은 기존 map data와 scene 참조의 호환성을 확인한다.

### Server

`Server/MiniServerProject`의 책임은 다음과 같다.

- Controllers: HTTP 입력 검증과 응답
- Application: use case 조합, 멱등성 흐름, transaction 경계
- Domain: User 상태 전이, 게임 규칙, Server 전투 재현
- Infrastructure: EF Core, MySQL, Redis와 구현체

## 4. 전투 상태와 경계

- `EntityBrain`은 다음 상태의 종류를 판단한다.
- `IState`와 각 State는 `Enter`, `Update`, `Exit` 행동을 담당한다.
- `Entity`는 상태 교체와 자신의 data·상태를 관리한다.
- `BattleMapSimulator`는 Entity·Projectile 생성·갱신·제거와 전투 종료를 관리한다.
- 도메인 조회·요청은 `IBattleMapContext`, 사건 전달은 `IBattleMapEventHandler`를 사용한다.

새 Entity state를 추가하면 `EntityStateType`, `EntityBrain`, state 구현체, `Entity.GetState`, 전이 test, Client event·animation 영향을 함께 확인한다.

## 5. 수치와 결정성

- 좌표·방향·거리 계산은 `FixedPos`, `FixedDir`, `GridPos`와 정수 계산을 유지한다.
- CommonLib 전투 규칙에 float/double을 새로 도입하지 않는다.
- `deltaMs`와 fixed-point 배율, 기존 `ushort`·`uint`·`ulong`·`long` 범위를 암묵적으로 바꾸지 않는다.
- Entity·Projectile 조회에는 Dictionary를 사용할 수 있지만, 결과에 영향을 주는 순회 순서는 ID 오름차순이 보장된 별도 목록을 사용한다.
- 매 tick의 `OrderBy`, collection 복사, 정렬을 피한다.
- 동률 선택 기준은 거리, 이후 EntityId처럼 명시한다.
- 생성 순서가 달라도 같은 EntityId 집합과 입력이면 결과가 같아야 한다.

결정성 변경은 다른 삽입 순서, 동일 거리 target, 같은 tick의 공격·피격·사망, 소환·제거 뒤 순서, Client·Server 최종 결과와 event 순서를 검증한다.

## 6. 코드 스타일과 Unity 계약

- 인접 코드의 멤버 순서, 접근 제한자, guard clause, 빈 줄 패턴을 우선한다.
- 메서드 선언·호출과 간단한 식은 한 줄로 무리 없이 읽을 수 있으면 불필요하게 여러 줄로 나누지 않는다. 한 줄이 지나치게 길거나 논리 구조를 드러낼 필요가 있을 때만 줄바꿈한다.
- type·public member는 PascalCase, private field는 `_camelCase`, 지역 변수·매개변수와 직렬화 public field는 camelCase를 사용한다.
- 변수와 메서드 이름은 쉬운 단어를 사용하고, 대상과 역할을 코드만 읽어도 직관적으로 이해할 수 있게 짓는다. 생소한 전문 용어나 불필요하게 추상적인 표현은 피한다.
- Dictionary 조회는 `TryGetValue`, 계약상 새 key 등록은 `Add`, 의도적인 갱신은 indexer를 사용한다.
- 단일 문장 guard clause는 중괄호 없이 다음 줄에 쓰고, 여러 문장은 중괄호로 묶는다.
- `Update` 경로에 매 frame `Find`, `GetComponent`, LINQ, 정렬, 새 collection 생성을 추가하지 않는다.
- `UnityEngine.Object`는 Unity bool null semantics를 사용하고, 일반 C# 참조에는 `null` 비교를 유지한다.
- Inspector field 이름, prefab 자식 이름, scene 구조, Resources·asset 경로는 외부 계약이다.
- 직렬화 field를 바꿀 때 `FormerlySerializedAs`를 기본 해법으로 추가하지 않는다. 사용자가 migration을 요청하지 않았다면 scene·prefab 참조를 직접 다시 연결하고 검증한다.
- 주석은 원칙적으로 한국어로 쓰되, 직역이 어색한 technical term·API·고유 계약명은 자연스러운 영어 표현을 유지한다.
- 주석은 코드의 동작을 반복하지 않고 이유·제약·계약·임시 선택을 설명한다. TODO에는 해결 대상과 남겨 둔 이유를 적는다.

## 7. Server 규칙

- Controller는 필수 ID와 request를 검증하고 Application을 호출한다. DB 상태를 직접 변경하지 않는다.
- 도메인 규칙 위반은 `DomainException`과 공통 middleware를 통해 일관된 API 오류로 변환한다.
- 변경성 요청은 Redis cache → DB log → 유효성·상태 전이 → DB log 기록 → UNIQUE 충돌 시 기존 log 재조회 → cache 저장 흐름을 유지한다.
- Redis는 최종 정합성 근거가 아니며, DB UNIQUE 제약과 조건부 UPDATE가 최종 보호 장치다.
- 새 POST API는 같은 requestId 재시도, 다른 requestId 동시 요청, cache·DB log hit, UNIQUE 충돌을 검토한다.
- `CancellationToken`은 Controller부터 외부 호출·영속성 계층까지 전달한다.

## 8. 검증 명령과 성공 판정

Unity Editor가 같은 project를 열고 있으면 batch test가 중단될 수 있으므로 먼저 종료 여부를 확인한다.

```powershell
dotnet build .\CommonLib.csproj
dotnet build .\Server\MiniServerProject\MiniServerProject.csproj

& 'C:\Program Files\Unity\Hub\Editor\2022.3.58f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\Github\TacticalCombatSystem' `
  -runTests `
  -testPlatform EditMode `
  -logFile 'D:\Github\TacticalCombatSystem\Temp\EditMode.log'
```

- build 성공은 test 통과나 실제 Play 확인을 대신하지 않는다.
- Unity test 성공은 `Running tests for EditMode`, 관련 suite의 pass/fail, 정상 종료가 같은 실행 로그에 있는지 확인한다.
- 결과 XML은 생성되는 경우에만 보조 artifact로 사용하며, XML 미생성만으로 로그 기반 통과를 무효화하지 않는다.
- 결정성·순서 변경은 같은 입력의 반복 실행과 입력 삽입 순서 변형을 포함한다.
- UI·animation·effect·movement 체감은 자동 test와 별도로 실제 Play 화면에서 확인하고, 남은 수동 항목을 기록한다.
- 생성한 log·XML·trace는 `Temp` 아래에 두고 source 변경과 섞지 않는다.

## 9. 완료 보고

변경 파일, 핵심 결정, 실행한 검증과 결과, 남은 수동 확인만 보고한다. 검증하지 못한 항목은 통과로 추정하지 않는다. 높은 위험 변경은 독립 reviewer가 현재 diff와 실행 근거를 직접 확인한다.
