# TacticalCombatSystem ENGINEERING_GUIDE

## 공통 기준의 포함과 동기화

이 문서는 TacticalCombatSystem 저장소만 열어도 개발·검수 기준을 완전히 확인할 수 있도록, 공통 ENGINEERING_GUIDE.md의 기준을 저장소 안에 함께 기록한다.

공통 기준의 원본 문서는 AI 협업 작업 공간의 `engineering/ENGINEERING_GUIDE.md`에 있다. 실제 로컬 경로는 작업 환경별로 다르므로 저장소에 기록하지 않는다.

이 문서의 `공통 개발 기준` 부분은 원본 문서를 복제한 내용이며, 이후의 TacticalCombatSystem 전용 부분은 이 프로젝트의 구조·도메인·검증 기준을 관리한다.

### 동기화 규칙

- 작업 전에는 원본 공통 기준 문서와 이 문서의 공통 개발 기준 부분을 함께 읽고, 내용이 동기화되었는지 확인한다.
- 공통 기준을 변경할 때는 원본 문서와 이 저장소의 복제 부분을 같은 작업에서 함께 수정한다.
- TacticalCombatSystem에만 적용되는 기준은 원본 공통 기준 문서에 추가하지 않는다.
- 원본 공통 기준 문서가 변경되면, 이 저장소의 공통 개발 기준 부분에도 해당 변경을 반영한다.
- 두 문서의 내용이 다르면 원본 문서를 기준으로 차이를 검토하되, 프로젝트 전용 예외는 이 문서에 명시적으로 남긴다.

## 공통 개발 기준

### 문서 우선순위와 작업 범위

새 코드는 다음 순서로 기준을 적용한다.

1. 사용자의 최신 지시
2. 저장소의 AGENTS.md, README.md, 운영 문서
3. 프로젝트별 `ENGINEERING_GUIDE.md`
4. 공통 기준 원본 문서
5. 수정 대상과 인접 코드의 관례

새 문법이나 일반론보다 기존 코드와의 일관성을 우선한다. 요구사항에 없는 리팩터링, 패키지 교체, 파일 대규모 이동, 공개 계약 변경은 하지 않는다.

변경 전에는 수정 대상의 호출자와 호출 대상, 데이터의 생성·변경·저장·폐기 위치, Unity 생명주기 또는 서버 요청 경로, 기존 테스트와 수동 검증 방법, 공개 API·직렬화 데이터·네트워크 계약·씬 참조에 미치는 영향을 확인한다.

### 책임 분리와 의존성

- 도메인 규칙, 상태 전이, 데이터 변경은 화면·입력·네트워크 표현과 분리한다.
- MonoBehaviour는 씬, 입력, UI, 애니메이션, 오디오, 표현 이벤트를 조정한다.
- 도메인 계산은 UnityEngine, UnityEditor, View, HTTP, DB 구현체에 직접 의존하지 않는다.
- 외부 의존성은 Interface, Context, Handler, Service 등 명시적 경계를 통해 연결한다.
- 상태를 변경하는 책임자는 하나로 정한다.
- 화면·에디터·클라이언트는 도메인 규칙을 중복 구현하지 않는다.
- 객체 생명주기 동안 필요한 협력자는 생성자에서 받고, 한 호출에만 필요한 값은 메서드 매개변수로 전달한다.
- 데이터 읽기, 계산, 상태 변경, 외부 전송을 한 메서드에 섞지 않는다. 함께 수행해야 한다면 단계와 실패 처리를 명시한다.

### 이름과 타입

