# VDele API — документация для фронта

Документ актуален на **2026-05-15**. Бэк: Михаил.

---

## 0. Зачем этот документ

Описание REST API двух приложений: **VDele** (маркетплейс услуг, заказчики + мастера) и **VLavke** (маркетплейс товаров, покупатели + продавцы). Используют общий бэк, общий User по телефону, но **разные роли в каждом приложении**.

Один юзер может одновременно быть:
- В VDele: заказчиком (`customer`) и/или мастером (`specialist`)
- В VLavke: покупателем (`customer`) и/или продавцом (`seller`)
- Под одним и тем же телефоном — это один и тот же User

Бэк построен на .NET 10 ASP.NET Core. Все эндпоинты под общим префиксом `/api`.

---

## 1. Базовая информация

| Параметр | Значение |
|---|---|
| Base URL (prod) | `https://api.vdele.online/api` |
| Base URL (local) | `http://localhost:9001/api` |
| Content-Type | `application/json; charset=utf-8` |
| Авторизация | JWT в заголовке `Authorization: Bearer <token>` |
| Кодировка телефона | Только формат `+7XXXXXXXXXX` (11 цифр с плюсом) |
| Кодировка SMS-кода | 4 цифры (regex `^\d{4}$`) |

⚠️ **Важно:** в URL-параметрах символ `+` надо кодировать как `%2B`. Например `?phone=%2B79991234567`. В Postman через Params-tab это происходит автоматически, в raw URL — нет.

---

## 2. Формат ответов (Envelope)

**Все** ответы (успех и ошибка) обёрнуты в Envelope-структуру.

### Успешный ответ
```json
{
  "result": { "...": "полезные данные тут" },
  "errors": null,
  "isError": false,
  "timeGenerated": "2026-05-15T10:30:00"
}
```

Если `result` — примитив или строка (как JWT), то:
```json
{
  "result": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "errors": null,
  "isError": false,
  "timeGenerated": "2026-05-15T10:30:00"
}
```

### Ответ с ошибкой
```json
{
  "result": null,
  "errors": [
    {
      "code": "auth.phone_code.invalid_code",
      "message": "Invalid verification code",
      "type": "VALIDATION",
      "invalidField": "code"
    }
  ],
  "isError": true,
  "timeGenerated": "2026-05-15T10:30:00"
}
```

В массиве `errors` может быть несколько объектов (например при валидации нескольких полей).

### Соответствие `type` и HTTP-кода

| `type` | HTTP | Семантика |
|---|---|---|
| VALIDATION | 400 | Невалидные данные в запросе (поле в `invalidField`) |
| NOT_FOUND | 404 | Ресурс не найден |
| CONFLICT | 409 | Конфликт состояния (юзер уже зареган и т.п.) |
| AUTHENTICATION | 401 | Авторизация не прошла |
| AUTHORIZATION | 403 | Нет доступа |
| FAILURE | 500 | Внутренняя ошибка |

`invalidField` (только для VALIDATION) — имя поля из тела запроса в camelCase. Используется для подсветки конкретного поля в форме.

---

## 3. JWT

### Что внутри JWT (payload)

Получаешь токен из эндпоинтов login/register. Декодировать можно на jwt.io. Внутри:

| Claim | Что |
|---|---|
| `nameid` (или `sub`) | userId (Guid строкой) |
| `email` | Почта (опционально, может быть пусто) |
| `unique_name` (или `name`) | Username — обычно совпадает с телефоном |
| `application` | `vdele` / `vlavke` / `admin_panel` |
| `role` | **Массив строк** ролей: `["customer"]`, `["customer","specialist"]` |
| `exp` | Unix timestamp истечения |
| `iss` | `osnovanie-api` |

⚠️ **Важно — claim `role` это массив.** Даже если у юзера одна роль, в JWT будет `"role": ["customer"]` (или может быть строкой если одна — зависит от сериализатора). Безопаснее всегда обрабатывать как массив:

```typescript
const roleClaim = decoded.role;
const roles: string[] = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
```

### Сценарий применения JWT

1. Получил токен → положил в `localStorage.setItem('jwt', token)`
2. В каждый защищённый запрос добавляешь header:
   ```
   Authorization: Bearer <jwt>
   ```
3. Если бэк отвечает 401 — токен истёк/невалиден, разлогинить юзера (удалить из localStorage, перейти на форму логина)

### Срок жизни

Сейчас **30 дней** (`JwtOptions.TokenLifetimeMinutes = 43200`). Когда истечёт — фронт получит 401, надо будет перелогинить.

### Применимость JWT
JWT от VDele работает **только** для эндпоинтов `/vdele/*` (где `application=vdele`). JWT от VLavke — только для `/vlavke/*`. Cross-app использование вернёт 401.

---

## 4. Эндпоинты — общие auth

### 4.1. `POST /auth/phone/send-code`

Отправить SMS с кодом подтверждения.

**Body:**
```json
{ "phone": "+79991234567" }
```

**Response 200:** `null` (просто факт что отправлено)

**Особенности:**
- Телефон в формате `+7XXXXXXXXXX`
- Если телефон в списке `TestPhones` (env-var) — SMS **не отправляется**, в БД пишется фиксированный тестовый код. Это для QA, чтобы не жечь смсы
- Не требует авторизации

**Errors:**
- 400 `auth.phone.invalid_format` — неверный формат телефона
- 500 `auth.sms.send_failed` — провайдер SMS не ответил

---

### 4.2. `GET /auth/phone/exists`

Узнать существует ли юзер с таким телефоном и какие у него роли в указанном приложении.

**Query:** `?phone=%2B79991234567&applicationCode=vdele`

`applicationCode` — одно из: `vdele`, `vlavke`.

**Response 200:**
```json
{
  "result": { "roles": ["customer", "specialist"] }
}
```

**Возможные исходы:**

