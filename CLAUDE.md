# CLAUDE.md

Игра для геймджема (2D top-down, космос, полёт на отдаче пушки).
Полный план и история решений: Assets/GAME_PLAN.md.

## Техника
- Проект сохранён в Unity 6000.0.79f1, у пользователя стоит 6000.0.85f2:
  C:\Program Files\Unity\Hub\Editor\6000.0.85f2\Editor\Unity.exe
- Проект на URP 2D. В сцене есть Main Camera (тег MainCamera) и Global Light 2D.
  Спрайтам оставляй материал по умолчанию. Shader.Find не используй: в сборке
  шейдер может быть вырезан.
- Unity 6 API: у Rigidbody2D linearVelocity и linearDamping.
- Глобальная гравитация в настройках -9.81, её не меняй.
  У всех наших тел gravityScale = 0.
- Ввод: UnityEngine.Input (в проекте включены оба способа).
- Не создавай теги, слои и sorting layers: они хранятся в ProjectSettings,
  которые мы не коммитим. Порядок отрисовки задавай через sortingOrder.
  Столкновение пули с игроком отключай через Physics2D.IgnoreCollision.
- Интерфейс игры (HUD): uGUI. Надписи рисуются своим пиксельным шрифтом
  (PixelFont: 5x7, только заглавные латинские, цифры и - . : / + !) через
  компонент PixelLabel на RawImage; иконки лежат в Assets/Art/Shapes/icon_*.png.
  На экране иконки и числа, без предложений и подсказок.
  Меню (s_StartMenu, TextMesh Pro) делает программист, его не трогаем.
- Рестарт и смена уровня: без перезагрузки сцены (Destroy уровня и новый
  Instantiate префаба). Статические поля сбрасывай при старте.
- Ошибки компиляции смотри в логе редактора:
  %LOCALAPPDATA%\Unity\Editor\Editor.log
  Если редактор закрыт, можешь проверить компиляцию запуском Unity
  в batchmode с -quit и -logFile.

## Архитектура
- Игра НЕ строится из кода при запуске. Всё лежит в сцене и префабах, ссылки
  назначены в Inspector ([SerializeField]). Эту структуру задал программист в main.
  - Assets/_Scenes/DraftScene.unity: камера (CameraRig), Global Light 2D, HUD,
    объект DraftGame (список префабов уровней, стартовый уровень).
  - Assets/Prefabs/: Player, Bullet, Asteroid_*, Meteor_*, Ship, OxygenTank,
    OxygenCrystal, Mine_Hunter/_Wall/_Patrol/_Orbit/_Heavy, MineRing,
    BlackHole_Big/_Small/_Drift, Planet, Leak, AirPuff, Blast,
    Background_LevelN (фон в цветах уровня).
    Уровни: Prefabs/Levels/LevelN.prefab (корень с компонентом Level: имя,
    кислород, границы, цвет света; MeteorSpawner на корне включает метеоры).
  - Во время игры создаются только Instantiate префабов (уровень, пули,
    кристаллы, осколки, метеоры, взрывы, течи). Рестарт: Destroy уровня и новый Instantiate.
  - Меню Filip > Rebuild Prefabs And Scene (Assets/Editor/DraftSetup.cs) делает все
    префабы из кода и из levelN.txt. Повторный запуск ПЕРЕЗАПИСЫВАЕТ префабы и
    HUD/DraftGame в сцене: если префабы уже правили руками, сначала спроси.
    То же из командной строки: Unity -batchmode -quit -executeMethod DraftSetup.RebuildAll
  - Art, SpriteFactory, LevelData только для редактора (Assets/Editor).
    Простые формы и иконки лежат в Assets/Art/Shapes.
- Скрипты игры: Assets/_Scripts/. Скрипты редактора: Assets/Editor/.
- Картинки: Assets/_Resources/Art/. Исходники уровней: Assets/_Resources/Levels/.
  Генераторы картинок и уровней (вне Unity): Assets/Editor/Tools~.
- Все числа баланса в ассете Assets/_Settings/Tuning.asset (класс Tuning,
  ScriptableObject). У каждого компонента есть поле tuning; его заполняет
  меню Filip > Connect Tuning To Prefabs And Scene. Новое число: поле в Tuning.cs
  со значением по умолчанию; если меняешь старое, поправь и Tuning.asset.
- Все слова на экране: Assets/_Scripts/Texts.cs.
- Масштаб: 1 единица = 100 пикселей. Камера ортографическая, size 10
  (видно примерно 35.5x20 единиц). Клетка уровня 2x2 единицы.
- Вид игры: светится только важное для игры (игрок, пули, баллоны, метеоры, мины).
  Камни крупные (2, 4, 6 единиц = 1, 2, 3 клетки), тёплые и светлые. Фон
  приглушён и не мерцает, чтобы не спорить с камнями.
- Пробная сборка без изменения ProjectSettings: меню Filip > Test Build (Windows).

## Правила Git
- Никогда не коммить в main. Вся работа в ветке filip/draft.
  (Старая версия, где игра строилась из кода, сохранена тегом draft-runtime-backup.)
- git push не делай никогда, пока пользователь не напишет "заливай".
  Заливается только то, что уже работает, и пользователь сам говорит когда.
- После каждого шага делай только локальный коммит в ветке filip/draft.
- Если шаг что-то сломал и пользователь пишет "откати", вернись к предыдущему коммиту.
- Не трогай чужое: меню (Assets/_Scenes/s_StartMenu.unity, MainMenuScript.cs,
  SettingMenuScript.cs, Assets/TextMesh Pro), Assets/_Scenes/SampleScene.unity,
  Assets/_Scenes/LV1.unity, Assets/_Scripts/GunScript.cs, PlayerMovementScript.cs.
- Не коммить изменения в ProjectSettings/ и Packages/, даже если Unity
  изменил их сам. Перед каждым коммитом проверяй git status.
- Коммить файлы вместе с .meta.
- В main не сливай без "ок, сливай" от пользователя. При конфликте остановись.

## Правила работы
- Один шаг за раз. После шага остановись и жди отзыва.
- Не добавляй ничего сверх шага, даже если кажется полезным.
- Отчёт после шага: что сделано (3 строки), что нажать в Unity (3 строки).
- Когда пользователь пишет "слабо", "сильно", "медленно", меняй числа в Tuning.
- Пользователь не программист: в Unity он только открывает сцену и жмёт Play.
  Не задавай вопросов, решай сам и коротко пиши, что решил. Исключение:
  если git попросит имя и почту для коммита, спроси.