- namespace, type, public/protected/internal member는 PascalCase를 사용한다.
- interface에는 `I` 접두사를 사용한다.
- private field는 `_camelCase`, 지역 변수와 매개변수는 `camelCase`를 사용한다.
- JSON, Unity Inspector, 테이블 등 직렬화 데이터의 public field는 `camelCase`를 사용한다. field 이름은 직렬화 계약이므로 기존 이름을 변경하거나 property로 전환하지 않는다.
- 상수는 PascalCase를 사용하고, 컬렉션은 복수형으로 짓는다.
- ID는 대상 이름과 함께 쓰고, 시간·거리·수량은 단위를 이름에 포함한다.
- bool은 상태나 질문이 드러나는 이름을 사용한다.
- 역할 타입에는 `…Service`, `…Handler`, `…Context`, `…Factory`, `…Response`처럼 책임을 나타내는 접미사를 사용한다.
- 단순 조회는 `Get…`, 실패 가능한 조회는 `TryGet…`, 상태 확인은 `Is…` 또는 `Has…`를 사용한다.
- 상태 변경은 `Set…`, 추가는 `Add…`, 제거는 `Remove…`, 외부 요청은 `Request…`, 이벤트 통지는 `On…`으로 표현한다.
- 비동기 메서드에는 `Async` 접미사를 사용한다.
- `Do…`는 Unity 연출이나 명시적인 게임 흐름을 시작하는 경우에 사용한다. 도메인 로직에서는 결과와 책임이 드러나는 동사를 우선한다.

### 상태 노출과 데이터 모델

- 도메인 객체의 변경 가능한 내부 상태는 private field로 둔다.
- 외부 조회는 getter-only property 또는 `get; private set;` property로 제공한다.
- 생성 뒤 변하지 않는 식별자와 의존성은 getter-only property 또는 readonly field를 사용한다.
- JSON, Unity Inspector, 테이블 등 직렬화 데이터는 기존 계약에 맞춰 단순 public field를 사용할 수 있다.
- 직렬화 편의를 이유로 도메인 객체의 상태를 public field로 바꾸지 않는다.
- 상속 계약이 없는 Application, Service, Controller, Context 같은 타입은 sealed를 우선 검토한다.
- Unity MonoBehaviour, 직렬화 데이터, 확장이 의도된 도메인 타입에는 sealed를 기계적으로 적용하지 않는다.
- 데이터 key, JSON field, PlayerPrefs key, prefab 참조 이름은 호환성 계약이다. 변경 시 기존 데이터와 참조의 이행 방식을 함께 검토한다.

### 제어 흐름과 예외 처리

- 단일 문장 guard clause와 단순 분기는 중괄호 없이 다음 줄에 한 문장으로 작성한다.
- 여러 문장을 실행하거나 분기 내부 상태가 복잡하면 중괄호를 사용한다.
- 실패, 없는 대상, 처리 불가 상태는 먼저 검사하고 `return`, `continue`, `throw`로 종료한다.
- 정상 흐름은 guard clause 뒤에 들여쓰기 없이 둔다.
- `else`는 두 결과 중 하나를 반드시 선택해야 하는 값 대입·상태 갱신에만 사용한다.
- 우선순위가 있는 상태 판단은 `if-return`을 위에서 아래로 나열한다.
- 반복문에서는 제외 조건을 먼저 `continue`하고, 남은 본문에는 처리 대상의 핵심 동작만 둔다.
- Dictionary 조회는 인덱서보다 `TryGetValue`를 우선한다.
- 계약 위반, 복구 불가능한 설정 오류, 서버 요청 오류는 명시적인 예외 또는 도메인 오류로 처리한다.
- switch에서 다루지 못한 enum 값은 조용히 무시하지 않고 예외 또는 명시적인 fallback을 고려한다.

### 컬렉션, 캐시, 수치