| Сценарий | HTTP | result |
|---|---|---|
| Юзер есть, в VDele зареган как customer | 200 | `{ roles: ["customer"] }` |
| Юзер есть, в VDele зареган как мастер | 200 | `{ roles: ["customer","specialist"] }` |
| Юзер есть, но НЕ в VDele (зареган только в VLavke) | 200 | `{ roles: [] }` |
| Телефона вообще нет в системе | 404 | `auth.user.not_found_by_phone` |

**Когда использовать:** **до логина**, юзер только ввёл телефон. По ответу решаешь — показать форму логина или регистрации.

**Логика на фронте:**
```
if 404 → новый юзер, форма регистрации
if 200 && roles.length === 0 → юзер в системе есть, но не в нашем приложении → форма регистрации в этом приложении
if 200 && roles.length > 0 → юзер уже зареган тут, форма логина
```

---

## 5. Эндпоинты — VDele

### 5.1. `POST /vdele/auth/login-by-phone`

Вход в VDele. **Не привязан к конкретной роли** — пускает любого юзера у которого есть хоть одна роль в VDele.

**Body:**
```json
{ "phone": "+79991234567", "code": "1234" }
```

**Response 200:** `"<JWT>"` (строкой, в `result`)

JWT внутри содержит **все** роли юзера в VDele.

**Errors:**
- 400 — невалидный формат phone/code
- 401 `auth.invalid_credentials` — юзер не найден ИЛИ нет роли в VDele
- 400 `auth.phone_code.invalid_code` — код не совпадает с тем что в БД
- 400 `auth.phone_code.expired` — код просрочен
- 400 `auth.phone_code.already_used` — код уже использован
- 404 `auth.phone_code.not_found` — для этого телефона нет активного кода (не запрашивал send-code или истёк)

---

### 5.2. `POST /vdele/customers/register-by-phone`

Регистрация заказчика VDele.

**Body:**
```json
{
  "phone": "+79991234567",
  "code": "1234",
  "fullName": "Михаил Иванов",
  "cityId": "00000000-0000-0000-0000-000000000000",
  "email": "optional@example.com"
}
```

`cityId` — берётся из `/reference-data/cities`.
`email` — опционально, может быть `null` или отсутствовать.

**Response 200:** `"<JWT>"` с `roles: ["customer"]` (или больше если юзер уже зарегистрирован в VDele с другими ролями).

**Поведение по сценариям:**

| Состояние юзера | Что происходит |
|---|---|
| Телефон не существует в системе | Создаётся User + customer-роль + CustomerProfile |
| Юзер есть, но без customer-профиля в VDele | Добавляется только customer-роль + CustomerProfile |
| Юзер есть и customer-профиль уже есть | **409 `vdele.customer.already_exists`** |

**Errors:**
- 400 — невалидные поля
- 400/404 — проблемы с SMS-кодом (см. login)
- 409 `vdele.customer.already_exists` — заказчик уже зареган

---

### 5.3. `POST /vdele/specialists/register-by-phone`

Регистрация мастера VDele.

**Body:**
```json
{
  "phone": "+79991234567",
  "code": "1234",
  "fullName": "Михаил Иванов",
  "cityId": "guid",
  "email": "optional",
  "about": "Опционально, описание услуг до 2000 символов"
}
```

**Response 200:** `"<JWT>"` с `roles: ["customer", "specialist"]`.

**Поведение по сценариям:**

| Состояние юзера | Что происходит |
|---|---|
| Телефон не существует | Создаётся User + customer-роль + **CustomerProfile baseline** + specialist-роль + SpecialistProfile |
| Юзер есть, без specialist-профиля | Добавляются specialist-роль + SpecialistProfile (если CustomerProfile тоже не было — создаётся baseline) |
| Юзер есть и specialist-профиль есть | **409 `vdele.specialist.already_exists`** |

⚠️ **Бизнес-правило:** мастер **всегда** автоматически становится и заказчиком. CustomerProfile baseline создаётся с теми же ФИО/город/email что указаны в форме мастера.

**Errors:**
- 400 — невалидные поля
- 409 `vdele.specialist.already_exists` — мастер уже зареган

---

### 5.4. `POST /vdele/specialists/profile`

Добавить роль specialist уже **залогиненному** заказчику. Без SMS — авторизация через Bearer.

**Headers:** `Authorization: Bearer <JWT>`

**Body:**
```json
{
  "fullName": "Михаил Иванов",
  "cityId": "guid",
  "email": "optional",
  "about": "optional"
}
```

**Response 200:** `"<новый JWT с обеими ролями>"`

**Когда использовать:** юзер залогинен как customer, в личном кабинете жмёт "Стать мастером". Не нужно повторно подтверждать телефон — он уже залогинен.

⚠️ **После успеха:** замени старый JWT в localStorage на новый из response. Старый содержит `roles: ["customer"]`, новый — `["customer", "specialist"]`.

**Errors:**
- 401 — нет JWT или невалидный
- 400 — невалидные поля
- 409 `vdele.specialist.already_exists` — у юзера уже есть specialist-профиль

---

### 5.5. `GET /vdele/auth/me`

Получить актуальное состояние своего аккаунта в VDele (берётся из БД, не из JWT).

**Headers:** `Authorization: Bearer <JWT>`

**Response 200:**
```json
{
  "result": {
    "userId": "00000000-0000-0000-0000-000000000000",
    "phone": "+79991234567",
    "roles": ["customer", "specialist"],
    "hasCustomerProfile": true,
    "hasSpecialistProfile": false
  }
}
```

**Когда использовать:**
1. **При старте приложения**, если JWT уже есть — освежить картинку
2. **После 403** на каком-то VDele-эндпоинте — возможно роль изменилась
3. **Чтобы понять заполнен ли профиль** — флаги `hasCustomerProfile`/`hasSpecialistProfile`

