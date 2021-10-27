using System.Collections;
using System.Collections.Generic;

namespace NekoPainter.Core.UndoCommand
{
    public class UndoManager : System.IDisposable
    {
        public LinkedList<IUndoCommand> undoStack;
        public Stack<IUndoCommand> redoStack;
        public bool UndoStackIsNotEmpty { get => undoStack.First != null; }
        public bool RedoStackIsNotEmpty { get => redoStack.Count != 0; }
        //public event System.EventHandler UndoHappened;
        //public event System.EventHandler RedoHappened;
        //public event System.EventHandler UndoAdded;
        /// <summary>
        /// 撤销
        /// </summary>
        public void Undo()
        {
            IUndoCommand eCmd = undoStack.First.Value;
            IUndoCommand redoCmd = eCmd.Execute();
            undoStack.RemoveFirst();
            redoStack.Push(redoCmd);
            eCmd.Dispose();
            //UndoHappened?.Invoke(this,new System.EventArgs());
        }
        /// <summary>
        /// 重做
        /// </summary>
        public void Redo()
        {
            IUndoCommand eCmd = redoStack.Pop();
            IUndoCommand undoCmd = eCmd.Execute();
            undoStack.AddFirst(undoCmd);
            eCmd.Dispose();
            //RedoHappened?.Invoke(this, new System.EventArgs());
        }
        public UndoManager()
        {
            undoStack = new LinkedList<IUndoCommand>();
            redoStack = new Stack<IUndoCommand>();
        }
        /// <summary>
        /// 添加撤销数据，清除重做栈的数据。
        /// </summary>
        public void AddUndoData(IUndoCommand cmd)
        {
            foreach (IUndoCommand disposed in redoStack)
            {
                disposed.Dispose();
                if (disposed is ICanDeleteCommand a) a.Delete();
            }
            redoStack.Clear();
            undoStack.AddFirst(cmd);
            if (undoStack.Count > 20)
            {
                undoStack.Last.Value.Dispose();
                if (undoStack.Last.Value is ICanDeleteCommand a) a.Delete();
                undoStack.RemoveLast();
            }
            //UndoAdded?.Invoke(this,new System.EventArgs());
        }

        public void Dispose()
        {
            while (undoStack.Count != 0)
            {
                undoStack.First.Value.Dispose();
                undoStack.RemoveFirst();
            }
            while (redoStack.Count != 0)
                redoStack.Pop().Dispose();
        }
    }
}

