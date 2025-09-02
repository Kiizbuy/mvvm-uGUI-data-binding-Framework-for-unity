using System;
using MVVM.Runtime.Core;
using UnityEngine;
using UnityEngine.Scripting;

[Serializable]
[Bindable]
[Preserve]
public class ExampleViewModel : ViewModel
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

    public ExampleViewModel(string name)
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