**Что делать с результатом:**
- Сравни `roles` с тем что в JWT. Если **отличается** — попроси юзера перелогиниться (его JWT устарел, на другом устройстве добавились роли)
- По флагам `has*Profile` решай показывать ли "доделать профиль"

**Errors:**
- 401 — нет JWT, истёк или невалидный

---

### 5.6. `POST /vdele/admin/login`

Логин админа VDele. По логину/паролю, **без SMS**.

**Body:**
```json
{ "username": "vdele_admin", "password": "secret" }
```

**Response 200:** `"<JWT>"` с `roles: ["admin"]`.

**Errors:**
- 401 `auth.invalid_credentials` — неверный логин/пароль

---

## 6. Эндпоинты — VLavke

VLavke устроен **симметрично** VDele. Все эндпоинты ниже работают так же как VDele-аналоги, только с другими ролями.

### 6.1. `POST /vlavke/auth/login-by-phone`
Идентично VDele login. JWT с ролями для VLavke.

### 6.2. `POST /vlavke/customers/register-by-phone`
Регистрация покупателя.

**Body:**
```json
{
  "phone": "+7...",
  "code": "1234",
  "fullName": "...",
  "cityId": "guid",
  "email": "optional"
}
```

### 6.3. `POST /vlavke/sellers/register-by-phone`
Регистрация продавца.

**Body:**
```json
{
  "phone": "+7...",
  "code": "1234",
  "fullName": "...",
  "mainCityId": "guid",   ← обрати внимание: mainCityId, не cityId
  "email": "optional"
}
```

⚠️ **Отличие от VDele:** seller **НЕ** создаёт автоматически профиль customer (в отличие от specialist в VDele который создаёт customer baseline). Если хочется чтобы продавец тоже мог покупать — это пока не реализовано на бэке.

### 6.4. `GET /vlavke/auth/me`

**Response:**
```json
{
  "result": {
    "userId": "guid",
    "phone": "+7...",
    "roles": ["customer", "seller"],
    "hasCustomerProfile": true,
    "hasSellerProfile": true
  }
}
```

### 6.5. `POST /vlavke/admin/login`
Идентично VDele admin login. JWT с `roles: ["admin"]` и `application: "admin_panel"`.

---

## 7. Reference data

### 7.1. `GET /reference-data/cities`

Список городов. Используется для `cityId`/`mainCityId` в регистрации.

**Response 200:**
```json
{
  "result": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Москва",
      "regionName": "Москва",
      "timeZoneId": "Europe/Moscow"
    },
    {
      "id": "...",
      "name": "Санкт-Петербург",
      "regionName": "Санкт-Петербург",
      "timeZoneId": "Europe/Moscow"
    }
  ]
}
```

Возвращает только видимые города, отсортированные по приоритету и имени. Не требует авторизации.

---

## 8. Полные сценарии (флоу)

### Сценарий A. Новый юзер регистрируется как заказчик VDele

```
1. Юзер на vdele.online, ввёл телефон +79991234567
2. GET /api/auth/phone/exists?phone=%2B79991234567&applicationCode=vdele
   → 404
3. Фронт: "Новый юзер. Регистрируемся как заказчик"
4. GET /api/reference-data/cities → список городов для select
5. Юзер выбрал город, ввёл ФИО
6. POST /api/auth/phone/send-code { phone }
   → 200
7. Юзер ввёл код из SMS
8. POST /api/vdele/customers/register-by-phone
   { phone, code, fullName, cityId, email }
   → 200 + JWT (roles=[customer])
9. localStorage.setItem('jwt', token)
10. Юзер залогинен в режиме customer
```

### Сценарий B. Существующий заказчик логинится

```
1. Ввёл телефон
2. GET /api/auth/phone/exists?phone=...&applicationCode=vdele
   → 200 { roles: ["customer"] }
3. Фронт: "Юзер есть, логинимся"
4. POST /api/auth/phone/send-code
5. Юзер вводит код
6. POST /api/vdele/auth/login-by-phone { phone, code }
   → 200 + JWT
7. Сохранил JWT, режим customer
```

### Сценарий C. Заказчик хочет стать мастером (уже залогинен)

```
1. JWT в localStorage (roles=[customer])
2. В ЛК нажал "Стать мастером"
3. Фронт показывает форму: ФИО, город, описание
4. POST /api/vdele/specialists/profile
   Headers: Authorization: Bearer <JWT>
   Body: { fullName, cityId, email, about }
   → 200 + новый JWT (roles=[customer, specialist])
5. localStorage.setItem('jwt', newToken) ← заменили!
6. UI обновляется: появляется переключатель "Режим заказчика / Режим мастера"
```

### Сценарий D. Новый юзер сразу как мастер

```
1. На лендинге для мастеров ввёл телефон
2. GET /api/auth/phone/exists → 404
3. Фронт показывает форму регистрации мастера
4. send-code → SMS → код
5. POST /api/vdele/specialists/register-by-phone
   { phone, code, fullName, cityId, email, about }
   → 200 + JWT (roles=[customer, specialist])
   ← бэк сразу создаёт обе роли
6. Юзер залогинен с обеими ролями
```

### Сценарий E. Логин с нового устройства (юзер уже зареган и где-то ещё)

```
Устройство A — добавил specialist роль
БД: roles=[customer, specialist]

Устройство B (новое):
1. Юзер вводит телефон
2. GET /api/auth/phone/exists → 200 { roles: ["customer", "specialist"] }
3. send-code → код
4. POST /api/vdele/auth/login-by-phone
   → 200 + JWT с обеими ролями
5. На устройстве B сразу видны обе роли
```

### Сценарий F. Освежить состояние при старте приложения

