using System;
using MVVM.Runtime.Collections;
using UnityEngine;
using MVVM.Runtime.Core;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class ExampleViewModelMonoBehaviour : ScreenViewModelMonoBehaviour
{
    [Bindable]
    public ExampleViewModel SubViewModel => _subViewModel;

    private ExampleViewModel _subViewModel = new ExampleViewModel("placeholder");
    
    [Bindable]
    public ExampleViewModelExtended ExtendedSubViewModel => _extendedSubViewModel;

    private ExampleViewModelExtended _extendedSubViewModel = new ExampleViewModelExtended("placeholder2");
    
    [Bindable]
    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    [Bindable]
    public Sprite Avatar
    {
        get => _avatar;
        set => SetProperty(ref _avatar, value);
    }

    [Bindable]
    public float Score
    {
        get => _score;
        set => SetProperty(ref _score, value);
    }

    [Bindable]
    public int SomeCounter
    {
        get => _someCounter;
        set => SetProperty(ref _someCounter, value);

    }
    
    [Bindable]
    public bool Active
    {
        get => _active;
        set => SetProperty(ref _active, value);
    }
    
    [Bindable]
    public ObservableDataList<ExampleViewModel> SomeTest { get; } = new ObservableDataList<ExampleViewModel>(); 
    [Bindable]
    public ObservableDataList<ExampleViewModel> SomeTest2 { get; } = new ObservableDataList<ExampleViewModel>();
    [Bindable]
    public ObservableDataList<int> DropdownOptions { get; } = new ObservableDataList<int>();

    [Bindable]
    public int SelectedOption
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }
    [SerializeField] private int _selected;
    [SerializeField] private string _playerName = "Player";
    [SerializeField] private Sprite _avatar;
    [SerializeField] private float _score;
    [SerializeField] private int _someCounter;
    [SerializeField] private bool _active;

    [Bindable]
    private void AddNewElement()
    {
        SomeTest.Add(new ExampleViewModel("First" + UnityEngine.Random.Range(1, 100)));
    }
    
    private void Awake()
    {
        DropdownOptions.Add(1);
        DropdownOptions.Add(2);
        DropdownOptions.Add(3);
        SelectedOption = 3;
    }

    protected override void OnShown()
    {
    }

    protected override void OnHided()
    {
    }
}