- Dictionary는 key·ID 기반 조회에 사용한다.
- List는 순서가 있는 데이터, 명시적 순회, API 입력에 사용한다.
- HashSet은 중복이 없어야 하는 집합에 사용한다.
- 결과에 영향을 주는 처리 순서는 자료구조의 우연한 열거 순서에 맡기지 않는다.
- 실행 빈도가 높은 경로에는 LINQ, 매 호출 컬렉션 생성, 불필요한 정렬을 추가하지 않는다.
- 지연 계산 캐시는 private field에 저장하고, 외부에는 읽기 property를 제공한다.
- 원본 데이터를 런타임에 변경하는 기능에는 캐시 무효화 규칙도 함께 구현한다.
- 수치 타입은 도메인의 범위와 정밀도 요구에 맞춰 선택한다.
- 위치·거리·시간처럼 단위가 있는 값은 이름과 타입으로 단위를 드러낸다.
- 플랫폼 간 재현성이 필요한 규칙에는 부동소수점 도입 여부와 계산 순서를 검토한다.

### Unity 규칙

- Inspector 참조 이름, prefab 자식 이름, scene 구조, Resources·Asset 경로는 외부 계약이다.
- 직렬화 field의 이름·접근 제한자 변경은 prefab/scene 참조 영향을 확인한 뒤에만 수행한다.
- Update 계열에 매 프레임 Find, GetComponent, 정렬, LINQ, 새 컬렉션 생성을 추가하지 않는다.
- Coroutine, Tween, 이벤트 구독은 비활성화·재시작·씬 전환 때 중복 실행과 해제를 확인한다.
- `async void`는 Unity 이벤트 콜백처럼 Task를 반환할 수 없는 경계에서만 사용한다.
- Unity Object의 null 검사는 Unity의 null semantics에 맞춰 작성한다.

### 오류, API, 비동기

- 클라이언트 또는 UI 경계는 실패를 결과값과 로그로 표현하고, 호출자가 이후 흐름을 명시적으로 중단하게 한다.
- 서버 또는 API 경계는 계약 위반을 명시적인 도메인 오류로 표현하고, 일관된 예외 처리 경로를 둔다.
- 오류 로그에는 작업명과 실패 원인을 포함한다.
- 서버 로그에는 사용자·요청·대상 ID를 구조화된 placeholder로 기록한다.
- 비동기 서버 API는 CancellationToken을 외부 호출과 영속성 계층까지 전달한다.
- 변경성 API에서는 재시도와 동시 요청을 고려한다.
- 캐시는 응답 최적화 수단이며, 최종 정합성은 영속 저장소의 제약과 조건부 상태 변경으로 보장한다.
- CommonLib와 ClientLib은 기존의 null 검사와 조기 반환 흐름을 유지한다. Server는 nullable reference type, `?? throw`, 도메인 오류를 사용해 요청 계약 위반을 전파할 수 있다.

### 코드 서식과 주석

- 한 파일 안에서는 기존 멤버 순서, 이름짓기, 접근 제한자, 조건문·중괄호·빈 줄·주석 패턴을 우선 따른다.
- 파일 내부의 일관성은 전역 규칙을 기계적으로 적용하는 것보다 우선한다. 새 코드만 다른 표현이나 구조를 도입하지 않는다.
- 파일의 기존 관례가 안전성·정확성·명시된 프로젝트 규칙과 충돌하면, 충돌 이유를 기록하고 더 작은 범위의 일관된 개선을 제안한다.
- 네 칸 들여쓰기를 사용하고, 중괄호는 새 줄에 둔다.
- 의미 있는 처리 단계 사이에는 빈 줄을 둔다.
- 짧은 계산·단순 getter는 expression-bodied member를 허용한다.
- 복잡한 조건은 줄을 나눠 의도를 드러낸다.
- 주석은 코드가 무엇을 하는지 반복하지 않고, 이유·제약·데이터 계약·임시 선택을 설명한다.
- TODO에는 해결 대상과 맥락을 적고, 해결되지 않은 TODO를 새 코드로 확산하지 않는다.

### 테스트와 검증