```
1. Фронт стартует, нашёл JWT в localStorage
2. Декодировал JWT — там roles=[customer]
3. GET /api/vdele/auth/me с Bearer
   → 200 { roles: ["customer", "specialist"], hasCustomerProfile: true, hasSpecialistProfile: true }
4. Расхождение! В JWT одна роль, в БД две
5. Фронт показывает баннер: "Найдены новые роли. Перелогиньтесь чтобы продолжить"
6. Юзер перелогинивается → получает свежий JWT
```

---

## 9. Что изменилось от старой версии API

Если до этого использовался **старый API** (одна роль в JWT, role-specific логины):

### Удалено
- `POST /vdele/customers/login-by-phone` → используй `POST /vdele/auth/login-by-phone`
- `POST /vdele/specialists/login-by-phone` → используй `POST /vdele/auth/login-by-phone`
- `POST /vlavke/customers/login-by-phone` → используй `POST /vlavke/auth/login-by-phone`
- `POST /vlavke/sellers/login-by-phone` → используй `POST /vlavke/auth/login-by-phone`

### Изменилось
- `GET /auth/phone/exists` — убран query-параметр `roleCode`. Возвращает **массив ролей** `roles[]` вместо 200/404 по конкретной роли. 404 теперь означает "телефона вообще нет в системе".
- JWT claim `role` теперь **массив** строк, не одна строка
- При регистрации заказчика — если телефон есть в системе но customer-профиля нет, **не возвращает CONFLICT**, а добавляет роль customer (раньше блокировал)

### Добавлено
- `POST /vdele/auth/login-by-phone` — единый логин в VDele
- `POST /vlavke/auth/login-by-phone` — единый логин в VLavke
- `POST /vdele/specialists/profile` — добавить роль specialist залогиненному юзеру (Bearer вместо SMS)
- `GET /vdele/auth/me` — актуальное состояние аккаунта в VDele
- `GET /vlavke/auth/me` — то же для VLavke

---

## 10. Чек-лист миграции фронта

- [ ] Парсинг JWT — `role` обрабатывать как `string[]` (или конвертировать строку→массив для совместимости)
- [ ] Удалить вызовы `/vdele/customers/login-by-phone` и `/vdele/specialists/login-by-phone`, использовать `/vdele/auth/login-by-phone`
- [ ] Удалить вызовы `/vlavke/customers/login-by-phone` и `/vlavke/sellers/login-by-phone`, использовать `/vlavke/auth/login-by-phone`
- [ ] Обновить `/auth/phone/exists` — убрать параметр `roleCode`, читать массив `result.roles`
- [ ] Добавить вызов `/vdele/auth/me` при старте app для синхронизации
- [ ] Добавить UI "Стать мастером" с вызовом `/vdele/specialists/profile` + замену JWT
- [ ] Если есть переключатель режимов (заказчик/мастер) — показывать его только если в `roles` есть обе роли

---

## 11. Тестовые номера (если настроены через env)

Если на сервере прописаны `Auth__PhoneVerification__TestPhones` — звонки `send-code` для этих номеров **не отправляют SMS**, в БД пишется фиксированный код из `Auth__PhoneVerification__TestPhoneFixedCode`. Используется для QA чтобы не тратить деньги на смс.

Если фронт тестируется с тестовым номером — код приходит не из SMS а от QA через `.env` сервера. Уточняй у бэка какой номер и код прописаны.

---

## 12. Известные ограничения

1. **JWT не отзывается.** Если юзер вышел из аккаунта на одном устройстве, JWT остаётся валидным до `exp`. Это стандартное поведение stateless-JWT. Если нужны refresh-токены или blacklist — пока не реализовано.

2. **Рассинхрон JWT и БД.** JWT — слепок на момент логина. Если на устройстве A добавили роль через `/specialists/profile`, на устройстве B JWT не "обновится сам". Решается через `/me` + перелогин.

3. **Один JWT — одно приложение.** JWT от VDele не работает для VLavke и наоборот. Если юзер хочет работать в обоих — два независимых логина и два JWT (фронт хранит отдельно).

4. **Сменился владелец номера.** Если SIM-карту перепродали, новый владелец сможет зайти как старый юзер (получит SMS на этот номер). Известная слабость phone-auth, не специфична для нашего проекта.

5. **VLavke seller без customer baseline.** В отличие от VDele specialist (где регистрация мастера автоматически делает заказчика), VLavke seller — только продавец. Бизнес-решение обсудимое.

---

## 13. TypeScript типы (готовые к использованию)

Скопируй в свой проект целиком. Имена в `result` идут camelCase'ом.

