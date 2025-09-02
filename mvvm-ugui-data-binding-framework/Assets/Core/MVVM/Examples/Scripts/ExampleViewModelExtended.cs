using MVVM.Runtime.Core;
using UnityEngine.Scripting;

[Bindable]
[Preserve]
public class ExampleViewModelExtended : ExampleViewModel
{
    public ExampleViewModelExtended(string name) : base(name)
    {
    }

    [Preserve]
    [Bindable]
    public string Exp
    {
        get => _exp;
        set => SetProperty(ref _exp, value);
    }

    private string _exp;

    [Bindable]
    [Preserve]
    public void Test()
    {
        
    }
}