- 기능 변경에는 정상, 실패, 경계 조건을 포함한다.
- 시간·순서·난수·동시성이 결과에 영향을 주면 재현 가능한 입력으로 검증한다.
- 테스트명은 대상 기능, 전제 조건, 기대 결과를 드러낸다.
- 특정 시나리오의 기대값은 테스트 본문에서 명시한다. 비교 helper는 기대값을 매개변수로 받아 여러 시나리오에서 재사용할 수 있게 작성한다.
- 버그 수정은 가능한 경우 수정 전 실패하는 재현 사례를 남긴다.
- 외부 DB·파일 등 테스트 자원은 항상 정리한다.
- 테스트를 실행하지 못했다면 이유와 수동 검증 범위를 분리해 기록한다.
- 빌드 성공은 동작 검증을 대체하지 않는다.
- 컴파일 성공은 테스트 실행 통과가 아니다. 자동 테스트는 러너의 pass/fail 결과와 결과 XML 등 실행 artifact를 확인해 기록한다.

### 질문과 승인 경계

- 공개 API, 저장 형식, JSON/테이블 field 변경
- Unity·.NET·패키지 버전 변경
- DB schema와 migration 변경
- prefab·scene 구조와 Inspector 연결 변경
- 성능·결정성·밸런스에 영향을 주는 동작 변경
- 대규모 rename, 파일 이동, 구조 전환
- 비밀값, 서버 주소, 외부 서비스 설정 변경

위 항목은 명시적 승인 없이 결정하지 않는다.

---

## TacticalCombatSystem 전용 기준

## 1. 프로젝트 목표

TacticalCombatSystem은 Unity 클라이언트가 전투를 실행하고, 서버가 같은 공용 전투 로직으로 결과를 재현·검증하는 프로젝트다.

핵심 품질 기준은 다음 세 가지다.

1. 공용 전투 규칙은 클라이언트와 서버에서 동일하게 동작한다.
2. 동일 입력은 플랫폼과 실행 이력에 관계없이 동일 결과를 낸다.
3. 화면 표현과 서버 검증은 전투 판정을 중복 구현하지 않는다.

## 2. 폴더와 책임

### CommonLib

- 컴파일 성공은 테스트 실행 통과가 아니다. Unity EditMode 테스트는 테스트 러너의 통과·실패 결과와 결과 XML 등의 실행 artifact를 함께 확인하고 기록한다.

`Assets/Script/CommonLib`은 클라이언트와 서버가 공유한다.

여기에 둘 수 있는 코드:

- 전투 상태와 전투 규칙
- Entity, Projectile, Map, Pathfinding
- FixedPos, FixedDir, GridPos 같은 결정성 값 타입
- 공용 요청·응답 DTO
- 정적 테이블과 테이블 조회
- 공용 JSON·로그·파일 보조 코드

여기에 두지 않는 코드:

- MonoBehaviour
- Unity View, Animator, GameObject
- UnityEditor, AssetDatabase
- 서버 DB, Redis, Controller, HTTP 관련 코드

`BattleMapData`의 `BlockedPoints`, `Waypoints` 지연 계산 캐시를 변경할 때는 cache invalidation도 함께 구현한다.

### ClientLib

`Assets/Script/ClientLib`은 Unity 클라이언트 표현과 서버 호출을 담당한다.

- `ClientBattleMapSimulator`는 공용 시뮬레이터의 이벤트를 받아 View를 만든다.
- `EntityView`, `ProjectileView`는 모델·애니메이션·표현 상태를 갱신한다.
- 네트워크 App 계층은 API 호출 순서와 클라이언트 상태를 관리한다.
- ClientLib는 전투 피해, 상태 전이, 경로 탐색 규칙을 자체 구현하지 않는다.

### EditorLib

`Assets/Script/EditorLib`은 맵·장애물·배치 데이터의 작성과 저장을 담당한다.

- Editor 전용 코드는 CommonLib나 ClientLib의 런타임 어셈블리에 섞지 않는다.
- 편집 데이터의 JSON 형식 변경은 기존 맵 데이터 호환성을 확인한다.

### Server

`Server/MiniServerProject`는 다음 책임으로 구분한다.