```typescript
// ============================================================
// Базовая обёртка ответа
// ============================================================

export interface Envelope<T> {
  result: T | null;
  errors: ApiError[] | null;
  isError: boolean;
  timeGenerated: string; // ISO datetime
}

export interface ApiError {
  code: string;          // напр. "auth.phone_code.invalid_code"
  message: string;       // человекочитаемое
  type: ErrorType;
  invalidField: string | null;  // для VALIDATION — имя поля
}

export type ErrorType =
  | "VALIDATION"
  | "NOT_FOUND"
  | "CONFLICT"
  | "AUTHENTICATION"
  | "AUTHORIZATION"
  | "FAILURE";

// ============================================================
// JWT payload (после декодирования)
// ============================================================

export interface JwtPayload {
  // .NET сериализует ClaimTypes.* в URI-имена. Используем оба варианта:
  sub?: string;
  nameid?: string;           // = userId (Guid)
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"?: string;

  email?: string;
  unique_name?: string;      // = phone обычно

  application: "vdele" | "vlavke" | "admin_panel";

  // ⚠️ может быть string ИЛИ string[] в зависимости от количества ролей
  role: string | string[];
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string | string[];

  exp: number;               // unix timestamp
  iss: string;
}

// Утилита для безопасного получения массива ролей
export function extractRoles(payload: JwtPayload): string[] {
  const r = payload.role ?? payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  if (!r) return [];
  return Array.isArray(r) ? r : [r];
}

// ============================================================
// Domain константы
// ============================================================

export const ApplicationCodes = {
  VDele: "vdele",
  VLavke: "vlavke",
  AdminPanel: "admin_panel",
} as const;

export const RoleCodes = {
  Customer: "customer",
  Specialist: "specialist",
  Seller: "seller",
  Admin: "admin",
} as const;

// ============================================================
// Auth (общие)
// ============================================================

export interface SendPhoneCodeRequest {
  phone: string;           // "+7XXXXXXXXXX"
}
// Response: Envelope<null>

export interface CheckPhoneExistsQuery {
  phone: string;
  applicationCode: "vdele" | "vlavke";
}
export interface CheckPhoneExistsResponse {
  roles: string[];         // например ["customer", "specialist"]
}

// ============================================================
// VDele
// ============================================================

export interface VDeleLoginByPhoneRequest {
  phone: string;
  code: string;            // 4 цифры
}
// Response: Envelope<string>  ← в result строка JWT

export interface RegisterVDeleCustomerRequest {
  phone: string;
  code: string;
  fullName: string;        // max 200
  cityId: string;          // GUID
  email?: string | null;
}
// Response: Envelope<string>  ← JWT

export interface RegisterVDeleSpecialistRequest {
  phone: string;
  code: string;
  fullName: string;        // max 200
  cityId: string;
  email?: string | null;
  about?: string | null;   // max 2000
}
// Response: Envelope<string>  ← JWT

export interface AddSpecialistProfileRequest {
  fullName: string;
  cityId: string;
  email?: string | null;
  about?: string | null;
}
// Headers: Authorization: Bearer <JWT>
// Response: Envelope<string>  ← новый JWT с обновлёнными ролями

export interface VDeleAuthMeResponse {
  userId: string;
  phone: string;
  roles: string[];
  hasCustomerProfile: boolean;
  hasSpecialistProfile: boolean;
}

export interface AdminLoginRequest {
  username: string;
  password: string;
}
// Response: Envelope<string>  ← JWT

// ============================================================
// VLavke
// ============================================================

export interface VLavkeLoginByPhoneRequest {
  phone: string;
  code: string;
}

export interface RegisterVLavkeCustomerRequest {
  phone: string;
  code: string;
  fullName: string;
  cityId: string;
  email?: string | null;
}

export interface RegisterVLavkeSellerRequest {
  phone: string;
  code: string;
  fullName: string;
  mainCityId: string;      // обрати внимание: mainCityId, не cityId
  email?: string | null;
}

export interface VLavkeAuthMeResponse {
  userId: string;
  phone: string;
  roles: string[];
  hasCustomerProfile: boolean;
  hasSellerProfile: boolean;
}

// ============================================================
// Reference data
// ============================================================

export interface CityResponse {
  id: string;
  name: string;
  regionName: string;
  timeZoneId: string;
}
// GET /reference-data/cities → Envelope<CityResponse[]>
```

---

## 14. Полный справочник кодов ошибок

Колонка `code` — то что приходит в `errors[].code`. Колонка `field` — то что в `errors[].invalidField`. Используй `code` для логики (не `message`), `message` показывай юзеру.

### 14.1. Общие auth-ошибки (модуль Auth)

| code | type | http | field | Когда возникает |
|---|---|---|---|---|
| `auth.user.not_found_by_phone` | NOT_FOUND | 404 | — | Телефона нет в БД |
| `auth.user.not_found` | NOT_FOUND | 404 | — | UserId не существует (для /me) |
| `auth.invalid_credentials` | AUTHENTICATION | 401 | — | Юзер не найден / нет ролей в applicationCode / неверный пароль |
| `auth.user.already_exists` | CONFLICT | 409 | — | (legacy, больше не возвращается из new flow) |
| `auth.phone.already_confirmed` | VALIDATION | 400 | — | (legacy) |
| `auth.registration.failed` | FAILURE | 500 | — | UserManager не смог создать User |
| `auth.email.send_failed` | FAILURE | 500 | — | Не удалось отправить email |
| `auth.sms.send_failed` | FAILURE | 500 | — | Не удалось отправить SMS (smsint недоступен) |
| `auth.user.id.empty` | VALIDATION | 400 | — | UserId пустой (внутренняя) |
| `auth.application.empty` | VALIDATION | 400 | `applicationCode` | applicationCode не передан |
| `auth.role.empty` | VALIDATION | 400 | — | (внутренняя) |

### 14.2. SMS-код

| code | type | http | Когда |
|---|---|---|---|
| `auth.phone_code.not_found` | NOT_FOUND | 404 | Для этого телефона нет активного кода (не запрашивал /send-code) |
| `auth.phone_code.invalid_code` | VALIDATION | 400 | Код не совпадает с тем что в БД |
| `auth.phone_code.expired` | VALIDATION | 400 | Код просрочен (время указано в `CodeLifetimeSeconds`) |
| `auth.phone_code.already_used` | VALIDATION | 400 | Код уже был использован |
| `auth.phone_code.not_confirmed` | VALIDATION | 400 | Код не подтверждён (внутренняя) |
| `auth.phone_code.phone_required` | VALIDATION | 400 | Phone пустой при создании кода |
| `auth.phone_code.invalid_lifetime` | VALIDATION | 400 | Lifetime ≤ 0 (внутренняя) |

### 14.3. Регистрация заказчика VDele

