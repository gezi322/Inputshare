using Avalonia;
using Avalonia.Markup.Xaml;

namespace InputshareB
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
