using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DirectCanvas.UICommand
{
    public class Command_Redo : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public CanvasCase CanvasCase
        {
            get { return _canvasCase; }
            set
            {
                _canvasCase = value;
                _canvasCase.UndoManager.UndoAdded += CanExecuteTest;
                _canvasCase.UndoManager.UndoHappened += CanExecuteTest;
                _canvasCase.UndoManager.RedoHappened += CanExecuteTest;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }
        }

        private void CanExecuteTest(object sender, EventArgs e)
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        CanvasCase _canvasCase;

        public bool CanExecute(object parameter)
        {
            if (_canvasCase == null) return false;
            return _canvasCase.UndoManager.RedoStackIsNotEmpty;
        }

        public void Execute(object parameter)
        {
            lock (_canvasCase)
            {
                _canvasCase.UndoManager.Redo();
                UI.Controller.AppController.Instance.CanvasRerender();
                executeAction?.Invoke();
            }
        }
        public Action executeAction;
    }
}
