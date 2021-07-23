using DirectCanvas.Core.Director;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DirectCanvas.UICommand
{
    public class Command_AniRailMute : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            bool muted = ((AnimationTagRail)parameter).Muted;
            ((AnimationTagRail)parameter).Muted = !muted;
            Executed?.Invoke(this, parameter);
        }

        public event EventHandler<object> Executed;
    }
}
