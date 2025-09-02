# MVVM uGUI data binding Framework for Unity

Коротко: лёгкий и производительный MVVM-фреймворк для Unity uGUI, вдохновлённый Unity Weld, но реализующий привязки через генерацию runtime expressions и делегатов для более высокой производительности.

---

## Оглавление

* [Введение](#введение)
* [Особенности](#особенности)
* [Базовые концепции](#базовые-концепции)
* [Требования и быстрая установка](#требования-и-быстрая-установка)
* [Создание ViewModel](#создание-viewmodel)

   * [ViewModelMonoBehaviour (пример)](#viewmodelmonobehaviour-пример)
   * [Обычный ViewModel (POCO) (пример)](#обычный-viewmodel-poco-пример)
* [Привязка данных (Binding)](#привязка-данных-binding)

   * [Привязка к свойству](#привязка-к-свойству)
   * [Привязка к SubViewModel](#привязка-к-subviewmodel)
* [Binder'ы: OneWay / TwoWay / Command / Collection](#binders-oneway--twoway--command--collection)

   * [Пример OneWayBinder](#пример-onewaybinder)
   * [Пример TwoWayBinder](#пример-twowaybinder)
* [Коллекции](#коллекции)
* [Конвертеры (Converters)](#конвертеры-converters)
* [FAQ](#чаво-faq)
* [Контакты и вклад](#контакты-и-вклад)
* [Лицензия](#лицензия)

---

## Введение

Этот фреймворк был вдохновлён проектом **Unity Weld** ([ссылка на репозиторий](https://github.com/Real-Serious-Games/Unity-Weld)), но реализует привязки иначе — не через тяжёлую чистую рефлексию, а через генерацию runtime expressions, которые в рантайме конвертируются в делегаты с прямым вызовом методов. Это даёт заметное преимущество по производительности на сложных UI. Также реализована гибкая система Binder'ов и поддержка SubViewModel, а Editor часть сделана на UI Toolkit и максимально универсален.

---

## Особенности

* Реализация привязок через runtime expressions → делегаты.
* Гибкая архитектура Binder'ов — легко расширяется под ваши нужды.
* Поддержка SubViewModel и коллекций ViewModel.
* Editor-инструменты на UI Toolkit (расширяемые).
* Работает на IL2CPP (при соблюдении некоторых требований) и на мобильных платформах.

---

## Базовые концепции

* **Model** — бизнес-логика и данные (POCO, ECS-компоненты и т.п.).
* **View** — UI-элементы (TextMeshPro, Image, Toggle, Slider и т.д.).
* **ViewModel** — посредник между Model и View, содержит состояние, доступное для биндинга.
* **Binders** — компоненты, связывающие ViewModel и View (OneWay, TwoWay, Command, Collection и т.д.).
* **Converters** — преобразуют данные (например: формат валюты, DateTime и т.д.).
* **ViewModelTemplates** — провайдеры ViewModel'ей для коллекций и SubViewModel.

---

## Требования и быстрая установка

* Unity 6000.0.33f1+ (рекомендуется LTS).
* Для IL2CPP + aggressive stripping помечайте ViewModel и их члены атрибутом `[UnityEngine.Scripting.Preserve]`.
* Просто добавьте папку фреймворка в ваш проект Unity и откройте примеры сцен/префабов (если мне не будет лень их добавлять :) ).

---

## Создание ViewModel

Есть два основных варианта:

* `ViewModelMonoBehaviour` — база для ViewModel, которые живут на GameObject (удобно для быстрого прототипирования и сцен).
* `ViewModel` — чистые классы (POCO), используются как SubViewModel или элементы коллекций.

### ViewModelMonoBehaviour (пример)

```csharp
public class ExampleViewModelBehaviour : ViewModelMonoBehaviour
{
    [Bindable]
    public string PlayerName
    {
        get => _playerName;
        set => Set(ref _playerName, value);
    }
    private string _playerName;

    [Bindable]
    public int Health
    {
        get => _health;
        set => Set(ref _health, value);
    }
    private int _health;

    [Bindable]
    public Sprite PlayerIcon
    {
        get => _playerIcon;
        set => Set(ref _playerIcon, value);
    }
    private Sprite _playerIcon;

    [Bindable]
    public SomePocoExampleViewModel SubViewModel => _subViewModel;
    private SomePocoExampleViewModel _subViewModel = new SomePocoExampleViewModel("placeholder");

    [Bindable]
    public ObservableDataList<SomePocoExampleViewModel> SomeViewModelCollection { get; } = new ObservableDataList<SomePocoExampleViewModel>();

    [Bindable]
    public ObservableDataList<int> DropdownOptions { get; } = new ObservableDataList<int>();

    [Bindable]
    public int SelectedOption
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }
    private int _selected;

    [Bindable]
    private void TestMethod()
    {
        Debug.Log(nameof(TestMethod));
        SomeViewModelCollection.Add(new SomePocoExampleViewModel("Меч") { Count = 1 });
    }
}
```

### Обычный ViewModel (POCO) — пример

```csharp
[Serializable]
[Bindable]
public class SomePocoExampleViewModel : ViewModel
{
    [Bindable]
    [Preserve]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    [Bindable]
    [Preserve]
    public Sprite Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    [Bindable]
    [Preserve]
    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    private string _name;
    private Sprite _icon;
    private int _count;

    public SomePocoExampleViewModel(string name)
    {
        Name = name;
    }

    [Bindable]
    [Preserve]
    public void FirstTestMethod()
    {
        Debug.Log(nameof(FirstTestMethod));
    }

    [Bindable]
    [Preserve]
    public void SecondTestMethod()
    {
        Debug.Log(nameof(SecondTestMethod));
    }
}
```

> Примечание: на чистых (non-Mono) ViewModel обязательно добавлять атрибут `[Bindable]`, чтобы SubViewModelTemplate/CollectionTemplate корректно обрабатывали члены класса.

---

## Привязка данных (Binding)

### Привязка к свойству

1. Создайте ViewModel (наследуйтесь от `ViewModelMonoBehaviour` или создайте POCO и проставьте Bindable).
2. Добавьте UI-элемент на сцену (Text, TMP, Image, Toggle и т.д.).
3. Добавьте соответствующий `Binder` к UI-элементу.
4. В инспекторе укажите:

   * Источник данных (ваша ViewModel)
   * Имя свойства (например `PlayerName`)

### Привязка к SubViewModel

1. На UI создайте контейнер (GameObject).
2. Добавьте компонент `SubViewModelTemplate` на префаб-контейнер.
3. В корневой ViewModel выберите свойство SubViewModel.
4. Внутри контейнера можно биндить свойства SubViewModel как обычно.

---

## Binder'ы: OneWay / TwoWay / Command / Collection

Фреймворк предоставляет набор стандартных Binder'ов и позволяет реализовать свои, унаследовавшись от `BinderBase`, `OneWayBinder<TSource,TTarget>`, `TwoWayBinder<TSource,TTarget>` и т.д.

### Пример OneWayBinder

```csharp
[AddComponentMenu("MVVM/Binders/GameObjectActivity Binder")]
public class GameObjectActivityBinder : OneWayBinder<bool, bool>
{
    [SerializeField] private GameObject _gameObject;

    private void Awake()
    {
        _gameObject ??= gameObject;
        //обязательно вызываем base.OnAwake после кеширования данных
        base.Awake();
    }

    protected override void ApplyTargetValue(bool value)
    {
        _gameObject.SetActive(value);
    }
}
```

### Пример TwoWayBinder

```csharp
[AddComponentMenu("MVVM/Binders/Slider Binder")]
[RequireComponent(typeof(Slider))]
[SupportedBindingTypes(typeof(float))]
public sealed class SliderBinder : TwoWayBinder<float, float>
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        base.Awake();
    }

    protected override void ApplyTargetValue(float value)
    {
        if (_slider != null && !_slider.wholeNumbers && Mathf.Abs(_slider.value - value) > 0.001f)
            _slider.value = value;
    }

    protected override void SubscribeToUiEvents()
    {
        if (_slider != null)
            _slider.onValueChanged.AddListener(OnUiValueChanged);
    }

    protected override void UnsubscribeFromUiEvents()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(OnUiValueChanged);
    }
}
```

---

## Коллекции

Для коллекций используется `ObservableDataList<T>` — поведение похоже на обычный `List<T>`, но с уведомлениями об изменениях.

```csharp
[Bindable]
public class InventoryViewModel : ViewModel
{
    [Bindable]
    public ObservableDataList<ItemViewModel> Items { get; } = new ObservableDataList<ItemViewModel>();

    public InventoryViewModel()
    {
        Items.Add(new ItemViewModel("Меч") { Count = 1 });
        Items.Add(new ItemViewModel("Зелье здоровья") { Count = 3 });
        // использование RemoveAt, Clear, Swap и т.д.
    }
}
```

### Привязка коллекции к UI

1. Создайте префаб элемента списка с компонентом `CollectionViewModelTemplate`.
2. На сцене разместите контейнер с компонентом `ObservableViewModelListBinder`.
3. В инспекторе укажите источник данных (ViewModel), имя свойства-коллекции и префаб.

---

## Конвертеры (Converters)

Создание конвертера — наследуйте от `ValueConverterT<TSource, TTarget>` и реализуйте `Convert`/`ConvertBack`.

```csharp
[CreateAssetMenu(menuName = "MVVM/Converters/Enum To String")]
public sealed class EnumToStringConverterT<TEnum> : ValueConverterT<TEnum, string> where TEnum : Enum
{
    public override string Convert(TEnum source) => source.ToString();
    public override TEnum ConvertBack(string target) => (TEnum)Enum.Parse(typeof(TEnum), target);
}
```

Чтобы использовать конвертер — добавьте созданный `ScriptableObject` конвертер в список конвертеров PropertyBinder'а в инспекторе.

---

## Чаво (FAQ)

* **Могу ли я писать свою сложную логику Binder'ов?** — Да, унаследуйтесь от `BinderBase` или нужного базового класса и реализуйте логику.
* **Работает ли с IL2CPP и aggressive stripping?** — Да, но пометьте классы и члены атрибутом `[UnityEngine.Scripting.Preserve]`.
* **Работает ли на WebGL / Android / iOS?** — Гарантированно работает на Android, PC, iOS. WebGL не тестировался полноценно — используйте с осторожностью.
* **Могу ли я используя данный MVVM фреймворк делать сложные анимации?** — В теории - да, если вы сделаете свою обертку над [Stateful UI](https://github.com/dmitry-ivashenko/StatefulUI) который может через StateRole воспроизводить анимации

---

## Контакты и вклад

Если хотите внести улучшения или нашли баг — присылайте PR/Issue в репозиторий (или напишите мне [в телеграм](https://t.me/kiizbuy)).

---


---