| code | type | http | field | Когда |
|---|---|---|---|---|
| `vdele.customer.phone.empty` | VALIDATION | 400 | `phone` | Phone пустой |
| `vdele.customer.code.invalid` | VALIDATION | 400 | `code` | Code не соответствует regex `^\d{4}$` |
| `vdele.customer.full_name.empty` | VALIDATION | 400 | `fullName` | FullName пустой |
| `vdele.customer.full_name.too_long` | VALIDATION | 400 | `fullName` | FullName > 200 символов |
| `vdele.customer.city_id.empty` | VALIDATION | 400 | `cityId` | CityId = Guid.Empty или не передан |
| `vdele.customer.email.invalid` | VALIDATION | 400 | `email` | Email есть и не соответствует формату |
| `vdele.customer.user_id.empty` | VALIDATION | 400 | `userId` | UserId пустой (внутренняя) |
| `vdele.customer.register.request.empty` | VALIDATION | 400 | `request` | Тело запроса = null |
| `vdele.customer.already_exists` | CONFLICT | 409 | — | У этого юзера уже есть CustomerProfile |

### 14.4. Регистрация мастера VDele

| code | type | http | field | Когда |
|---|---|---|---|---|
| `vdele.specialist.phone.empty` | VALIDATION | 400 | `phone` | Phone пустой |
| `vdele.customer.code.invalid` | VALIDATION | 400 | `code` | Code не 4 цифры (⚠️ префикс `customer` — копипаст-баг, см. п.18) |
| `vdele.specialist.full_name.empty` | VALIDATION | 400 | `fullName` | — |
| `vdele.specialist.full_name.too_long` | VALIDATION | 400 | `fullName` | > 200 символов |
| `vdele.specialist.city_id.empty` | VALIDATION | 400 | `cityId` | — |
| `vdele.specialist.email.invalid` | VALIDATION | 400 | `email` | — |
| `vdele.specialist.about.too_long` | VALIDATION | 400 | `about` | About > 2000 символов |
| `vdele.specialist.user_id.empty` | VALIDATION | 400 | `userId` | (внутренняя) |
| `vdele.specialist.register.request.empty` | VALIDATION | 400 | `request` | Тело = null |
| `vdele.specialist.already_exists` | CONFLICT | 409 | — | У юзера уже есть SpecialistProfile |

### 14.5. Регистрация покупателя VLavke

⚠️ В кодах ошибок прописан префикс `vdele.customer.*` — это **известный баг копипасты** (см. п.18). Реальное поведение работает корректно, просто коды одинаковые с VDele customer.

| code | type | http | field | Когда |
|---|---|---|---|---|
| `vdele.customer.phone.empty` | VALIDATION | 400 | `phone` | Phone пустой (тот же код что у VDele!) |
| `vdele.customer.code.invalid` | VALIDATION | 400 | `code` | — |
| `vdele.customer.full_name.empty` | VALIDATION | 400 | `fullName` | — |
| `vdele.customer.full_name.too_long` | VALIDATION | 400 | `fullName` | — |
| `vdele.customer.city_id.empty` | VALIDATION | 400 | `cityId` | — |
| `vdele.customer.email.invalid` | VALIDATION | 400 | `email` | — |
| `vdele.customer.user_id.empty` | VALIDATION | 400 | `userId` | — |
| `vdele.customer.register.request.empty` | VALIDATION | 400 | `request` | — |
| `vdele.customer.already_exists` | CONFLICT | 409 | — | (тот же код что у VDele) |

### 14.6. Регистрация продавца VLavke

| code | type | http | field | Когда |
|---|---|---|---|---|
| `vlavke.seller.phone.empty` | VALIDATION | 400 | `phone` | — |
| `vdele.customer.code.invalid` | VALIDATION | 400 | `code` | (⚠️ копипаст-баг, см. п.18) |
| `vlavke.seller.full_name.empty` | VALIDATION | 400 | `fullName` | — |
| `vlavke.seller.full_name.too_long` | VALIDATION | 400 | `fullName` | — |
| `vlavke.seller.main_city_id.empty` | VALIDATION | 400 | `mainCityId` | — |
| `vlavke.seller.email.invalid` | VALIDATION | 400 | `email` | — |
| `vlavke.seller.user_id.empty` | VALIDATION | 400 | `userId` | — |
| `vlavke.seller.register.request.empty` | VALIDATION | 400 | `request` | — |
| `vlavke.seller.already_exists` | CONFLICT | 409 | — | У юзера уже есть SellerProfile |

### 14.7. Reference data (cities)

| code | type | http | Когда |
|---|---|---|---|
| `location.city.id.empty` | VALIDATION | 400 | CityId пустой |
| `location.city.name.empty` | VALIDATION | 400 | (внутренняя) |
| `location.city.region.empty` | VALIDATION | 400 | (внутренняя) |
| `location.city.timezone.empty` | VALIDATION | 400 | (внутренняя) |
| `location.city.not_found` | NOT_FOUND | 404 | Город по id не найден |
| `location.city.not_visible` | VALIDATION | 400 | Город существует но скрыт |

### 14.8. Generic auth/admin

| code | type | http | Когда |
|---|---|---|---|
| `vdele.admin.username.empty` | VALIDATION | 400 | Username не передан |
| `vdele.admin.password.empty` | VALIDATION | 400 | Password не передан |
| `auth.password.empty` | VALIDATION | 400 | Password пустой при регистрации |
| `auth.password.too_short` | VALIDATION | 400 | < 6 символов |
| `auth.password.too_long` | VALIDATION | 400 | > 100 символов |
| `auth.firstname.empty` | VALIDATION | 400 | FirstName пустое |
| `auth.firstname.too_long` | VALIDATION | 400 | > 50 символов |

### 14.9. VDele auth (новые эндпоинты)

| code | type | http | field | Когда |
|---|---|---|---|---|
| `vdele.auth.phone.empty` | VALIDATION | 400 | `phone` | В `/vdele/auth/login-by-phone` phone пустой |
| `vdele.auth.code.invalid` | VALIDATION | 400 | `code` | В `/vdele/auth/login-by-phone` code не 4 цифры |
| `vlavke.auth.phone.empty` | VALIDATION | 400 | `phone` | В `/vlavke/auth/login-by-phone` phone пустой |
| `vlavke.auth.code.invalid` | VALIDATION | 400 | `code` | В `/vlavke/auth/login-by-phone` code не 4 цифры |

