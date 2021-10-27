using System.Collections;
using System.Collections.Generic;
namespace NekoPainter.Core.UndoCommand
{
    public interface ICanDeleteCommand
    {
        void Delete();
    }
}