- Controllers: HTTP 입력 검증, Application 호출, HTTP 응답
- Application: 유스케이스 조합, 멱등성 흐름, 트랜잭션 경계
- Domain: User 상태 전이, 게임 규칙, 서버 전투 재현
- Infrastructure: EF Core, MySQL, Redis, 구현체와 설정

## 3. 전투 도메인 작성 방식

### 상태 전이

Entity는 자신의 데이터와 상태 객체를 보유한다.

- `EntityBrain`: 다음 상태의 종류를 판단한다.
- `IState`: `Enter`, `Update`, `Exit` 계약을 제공한다.
- `IdleState`, `MoveState`, `AttackState`, `DieState`: 상태별 행동을 담당한다.
- `Entity`: 상태 교체, 공용 Context 호출, 자신의 기본 데이터와 상태를 관리한다.
- `BattleMapSimulator`: Entity와 Projectile의 생성·갱신·제거 및 전투 종료를 관리한다.

새 상태를 추가할 때는 다음을 함께 수정한다.

1. `EntityStateType`
2. `EntityBrain.ThinkNextStateType`
3. 상태 구현체
4. `Entity.GetState`
5. Enter/Update/Exit 전이 검증
6. Client 이벤트·애니메이션 필요 여부

### Context와 이벤트

도메인 객체는 구체적인 Client나 Server를 직접 참조하지 않는다.

- 도메인에서 필요한 조회·요청은 `IBattleMapContext`에 추가한다.
- 도메인 사건의 전달은 `IBattleMapEventHandler`를 사용한다.
- Client는 이벤트를 View 표현으로 변환한다.
- Server는 필요한 경우 이벤트를 기록·검증에 사용하되, 표현 의존성을 만들지 않는다.

### 값 타입과 수치

- 좌표·방향·거리 계산은 `FixedPos`, `FixedDir`, `GridPos`와 정수 계산을 유지한다.
- 전투 규칙에 float/double을 새로 도입하지 않는다.
- 수치 범위를 검토해 기존의 `ushort`, `uint`, `ulong`, `long` 선택을 유지한다.
- `deltaMs` 단위와 fixed-point 배율은 암묵적으로 바꾸지 않는다.
- 임시 상수는 도메인 의미가 생기는 시점에 `GameParameters` 또는 적절한 데이터 설정으로 이동하는 것을 검토한다.

## 4. 결정성은 필수 계약

Dictionary는 ID 조회를 위한 자료구조이며, 전투 결과에 영향을 주는 순회 순서를 제공하는 자료구조가 아니다.

현재 `BattleMapSimulator`는 `_entities.Values`, `_projectiles.Values`를 순회하고, 최근접 적 탐색도 `_entities.Values`를 순회한다. 이 방식은 문서의 결정론 목표에 맞추어 개선해야 하는 기준선이다.

전투 순회 규칙은 다음과 같다.

- Entity와 Projectile은 Dictionary로 빠르게 조회한다.
- 시뮬레이션 갱신과 타겟 탐색은 ID 오름차순이 보장된 별도 순회 목록을 사용한다.
- 생성·제거 시에만 순회 목록을 갱신한다.
- 매 tick의 `OrderBy`, 컬렉션 복사, 정렬은 사용하지 않는다.
- 동률 상황의 타겟 선택 기준도 명시한다. 예: 거리 우선, 이후 EntityId 오름차순.
- 생성 순서가 다르고 같은 EntityId 집합을 가진 경우에도 결과가 같아야 한다.
- 사망, 소환, Projectile 제거 뒤에도 다음 tick의 순회 순서가 유지되어야 한다.

결정성 변경은 다음을 함께 검증한다.

- 다른 삽입 순서의 Entity 구성
- 동일 거리 적의 타겟 선택
- 공격, 피격, 사망이 같은 tick에 일어나는 경우
- 소환·제거 뒤의 순서
- 동일 update interval로 실행한 클라이언트와 서버의 최종 승패·생존 HP
- 가능하면 이벤트 로그 순서까지의 일치
- tick별 GC 할당 또는 정렬 비용 증가 여부

