namespace MVVM.Runtime.Core
{
    public interface IValueConverter
    {
        object Convert(object source);
        object ConvertBack(object target);
    }
}