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

## 13. Контакт

Если что-то непонятно или нашёл расхождение между этим документом и реальным поведением API — пиши Михаилу.

Документ актуален: **2026-05-15**.