## 5. ClientLib 규칙

- `ClientBattleMapSimulator`는 전투 규칙을 계산하지 않고 이벤트를 View 조작으로 변환한다.
- Entity ID와 Projectile ID를 key로 하는 View Dictionary는 표현 객체 조회에만 사용한다.
- View 제거는 simulator의 retire/trigger 이벤트와 연결한다.
- AssetDatabase, UnityEditor 의존 코드는 Editor 환경에서만 안전한지 확인한다.
- 네트워크 실패는 전투 규칙을 변경하지 않고, 명시적인 로그와 UI/재시도 정책으로 처리한다.
- `Update`에서 호출되는 코드는 nullable simulator, 프레임 시간 범위, 반복 simulationSpeed의 영향을 검토한다.
- ClientLib의 네트워크 실패는 `LogHelper.Error("메서드명: 실패 이유")`를 남기고 `false` 또는 null 결과로 반환한다.

## 6. 서버 규칙

### 요청과 상태 전이

- Controller는 `stageId`, `userId`, `requestId` 같은 필수 입력을 먼저 검증한다.
- 도메인 규칙 위반은 `DomainException`과 전역 미들웨어를 통해 일관된 API 오류로 변환한다.
- User의 stamina, 재화, 현재 stage 같은 상태는 User 또는 명시적인 도메인 규칙을 통해 변경한다.
- Controller가 DB 상태를 직접 변경하지 않는다.

### 멱등성

변경성 요청의 순서는 유지한다.

1. Redis 캐시 조회
2. DB log 선조회
3. 유효성 확인과 실제 상태 전이
4. DB log 기록
5. UNIQUE 충돌 시 기존 log 재조회
6. 응답을 Redis에 저장

규칙:

- Redis는 빠른 응답 수단이며 최종 정합성 근거가 아니다.
- DB UNIQUE 제약과 조건부 UPDATE가 동시 요청의 최종 보호 장치다.
- 새 POST API는 같은 requestId 재시도, 다른 requestId 동시 요청, 캐시 적중, DB log 적중, UNIQUE 충돌을 모두 검토한다.
- `CancellationToken`은 Controller부터 DB 호출까지 전달한다.
- 실패 응답과 성공 응답의 재시도 정책을 구분한다.

## 7. 테스트 기준

### CommonLib

- Pathfinding, fixed-point 수치, 상태 전이, Entity 상태, 전투 종료 조건은 순수 C# 검증을 우선한다.
- 테스트명은 검증하는 조건을 드러낸다. 예: `TestDiagonalLine45Degrees`, `TestVerticalLineReverse`.
- 경로 탐색은 정방향·역방향·수평·수직·대각·경계 조건을 모두 포함한다.

### Server

- xUnit 기반 서비스 테스트는 정상 흐름과 실패 흐름을 모두 포함한다.
- 테스트명은 `대상_전제조건_Should기대결과` 형식을 사용하고, 본문은 `// Act`, `// Assert`로 실행과 검증을 구분한다.
- 멱등성 테스트는 같은 requestId, 캐시 hit, DB log hit, 동시성 충돌을 구분한다.
- 멱등성 검증은 응답뿐 아니라 DB log 수와 상태가 중복 변경되지 않았는지도 확인한다.
- state transition 테스트는 Enter → Clear, Enter → GiveUp, 잘못된 stageId, stamina 부족을 포함한다.

### Unity Client

자동화가 어렵다면 다음을 수동 시나리오로 남긴다.

- 전투 시작부터 종료까지 View 생성·제거
- 승리·패배·무승부 UI
- 서버 검증 성공·실패
- 반복 테스트와 scene 재로딩
- 맵 데이터 누락·잘못된 데이터
