using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DirectCanvas.Layout;
using DirectCanvas.UI.Controller;

namespace DirectCanvas.UICommand
{
    public class Command_LayoutHidden : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            bool hidden = ((PictureLayout)parameter).Hidden;
            ((PictureLayout)parameter).Hidden = !hidden;
            AppController.Instance.CanvasRerender();
        }
    }
}
