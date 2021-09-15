using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Core.UndoCommand
{
    public class CMD_Empty : IUndoCommand
    {
        public void Dispose()
        {

        }

        public IUndoCommand Execute()
        {
            return this;
        }
    }
}