---

## 15. Валидационные правила полей (backend-side)

| Поле | Правило | Что вернётся при нарушении |
|---|---|---|
| `phone` | Regex `^\+7\d{10}$` (плюс, 7, ровно 10 цифр) | `Phone must be in format +7XXXXXXXXXX` |
| `code` (SMS) | Regex `^\d{4}$` (ровно 4 цифры) | `vdele.customer.code.invalid` |
| `fullName` | NotEmpty + max 200 символов | `*.full_name.empty` / `.too_long` |
| `cityId` / `mainCityId` | NotEmpty (не `Guid.Empty`) | `*.city_id.empty` |
| `email` | Если не пустой — должен быть валидным email | `*.email.invalid` |
| `about` (только specialist) | Опционально, max 2000 символов | `vdele.specialist.about.too_long` |
| `applicationCode` (query) | NotEmpty, строка `vdele` или `vlavke` | `auth.application.empty` |
| `username` (admin) | NotEmpty | `vdele.admin.username.empty` |
| `password` (admin) | NotEmpty | `vdele.admin.password.empty` |

⚠️ **Сообщение в коде ошибки `vdele.customer.code.invalid` говорит "должен содержать 6 цифр"** — это устаревшее сообщение, валидация фактически принимает 4 цифры. См. п.18.

---

## 16. Примеры кода для фронта

### 16.1. HTTP-клиент с обработкой Envelope

```typescript
async function api<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const jwt = localStorage.getItem('jwt');

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (jwt) {
    (headers as Record<string, string>).Authorization = `Bearer ${jwt}`;
  }

  const res = await fetch(`https://api.vdele.online/api${path}`, {
    ...options,
    headers,
  });

  // 401 — JWT истёк или невалиден
  if (res.status === 401) {
    localStorage.removeItem('jwt');
    window.location.href = '/login';
    throw new Error('Unauthorized');
  }

  const envelope: Envelope<T> = await res.json();

  if (envelope.isError) {
    // Кидаем кастомную ошибку с массивом backend-ошибок
    throw new ApiException(envelope.errors ?? []);
  }

  return envelope.result as T;
}

class ApiException extends Error {
  constructor(public errors: ApiError[]) {
    super(errors[0]?.message ?? 'Unknown error');
  }

  // Первая ошибка для простых случаев
  get first(): ApiError | undefined {
    return this.errors[0];
  }

  // Проверка кода (логика)
  hasCode(code: string): boolean {
    return this.errors.some(e => e.code === code);
  }

  // Получить ошибки по конкретному полю (для форм)
  byField(field: string): ApiError[] {
    return this.errors.filter(e => e.invalidField === field);
  }
}
```

### 16.2. Сценарий: проверка телефона перед логином/регистрацией

```typescript
async function checkPhone(phone: string) {
  try {
    const result = await api<CheckPhoneExistsResponse>(
      `/auth/phone/exists?phone=${encodeURIComponent(phone)}&applicationCode=vdele`
    );

    if (result.roles.length === 0) {
      return { exists: true, hasVDeleAccess: false, roles: [] };
    }

    return { exists: true, hasVDeleAccess: true, roles: result.roles };

  } catch (e) {
    if (e instanceof ApiException && e.hasCode('auth.user.not_found_by_phone')) {
      return { exists: false, hasVDeleAccess: false, roles: [] };
    }
    throw e;
  }
}
```

### 16.3. Сценарий: логин

```typescript
async function loginVDele(phone: string, code: string): Promise<void> {
  const jwt = await api<string>('/vdele/auth/login-by-phone', {
    method: 'POST',
    body: JSON.stringify({ phone, code }),
  });

  localStorage.setItem('jwt', jwt);
}
```

### 16.4. Сценарий: регистрация заказчика

```typescript
async function registerCustomer(form: RegisterVDeleCustomerRequest): Promise<void> {
  try {
    const jwt = await api<string>('/vdele/customers/register-by-phone', {
      method: 'POST',
      body: JSON.stringify(form),
    });

    localStorage.setItem('jwt', jwt);

  } catch (e) {
    if (e instanceof ApiException && e.hasCode('vdele.customer.already_exists')) {
      // Юзер уже зареган — перенаправить на форму логина
      throw new Error('Уже зарегистрирован, войдите');
    }
    throw e;
  }
}
```

### 16.5. Сценарий: "стать мастером" с заменой JWT

```typescript
async function becomeSpecialist(form: AddSpecialistProfileRequest): Promise<void> {
  // С Bearer токеном — api-функция выше его добавит
  const newJwt = await api<string>('/vdele/specialists/profile', {
    method: 'POST',
    body: JSON.stringify(form),
  });

  // ⚠️ КРИТИЧНО — заменить старый JWT новым
  localStorage.setItem('jwt', newJwt);

  // Сейчас новый JWT содержит roles=[customer, specialist]
}
```

### 16.6. Сценарий: проверка состояния при старте app

```typescript
async function syncAuthState(): Promise<void> {
  const jwt = localStorage.getItem('jwt');
  if (!jwt) return;

  try {
    const me = await api<VDeleAuthMeResponse>('/vdele/auth/me');

    // Проверить расхождение с JWT
    const jwtPayload = parseJwt(jwt);
    const jwtRoles = extractRoles(jwtPayload);
    const dbRoles = me.roles;

    const sameRoles =
      jwtRoles.length === dbRoles.length &&
      jwtRoles.every(r => dbRoles.includes(r));

    if (!sameRoles) {
      // Роли в БД отличаются от JWT — попросить перелогин
      alert('Ваши роли обновились. Войдите снова.');
      localStorage.removeItem('jwt');
      window.location.href = '/login';
      return;
    }

    // Сохранить в store актуальные флаги
    store.setUser(me);

  } catch (e) {
    // 401 уже обработан в api()
    console.error('Failed to sync auth state', e);
  }
}

