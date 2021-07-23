using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectCanvas.Core.Director
{
    /// <summary>
    /// 表示单个动画
    /// </summary>
    public class Animation
    {
        public void EndingEffect(IDictionary<Guid, LayoutPropertyBag> dictionary)
        {
            foreach (var animationTagRail in animationTagRails)
            {
                if (animationTagRail.Muted) continue;
                if (dictionary.TryGetValue(TargetGuid, out var bag) &&
                    animationTagRail.Count > 0)
                {
                    if (animationTagRail.RailType == AnimationTagType.LayoutPropertyTag)
                    {
                        bag.propertyValues[animationTagRail.propertyIndex] = animationTagRail.Last().Value;
                    }
                    else if (animationTagRail.RailType == AnimationTagType.LayoutVisibleTag)
                    {
                        bag.hidden = animationTagRail.Last().Value != 0;
                    }
                }
            }
        }

        public void ToFrameIndex(IDictionary<Guid, LayoutPropertyBag> dictionary, float frameIndex)
        {
            if (!dictionary.TryGetValue(TargetGuid, out var bag))
            {
                bag = new LayoutPropertyBag();
                dictionary[TargetGuid] = bag;
            }
            foreach (var animationTagRail in animationTagRails)
            {
                if (animationTagRail.Muted) continue;
                if (animationTagRail.Count > 0)
                {

                    int i = 0;
                    while (animationTagRail.Count > i && animationTagRail[i].FrameIndex < frameIndex)
                    {
                        i++;
                    }
                    i = Math.Min(i, animationTagRail.Count - 1);
                    if (animationTagRail.RailType == AnimationTagType.LayoutPropertyTag)
                    {
                        if (i > 0 && frameIndex < animationTagRail[i].FrameIndex)
                        {
                            int frameIndexP = animationTagRail[i - 1].FrameIndex;
                            int frameIndexN = animationTagRail[i].FrameIndex;
                            float sc = (frameIndex - frameIndexP) / (float)(frameIndexN - frameIndexP);
                            bag.propertyValues[animationTagRail.propertyIndex] = (int)MathF.Round(animationTagRail[i].Value * sc + animationTagRail[i - 1].Value * (1 - sc));
                            bag.propertyValueUsed[animationTagRail.propertyIndex] = true;
                        }
                        else
                        {
                            bag.propertyValues[animationTagRail.propertyIndex] = animationTagRail[i].Value;
                            bag.propertyValueUsed[animationTagRail.propertyIndex] = true;
                        }
                    }
                    else if (animationTagRail.RailType == AnimationTagType.LayoutVisibleTag)
                    {
                        bag.hidden = animationTagRail[i].Value != 0;
                    }
                }
            }
        }

        public void Effect(IDictionary<Guid, LayoutPropertyBag> dictionary, float input)
        {
            float frameIndex = input * GetAnimationLength();
            ToFrameIndex(dictionary, frameIndex);
        }

        public int GetAnimationLength()
        {
            int maxLength = 0;
            for (int i = 0; i < animationTagRails.Count; i++)
            {
                maxLength = Math.Max(maxLength, animationTagRails[i].GetAnimationLength());
            }
            return maxLength;
        }

        public void Initialize()
        {
            animationTagRails.Clear();
            Name = "";
            Guid = Guid.Empty;
            TargetGuid = Guid.Empty;
            Saved = false;
        }

        public ObservableCollection<AnimationTagRail> animationTagRails = new ObservableCollection<AnimationTagRail>();
        public string Name;
        public Guid Guid;
        public Guid TargetGuid;
        public bool Saved;
    }
}
