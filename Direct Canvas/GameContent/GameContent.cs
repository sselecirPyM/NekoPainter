using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.Foundation;

namespace DirectCanvas
{
    class GameContent2
    {
        public void Main()
        {

        }

        public void StartRenderLoop()
        {
            if (m_renderLoopWorker?.Status == AsyncStatus.Started)
            {
                return;
            }

            WorkItemHandler workItemHandler = new WorkItemHandler(_ =>
            {
                while (_.Status == AsyncStatus.Started)
                {
                    Update();
                    if (Render())
                    {
                        //present
                    }
                }
            });
            m_renderLoopWorker = ThreadPool.RunAsync(workItemHandler,WorkItemPriority.High,WorkItemOptions.TimeSliced);
        }

        public void StopRenderLoop()
        {
            m_renderLoopWorker?.Cancel();
        }

        private void Update()
        {

        }

        private bool Render()
        {
            return false;
        }

        private IAsyncAction m_renderLoopWorker;
    }
}
