using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core.Director
{
    /// <summary>
    /// 表示装载了动画的轨迹
    /// </summary>
    public class AnimationTrack : TimelineRailItem
    {
        public Guid animationGuid;
        public int AnimationStartFrameIndex;
        public float AnimationSpeed = 1;

        //public override void ToFrameIndex(IDictionary<Guid, LayoutPropertyBag> dictionary, float frameIndex)
        //{
        //    if (UI.Controller.AppController.Instance.LoadedAnimation.TryGetValue(animationGuid, out Animation _animation))
        //    {
        //        _animation.ToFrameIndex(dictionary, frameIndex * AnimationSpeed + AnimationStartFrameIndex);
        //    }
        //}

        //public override void Effect(IDictionary<Guid, LayoutPropertyBag> dictionary, float input)
        //{
        //    if (UI.Controller.AppController.Instance.LoadedAnimation.TryGetValue(animationGuid, out Animation _animation))
        //    {
        //        _animation.Effect(dictionary, input);
        //    }
        //}

        //public override void EndingEffect(IDictionary<Guid, LayoutPropertyBag> dictionary)
        //{
        //    if (UI.Controller.AppController.Instance.LoadedAnimation.TryGetValue(animationGuid, out Animation _animation))
        //    {
        //        _animation.EndingEffect(dictionary);
        //    }
        //}
    }
}
