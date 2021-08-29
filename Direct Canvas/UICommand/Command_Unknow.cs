using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DirectCanvas.UICommand
{
    public class Command_Unknow : ICommand
    {
        public Command_Unknow()
        {

        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return activate;
        }

        public bool Activate { get { return activate; } set { activate = value; CanExecuteChanged?.Invoke(this, new EventArgs()); } }
        bool activate = true;

        public void Execute(object parameter)
        {
            executeAction?.Invoke();
        }
        public Action executeAction;
    }
}