function parseJwt(token: string): JwtPayload {
  const base64 = token.split('.')[1];
  const json = atob(base64.replace(/-/g, '+').replace(/_/g, '/'));
  return JSON.parse(json);
}
```

### 16.7. Сценарий: показать ошибки валидации в форме

```typescript
async function handleSubmit(form: RegisterVDeleCustomerRequest) {
  const formErrors: Record<string, string> = {};

  try {
    await registerCustomer(form);
    router.push('/dashboard');

  } catch (e) {
    if (e instanceof ApiException) {
      // Раскидать ошибки по полям формы
      for (const err of e.errors) {
        if (err.invalidField) {
          formErrors[err.invalidField] = err.message;
        }
      }

      // Если ни одна ошибка не привязана к полю — глобальное сообщение
      const generic = e.errors.find(e => !e.invalidField);
      if (generic) {
        showToast(generic.message);
      }
    } else {
      showToast('Что-то пошло не так');
    }
  }

  return formErrors;
}
```

---

## 17. Чек-листы

### 17.1. Чек-лист интеграции (новый фронт-проект)

- [ ] Реализовать `api<T>()` обёртку с обработкой Envelope
- [ ] Реализовать `ApiException` с методами `hasCode`/`byField`
- [ ] Реализовать interceptor для 401 → редирект на /login
- [ ] Утилита `parseJwt` + `extractRoles` для работы с массивом ролей
- [ ] Хранение JWT в localStorage + автоподстановка в Authorization
- [ ] При старте app — вызов `/vdele/auth/me` для синхронизации
- [ ] Кэшировать `/reference-data/cities` (меняется редко)

### 17.2. Чек-лист на каждую форму регистрации/логина

- [ ] Phone-input принимает только формат `+7XXXXXXXXXX`
- [ ] Перед отправкой проверить `/auth/phone/exists` и направить юзера на login/register
- [ ] SMS-код только 4 цифры (regex `^\d{4}$`)
- [ ] Локально показывать таймер до повторного `/send-code` (~60 сек)
- [ ] При 400 — показать ошибку у конкретного поля по `invalidField`
- [ ] При 409 (already_exists) — предложить логин вместо регистрации
- [ ] При 404 (code.not_found) — попросить запросить код заново

---

## 18. Известные несоответствия и нюансы

Список того что выглядит странно но **работает корректно**:

1. **Код ошибки `vdele.customer.code.invalid`** — текст сообщения говорит "должен содержать 6 цифр", фактически принимает 4. Это устаревший текст, не баг логики. Фронт ориентируется на regex `^\d{4}$`.

2. **Префикс `vdele.customer.*` в ошибках VLavke** — файл `VLavkeCustomerErrors.cs` и `VLavkeCustomerValidationErrors.cs` используют коды с префиксом `vdele.customer.*` (копипаст). То же в `VLavkeSellerValidationErrors.CodeIsInvalid()` — там `vdele.customer.code.invalid`. **Реальное поведение корректное**, только коды ошибок не отражают модуль. Если будешь маппить error code → UI message — учти что один и тот же код может прийти от двух модулей. Используй HTTP path для контекста.

3. **JWT-claim `role` бывает строкой или массивом** в зависимости от реализации JsonWebTokenHandler в .NET. Всегда оборачивай через `extractRoles()` чтобы получить `string[]`.

4. **JWT не отзывается на бэке.** После `/specialists/profile` старый JWT по-прежнему валиден до `exp`. Если юзер сидит на двух устройствах — на одном со старым JWT он не "увидит" что добавилась роль. Решается через `/auth/me` + перелогин.

5. **`/auth/phone/exists` для несуществующего телефона** возвращает **404** (`auth.user.not_found_by_phone`), а не 200 с пустым массивом. Это семантический выбор: 404 значит "вообще нет такого юзера", 200 с `{ roles: [] }` значит "юзер есть, но без ролей в этом приложении".

6. **VLavke seller не делает customer baseline.** При регистрации продавца customer-профиль НЕ создаётся (в отличие от VDele specialist). Если фронт нужно показывать "вы и продавец и покупатель" — это пока не поддержано на бэке.

7. **`mainCityId` vs `cityId`** — VLavke seller использует поле `mainCityId`, все остальные — `cityId`. Не перепутай.

8. **CORS allowed origins** — захардкожены в `DependencyInjection.cs`: `https://vdele.online`, `https://vlavke.online`, `https://api.vdele.online`, `https://www.vdele.online`, `https://www.vlavke.online`, `http://localhost:5173`, `http://localhost:5174`. Если деплоишь на другой домен — попроси добавить.

9. **Параметры `query string` с `+` в телефоне** — `+` в URL значит пробел. Кодировать как `%2B`. `encodeURIComponent` это делает.

10. **JWT lifetime — 30 дней.** Очень долго. Refresh-токенов нет. Если юзер не заходит 30+ дней — перелогинится по SMS как обычно.

---

## 19. Окружения

| Окружение | URL | Что |
|---|---|---|
| Прод API | `https://api.vdele.online/api` | Боевой |
| Лок API | `http://localhost:9001/api` | Запускается через `docker compose -f docker-compose-dev.yml up -d` или `dotnet run` |
| Прод фронт VDele | `https://vdele.online` | На Vercel |
| Прод фронт VLavke | `https://vlavke.online` | (планируется) |
| Логи (Seq) | `https://api.vdele.online:8081` или ssh-tunnel | Только Михаилу |

---

## 20. Контакт

Если что-то непонятно или нашёл расхождение между этим документом и реальным поведением API — пиши Михаилу.

Документ актуален: **2026-05-15**.
